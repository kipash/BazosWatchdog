using BazosWatchdog.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BazosWatchdog.Pages;

public class IndexModel : PageModel
{
    private readonly WatchdogDbContext _database;

    public IndexModel(WatchdogDbContext database) => _database = database;

    public List<QuerySummary> Queries { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var cutoff = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7));
        Queries = await _database.UserQueries
            .OrderBy(query => query.Name)
            .Select(query => new QuerySummary(
                query.Id,
                query.Name,
                query.SearchUrl,
                query.Results.Count(result => result.DatePosted >= cutoff)))
            .ToListAsync();
    }

    public sealed record QuerySummary(int Id, string Name, string SearchUrl, int RecentListingCount);
}
