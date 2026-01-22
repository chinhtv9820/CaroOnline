using Caro.Api.Controllers;
using Caro.Core.Entities;
using Caro.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Caro.Tests.Controllers;

public class GamesControllerTests
{
    private readonly Mock<IGameService> _gameServiceMock;
    private readonly Mock<IAiService> _aiServiceMock;
    private readonly GamesController _controller;

    public GamesControllerTests()
    {
        _gameServiceMock = new Mock<IGameService>();
        _aiServiceMock = new Mock<IAiService>();
        _controller = new GamesController(_gameServiceMock.Object, _aiServiceMock.Object);
    }

    [Fact]
    public async Task List_ShouldReturnOk()
    {
        // Arrange
        var games = new List<Game>
        {
            new Game { Id = 1, Mode = "PvE", CreatedAt = DateTime.UtcNow },
            new Game { Id = 2, Mode = "PvP", CreatedAt = DateTime.UtcNow }
        };

        _gameServiceMock.Setup(x => x.GetGamesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(games);

        // Act
        var result = await _controller.List(CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Get_WithExistingGame_ShouldReturnOk()
    {
        // Arrange
        var game = new Game
        {
            Id = 1,
            Mode = "PvE",
            P1UserId = 1,
            CurrentTurn = 1,
            TimeControlSeconds = 60
        };
        var moves = new List<Move>();

        _gameServiceMock.Setup(x => x.GetGameAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(game);
        _gameServiceMock.Setup(x => x.GetMovesAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(moves);

        // Act
        var result = await _controller.Get(1, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Get_WithNonExistentGame_ShouldReturnNotFound()
    {
        // Arrange
        _gameServiceMock.Setup(x => x.GetGameAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Game?)null);

        // Act
        var result = await _controller.Get(999, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task MakeMove_WithValidMove_ShouldReturnOk()
    {
        // Arrange
        var game = new Game
        {
            Id = 1,
            Mode = "PvE",
            P1UserId = 1,
            CurrentTurn = 1,
            FinishedAt = null
        };
        var move = new Move
        {
            Id = 1,
            GameId = 1,
            Player = 1,
            X = 7,
            Y = 7,
            MoveNumber = 1,
            CreatedAt = DateTime.UtcNow
        };

        _gameServiceMock.Setup(x => x.GetGameAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(game);
        _gameServiceMock.Setup(x => x.MakeMoveAsync(1, 1, 7, 7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(move);
        _gameServiceMock.Setup(x => x.GetGameAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(game);

        var request = new GamesController.MakeMoveRequest(7, 7, 1);

        // Act
        var result = await _controller.MakeMove(1, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task MakeMove_WithInvalidMove_ShouldReturnBadRequest()
    {
        // Arrange
        var game = new Game
        {
            Id = 1,
            Mode = "PvE",
            CurrentTurn = 1
        };

        _gameServiceMock.Setup(x => x.GetGameAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(game);
        _gameServiceMock.Setup(x => x.MakeMoveAsync(1, 1, 7, 7, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Not your turn"));

        var request = new GamesController.MakeMoveRequest(7, 7, 1);

        // Act
        var result = await _controller.MakeMove(1, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Create_WithPvEMode_ShouldReturnOk()
    {
        // Arrange
        var request = new GamesController.CreateRequest("PvE", 1, null, 1, 60);
        var game = new Game
        {
            Id = 1,
            Mode = "PvE",
            P1UserId = 1,
            PveDifficulty = 1,
            TimeControlSeconds = 60
        };

        _gameServiceMock.Setup(x => x.CreateGameAsync(It.IsAny<CreateGameOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(game);

        // Act
        var result = await _controller.Create(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Create_WithPvPMode_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new GamesController.CreateRequest("PvP", 1, null, null, 60);

        // Act
        var result = await _controller.Create(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Resign_WithValidGame_ShouldReturnOk()
    {
        // Arrange
        var game = new Game
        {
            Id = 1,
            Mode = "PvE",
            P1UserId = 1,
            P2UserId = 2,
            FinishedAt = null
        };
        var finishedGame = new Game
        {
            Id = 1,
            Mode = "PvE",
            FinishedAt = DateTime.UtcNow,
            Result = "P2_WIN_RESIGN"
        };

        _gameServiceMock.SetupSequence(x => x.GetGameAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(game)  // First call - check if game exists
            .ReturnsAsync(finishedGame); // Second call - get updated game
        _gameServiceMock.Setup(x => x.ResignAsync(1, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Resign(1, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Resign_WithFinishedGame_ShouldReturnBadRequest()
    {
        // Arrange
        var game = new Game
        {
            Id = 1,
            FinishedAt = DateTime.UtcNow
        };

        _gameServiceMock.Setup(x => x.GetGameAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(game);

        // Act
        var result = await _controller.Resign(1, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }
}

