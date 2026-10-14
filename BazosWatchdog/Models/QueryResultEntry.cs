namespace BazosWatchdog.Models;

public sealed class QueryResultEntry
{
    public int Id { get; set; }
    public int UserQueryId { get; set; }
    public UserQuery UserQuery { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string NormalizedTitle { get; set; } = string.Empty;
    public string ListingUrl { get; set; } = string.Empty;
    public string? Price { get; set; }
    public string? Location { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public DateOnly? DatePosted { get; set; }
    public DateTime FirstSeenAt { get; set; } = DateTime.UtcNow;
    public bool Notified { get; set; }
}
