namespace Caro.Core.Entities;

public class Message
{
    public int Id { get; set; }
    public int GameId { get; set; }
    public int SenderId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

