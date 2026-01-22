using Caro.Core.Entities;
using Caro.Infrastructure;
using Caro.Services;
using Caro.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Caro.Tests.Services;

public class GameServiceTests : IDisposable
{
    private readonly CaroDbContext _db;
    private readonly GameService _gameService;

    public GameServiceTests()
    {
        _db = TestHelper.CreateInMemoryDbContext();
        _gameService = new GameService(_db);
    }

    [Fact]
    public async Task CreateGameAsync_WithPvEMode_ShouldCreateGame()
    {
        // Arrange
        var options = new CreateGameOptions("PvE", 1, null, 1, 60);

        // Act
        var game = await _gameService.CreateGameAsync(options);

        // Assert
        game.Should().NotBeNull();
        game.Id.Should().BeGreaterThan(0);
        game.Mode.Should().Be("PvE");
        game.P1UserId.Should().Be(1);
        game.P2UserId.Should().BeNull();
        game.PveDifficulty.Should().Be(1);
        game.TimeControlSeconds.Should().Be(60);
        game.CurrentTurn.Should().Be(1);
        game.IsAi.Should().BeTrue();
        game.RoomOwnerId.Should().BeNull();
    }

    [Fact]
    public async Task CreateGameAsync_WithPvPMode_ShouldCreateGameWithRoomOwner()
    {
        // Arrange
        var options = new CreateGameOptions("PvP", 1, null, null, 60);

        // Act
        var game = await _gameService.CreateGameAsync(options);

        // Assert
        game.Should().NotBeNull();
        game.Mode.Should().Be("PvP");
        game.P1UserId.Should().Be(1);
        game.RoomOwnerId.Should().Be(1);
        game.IsAi.Should().BeFalse();
    }

    [Fact]
    public async Task MakeMoveAsync_WithValidMove_ShouldCreateMove()
    {
        // Arrange
        var game = await _gameService.CreateGameAsync(new CreateGameOptions("PvE", 1, null, 1, 60));
        var x = 7;
        var y = 7;

        // Act
        var move = await _gameService.MakeMoveAsync(game.Id, 1, x, y);

        // Assert
        move.Should().NotBeNull();
        move.GameId.Should().Be(game.Id);
        move.Player.Should().Be(1);
        move.X.Should().Be(x);
        move.Y.Should().Be(y);
        move.MoveNumber.Should().Be(1);
    }

    [Fact]
    public async Task MakeMoveAsync_WithValidMove_ShouldUpdateCurrentTurn()
    {
        // Arrange
        var game = await _gameService.CreateGameAsync(new CreateGameOptions("PvE", 1, null, 1, 60));

        // Act
        await _gameService.MakeMoveAsync(game.Id, 1, 7, 7);

        // Assert
        var updatedGame = await _gameService.GetGameAsync(game.Id);
        updatedGame.Should().NotBeNull();
        updatedGame!.CurrentTurn.Should().Be(2);
    }

    [Fact]
    public async Task MakeMoveAsync_WithWinCondition_ShouldMarkGameAsFinished()
    {
        // Arrange
        var game = await _gameService.CreateGameAsync(new CreateGameOptions("PvE", 1, null, 1, 60));
        
        // Create 4 consecutive moves for player 1 (horizontal), with AI blocking one end
        // Player 1: (7,7), (7,8), (7,9), (7,10) - 4 in a row
        // Player 2: blocks one end to prevent 4-two-ends win, but allows 5-in-a-row
        await _gameService.MakeMoveAsync(game.Id, 1, 7, 7);
        await _gameService.MakeMoveAsync(game.Id, 2, 7, 6); // AI blocks left end
        await _gameService.MakeMoveAsync(game.Id, 1, 7, 8);
        await _gameService.MakeMoveAsync(game.Id, 2, 8, 8); // AI blocks in different direction
        await _gameService.MakeMoveAsync(game.Id, 1, 7, 9);
        await _gameService.MakeMoveAsync(game.Id, 2, 8, 9); // AI blocks
        await _gameService.MakeMoveAsync(game.Id, 1, 7, 10);
        await _gameService.MakeMoveAsync(game.Id, 2, 8, 10); // AI blocks

        // Verify game is not finished yet (4 in a row but left end is blocked, so not 4-two-ends)
        var gameBeforeWin = await _gameService.GetGameAsync(game.Id);
        gameBeforeWin!.FinishedAt.Should().BeNull();

        // Act - Make 5th move to win (horizontal: 7,7 -> 7,11)
        await _gameService.MakeMoveAsync(game.Id, 1, 7, 11);

        // Assert
        var finishedGame = await _gameService.GetGameAsync(game.Id);
        finishedGame.Should().NotBeNull();
        finishedGame!.FinishedAt.Should().NotBeNull();
        finishedGame.WinnerId.Should().Be(1);
        finishedGame.Result.Should().Be("P1_WIN");
        finishedGame.WinType.Should().Be("FIVE_IN_ROW");
    }

