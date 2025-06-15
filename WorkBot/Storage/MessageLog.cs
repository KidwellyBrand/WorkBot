namespace WorkBot.Storage;

public class MessageLog
{
    public Guid Id { get; set; }
    public long ChatId { get; set; }
    public string? Username { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public  string OrderType { get; set; }
}
