namespace Caro.Core.Entities;

public class Ranking
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string PeriodType { get; set; } = string.Empty; // "day", "week", "month", "year"
    public int Rank { get; set; }
    public int Score { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
}

