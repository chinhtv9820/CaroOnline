using Caro.Core.Entities;
using Caro.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Caro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
/// <summary>
/// Controller trả về bảng xếp hạng dựa trên số trận thắng PvP trong các khoảng thời gian khác nhau.
/// </summary>
public class RankingsController : ControllerBase
{
    private readonly CaroDbContext _db;

    public RankingsController(CaroDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Lấy bảng xếp hạng theo period (day/week/month/year)
    /// </summary>
    /// <param name="period">Chuỗi day/week/month/year (không phân biệt hoa thường)</param>
    [HttpGet("{period}")]
    public async Task<IActionResult> GetRankings(string period, CancellationToken ct = default)
    {
        // Validate period
        if (!new[] { "day", "week", "month", "year" }.Contains(period.ToLower()))
        {
            return BadRequest(new { message = "Invalid period. Must be: day, week, month, or year" });
        }

        // Calculate date range based on period
        var now = DateTime.UtcNow;
        DateTime startDate = period.ToLower() switch
        {
            "day" => now.Date,
            "week" => now.AddDays(-(int)now.DayOfWeek).Date, // Start of week (Sunday)
            "month" => new DateTime(now.Year, now.Month, 1),
            "year" => new DateTime(now.Year, 1, 1),
            _ => now.Date
        };

        // Get all finished games in the period (only PvP games count for rankings)
        var games = await _db.Games
            .Where(g => g.Mode == "PvP" 
                     && g.FinishedAt != null 
                     && g.FinishedAt >= startDate
                     && g.FinishedAt < now
                     && g.WinnerId != null)
            .ToListAsync(ct);

        // Calculate scores for each user
        // Score = number of wins in the period
        var userScores = games
            .Where(g => g.WinnerId.HasValue)
            .GroupBy(g => g.WinnerId!.Value)
            .Select(g => new
            {
                UserId = g.Key,
                Score = g.Count()
            })
            .ToList();

        // Get user information
        var userIds = userScores.Select(s => s.UserId).ToList();
        var users = await _db.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u, ct);

        // Build rankings with user info
        var rankings = userScores
            .OrderByDescending(s => s.Score)
            .ThenBy(s => s.UserId) // Tie-breaker: lower user ID first
            .Select((score, index) => new
            {
                Rank = index + 1,
                UserId = score.UserId,
                Username = users.ContainsKey(score.UserId) ? users[score.UserId].Username : $"User{score.UserId}",
                Score = score.Score
            })
            .ToList();

        return Ok(rankings);
    }
}

