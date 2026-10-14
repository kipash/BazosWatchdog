using BazosWatchdog.Data;
using BazosWatchdog.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BazosWatchdog.Pages.Queries;

public sealed class DetailsModel(WatchdogDbContext database) : PageModel
{
    public UserQuery Query { get; private set; } = null!;
    public List<QueryResultEntry> Results { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var query = await database.UserQueries.AsNoTracking().SingleOrDefaultAsync(query => query.Id == id);
        if (query is null)
        {
            return NotFound();
        }

        Query = query;
        var cutoff = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7));
        Results = await database.QueryResultEntries.AsNoTracking()
            .Where(result => result.UserQueryId == id && result.DatePosted >= cutoff)
            .OrderByDescending(result => result.DatePosted)
            .ToListAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var query = await database.UserQueries
            .Include(userQuery => userQuery.Results)
            .SingleOrDefaultAsync(userQuery => userQuery.Id == id);
        if (query is null)
        {
            return NotFound();
        }

        database.UserQueries.Remove(query);
        await database.SaveChangesAsync();
        return RedirectToPage("/Index");
    }
}
