using Caro.Services;
using FluentAssertions;

namespace Caro.Tests.Services;

public class AiServiceTests
{
    private readonly AiService _aiService;

    public AiServiceTests()
    {
        _aiService = new AiService();
    }

    [Fact]
    public void ChooseMove_WithEmptyBoard_ShouldReturnValidMove()
    {
        // Arrange
        var board = new int[15, 15];
        var currentPlayer = 2; // AI is player 2

        // Act
        var move = _aiService.ChooseMove(board, currentPlayer, AiDifficulty.Easy);

        // Assert
        move.Should().NotBeNull();
        move.X.Should().BeInRange(0, 15);
        move.Y.Should().BeInRange(0, 15);
    }

    [Fact]
    public void ChooseMove_WithEasyDifficulty_ShouldReturnMove()
    {
        // Arrange
        var board = new int[15, 15];
        board[7, 7] = 1; // Player 1 move
        var currentPlayer = 2;

        // Act
        var move = _aiService.ChooseMove(board, currentPlayer, AiDifficulty.Easy);

        // Assert
        move.Should().NotBeNull();
        move.X.Should().BeInRange(0, 15);
        move.Y.Should().BeInRange(0, 15);
        board[move.X, move.Y].Should().Be(0); // Should be empty
    }

    [Fact]
    public void ChooseMove_WithMediumDifficulty_ShouldReturnMove()
    {
        // Arrange
        var board = new int[15, 15];
        board[7, 7] = 1;
        var currentPlayer = 2;

        // Act
        var move = _aiService.ChooseMove(board, currentPlayer, AiDifficulty.Normal);

        // Assert
        move.Should().NotBeNull();
        move.X.Should().BeInRange(0, 15);
        move.Y.Should().BeInRange(0, 15);
    }

    [Fact]
    public void ChooseMove_WithHardDifficulty_ShouldReturnMove()
    {
        // Arrange
        var board = new int[15, 15];
        board[7, 7] = 1;
        var currentPlayer = 2;

        // Act
        var move = _aiService.ChooseMove(board, currentPlayer, AiDifficulty.Hard);

        // Assert
        move.Should().NotBeNull();
        move.X.Should().BeInRange(0, 15);
        move.Y.Should().BeInRange(0, 15);
    }

    [Fact]
    public void ChooseMove_WithUltimateDifficulty_ShouldReturnMove()
    {
        // Arrange
        var board = new int[15, 15];
        board[7, 7] = 1;
        var currentPlayer = 2;

        // Act
        var move = _aiService.ChooseMove(board, currentPlayer, AiDifficulty.Ultimate);

        // Assert
        move.Should().NotBeNull();
        move.X.Should().BeInRange(0, 15);
        move.Y.Should().BeInRange(0, 15);
    }

    [Fact]
    public void ChooseMove_ShouldNotPlaceOnOccupiedCell()
    {
        // Arrange
        var board = new int[15, 15];
        // Fill center area
        for (int i = 6; i <= 8; i++)
        {
            for (int j = 6; j <= 8; j++)
            {
                board[i, j] = (i + j) % 2 == 0 ? 1 : 2;
            }
        }
        var currentPlayer = 2;

        // Act
        var move = _aiService.ChooseMove(board, currentPlayer, AiDifficulty.Easy);

        // Assert
        board[move.X, move.Y].Should().Be(0); // Should be empty
    }

    [Fact]
    public void ChooseMove_WithWinningMove_ShouldBlockOrWin()
    {
        // Arrange
        var board = new int[15, 15];
        // Player 1 has 4 in a row
        board[7, 7] = 1;
        board[7, 8] = 1;
        board[7, 9] = 1;
        board[7, 10] = 1;
        // AI should block
        var currentPlayer = 2;

        // Act
        var move = _aiService.ChooseMove(board, currentPlayer, AiDifficulty.Easy);

        // Assert
        move.Should().NotBeNull();
        // Should block at (7, 6) or (7, 11)
        (move.X == 7 && (move.Y == 6 || move.Y == 11)).Should().BeTrue();
    }
}

