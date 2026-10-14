namespace BazosWatchdog.Models;

public sealed class UserQuery
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SearchUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<QueryResultEntry> Results { get; set; } = new List<QueryResultEntry>();
}