    [Fact]
    public async Task MakeMoveAsync_WithInvalidTurn_ShouldThrowException()
    {
        // Arrange
        var game = await _gameService.CreateGameAsync(new CreateGameOptions("PvE", 1, null, 1, 60));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _gameService.MakeMoveAsync(game.Id, 2, 7, 7)); // Wrong turn
    }

    [Fact]
    public async Task MakeMoveAsync_WithOutOfBounds_ShouldThrowException()
    {
        // Arrange
        var game = await _gameService.CreateGameAsync(new CreateGameOptions("PvE", 1, null, 1, 60));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _gameService.MakeMoveAsync(game.Id, 1, 15, 15)); // Out of bounds
    }

    [Fact]
    public async Task MakeMoveAsync_WithOccupiedCell_ShouldThrowException()
    {
        // Arrange
        var game = await _gameService.CreateGameAsync(new CreateGameOptions("PvE", 1, null, 1, 60));
        await _gameService.MakeMoveAsync(game.Id, 1, 7, 7);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _gameService.MakeMoveAsync(game.Id, 2, 7, 7)); // Already occupied
    }

    [Fact]
    public async Task MakeMoveAsync_OnFinishedGame_ShouldThrowException()
    {
        // Arrange
        var game = await _gameService.CreateGameAsync(new CreateGameOptions("PvE", 1, null, 1, 60));
        
        // Create winning sequence for player 1 (horizontal)
        await _gameService.MakeMoveAsync(game.Id, 1, 7, 7);
        await _gameService.MakeMoveAsync(game.Id, 2, 7, 6); // AI blocks left end
        await _gameService.MakeMoveAsync(game.Id, 1, 7, 8);
        await _gameService.MakeMoveAsync(game.Id, 2, 8, 8);
        await _gameService.MakeMoveAsync(game.Id, 1, 7, 9);
        await _gameService.MakeMoveAsync(game.Id, 2, 8, 9);
        await _gameService.MakeMoveAsync(game.Id, 1, 7, 10);
        await _gameService.MakeMoveAsync(game.Id, 2, 8, 10);
        // This move should win the game (completes 5 in a row)
        await _gameService.MakeMoveAsync(game.Id, 1, 7, 11);

        // Verify game is finished
        var finishedGame = await _gameService.GetGameAsync(game.Id);
        finishedGame!.FinishedAt.Should().NotBeNull();

        // Act & Assert - Try to make a move after game is finished
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _gameService.MakeMoveAsync(game.Id, 2, 8, 12)); // Game already finished
        
        exception.Message.Should().Contain("Game already finished");
    }

    [Fact]
    public async Task ResignAsync_ShouldMarkGameAsFinished()
    {
        // Arrange
        var game = await _gameService.CreateGameAsync(new CreateGameOptions("PvE", 1, 2, 1, 60));

        // Act
        var result = await _gameService.ResignAsync(game.Id, 1);

        // Assert
        result.Should().BeTrue();
        var finishedGame = await _gameService.GetGameAsync(game.Id);
        finishedGame.Should().NotBeNull();
        finishedGame!.FinishedAt.Should().NotBeNull();
        finishedGame.WinnerId.Should().Be(2); // P2 wins when P1 resigns
        finishedGame.Result.Should().Be("P2_WIN_RESIGN");
        finishedGame.WinType.Should().Be("RESIGN");
    }

    [Fact]
    public async Task GetGameAsync_WithExistingGame_ShouldReturnGame()
    {
        // Arrange
        var game = await _gameService.CreateGameAsync(new CreateGameOptions("PvE", 1, null, 1, 60));

        // Act
        var retrievedGame = await _gameService.GetGameAsync(game.Id);

        // Assert
        retrievedGame.Should().NotBeNull();
        retrievedGame!.Id.Should().Be(game.Id);
    }

    [Fact]
    public async Task GetGameAsync_WithNonExistentGame_ShouldReturnNull()
    {
        // Act
        var game = await _gameService.GetGameAsync(999);

        // Assert
        game.Should().BeNull();
    }

    [Fact]
    public async Task GetMovesAsync_ShouldReturnAllMoves()
    {
        // Arrange
        var game = await _gameService.CreateGameAsync(new CreateGameOptions("PvE", 1, null, 1, 60));
        var move1 = await _gameService.MakeMoveAsync(game.Id, 1, 7, 7);
        var move2 = await _gameService.MakeMoveAsync(game.Id, 2, 7, 8);

        // Act
        var moves = await _gameService.GetMovesAsync(game.Id);

        // Assert
        moves.Should().HaveCount(2);
        moves.Should().Contain(m => m.Id == move1.Id && m.MoveNumber == move1.MoveNumber);
        moves.Should().Contain(m => m.Id == move2.Id && m.MoveNumber == move2.MoveNumber);
    }

    [Fact]
    public async Task EnsurePlayerAsync_WithP1_ShouldReturn1()
    {
        // Arrange
        var game = await _gameService.CreateGameAsync(new CreateGameOptions("PvP", 1, null, null, 60));

        // Act
        var player = await _gameService.EnsurePlayerAsync(game.Id, 1);

        // Assert
        player.Should().Be(1);
    }

    [Fact]
    public async Task EnsurePlayerAsync_WithNewPlayerInPvP_ShouldAssignAsP2()
    {
        // Arrange
        var game = await _gameService.CreateGameAsync(new CreateGameOptions("PvP", 1, null, null, 60));

        // Act
        var player = await _gameService.EnsurePlayerAsync(game.Id, 2);

        // Assert
        player.Should().Be(2);
        var updatedGame = await _gameService.GetGameAsync(game.Id);
        updatedGame!.P2UserId.Should().Be(2);
    }

    [Fact]
    public async Task SendMessageAsync_ShouldCreateMessage()
    {
        // Arrange
        var game = await _gameService.CreateGameAsync(new CreateGameOptions("PvP", 1, 2, null, 60));
        var content = "Hello, world!";

        // Act
        var message = await _gameService.SendMessageAsync(game.Id, 1, content);

        // Assert
        message.Should().NotBeNull();
        message.GameId.Should().Be(game.Id);
        message.SenderId.Should().Be(1);
        message.Content.Should().Be(content);
    }

    [Fact]
    public async Task GetMessagesAsync_ShouldReturnAllMessages()
    {
        // Arrange
        var game = await _gameService.CreateGameAsync(new CreateGameOptions("PvP", 1, 2, null, 60));
        await _gameService.SendMessageAsync(game.Id, 1, "Message 1");
        await _gameService.SendMessageAsync(game.Id, 2, "Message 2");

        // Act
        var messages = await _gameService.GetMessagesAsync(game.Id);

        // Assert
        messages.Should().HaveCount(2);
        messages[0].Content.Should().Be("Message 1");
        messages[1].Content.Should().Be("Message 2");
    }

    public void Dispose()
    {
        _db?.Dispose();
    }
}

