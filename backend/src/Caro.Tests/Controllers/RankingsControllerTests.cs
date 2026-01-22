using Caro.Api.Controllers;
using Caro.Core.Entities;
using Caro.Infrastructure;
using Caro.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Caro.Tests.Controllers;

public class RankingsControllerTests : IDisposable
{
    private readonly CaroDbContext _db;
    private readonly RankingsController _controller;

    public RankingsControllerTests()
    {
        _db = TestHelper.CreateInMemoryDbContext();
        _controller = new RankingsController(_db);
    }

    [Fact]
    public async Task GetRankings_WithDayPeriod_ShouldReturnOk()
    {
        // Arrange
        var user1 = new User { Username = "user1", Email = "user1@test.com", PasswordHash = "hash", CreatedAt = DateTime.UtcNow };
        var user2 = new User { Username = "user2", Email = "user2@test.com", PasswordHash = "hash", CreatedAt = DateTime.UtcNow };
        _db.Users.AddRange(user1, user2);
        await _db.SaveChangesAsync();

        var game1 = new Game
        {
            Mode = "PvP",
            P1UserId = user1.Id,
            P2UserId = user2.Id,
            WinnerId = user1.Id,
            FinishedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        _db.Games.Add(game1);
        await _db.SaveChangesAsync();

        // Act
        var result = await _controller.GetRankings("day", CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetRankings_WithInvalidPeriod_ShouldReturnBadRequest()
    {
        // Act
        var result = await _controller.GetRankings("invalid", CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetRankings_ShouldOnlyCountPvPGames()
    {
        // Arrange
        var user1 = new User { Username = "user1", Email = "user1@test.com", PasswordHash = "hash", CreatedAt = DateTime.UtcNow };
        _db.Users.Add(user1);
        await _db.SaveChangesAsync();

        // PvE game should not count
        var pveGame = new Game
        {
            Mode = "PvE",
            P1UserId = user1.Id,
            WinnerId = user1.Id,
            FinishedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        _db.Games.Add(pveGame);
        await _db.SaveChangesAsync();

        // Act
        var result = await _controller.GetRankings("day", CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var rankings = okResult!.Value as IEnumerable<dynamic>;
        rankings.Should().BeEmpty(); // PvE games don't count
    }

    [Fact]
    public async Task GetRankings_ShouldOrderByScoreDescending()
    {
        // Arrange
        var user1 = new User { Username = "user1", Email = "user1@test.com", PasswordHash = "hash", CreatedAt = DateTime.UtcNow };
        var user2 = new User { Username = "user2", Email = "user2@test.com", PasswordHash = "hash", CreatedAt = DateTime.UtcNow };
        _db.Users.AddRange(user1, user2);
        await _db.SaveChangesAsync();

        // User1 wins 2 games
        for (int i = 0; i < 2; i++)
        {
            _db.Games.Add(new Game
            {
                Mode = "PvP",
                P1UserId = user1.Id,
                P2UserId = user2.Id,
                WinnerId = user1.Id,
                FinishedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            });
        }

        // User2 wins 1 game
        _db.Games.Add(new Game
        {
            Mode = "PvP",
            P1UserId = user1.Id,
            P2UserId = user2.Id,
            WinnerId = user2.Id,
            FinishedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        // Act
        var result = await _controller.GetRankings("day", CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var rankings = okResult!.Value as IEnumerable<object>;
        var rankingsList = rankings!.ToList();
        rankingsList.Should().HaveCount(2);
        
        // Use reflection to access properties
        var firstRanking = rankingsList[0];
        var firstRank = firstRanking.GetType().GetProperty("Rank")?.GetValue(firstRanking);
        var firstScore = firstRanking.GetType().GetProperty("Score")?.GetValue(firstRanking);
        firstRank.Should().Be(1);
        firstScore.Should().Be(2); // User1 has 2 wins
        
        var secondRanking = rankingsList[1];
        var secondRank = secondRanking.GetType().GetProperty("Rank")?.GetValue(secondRanking);
        var secondScore = secondRanking.GetType().GetProperty("Score")?.GetValue(secondRanking);
        secondRank.Should().Be(2);
        secondScore.Should().Be(1); // User2 has 1 win
    }

    public void Dispose()
    {
        _db?.Dispose();
    }
}

