using System.Text;
using BazosWatchdog.Data;
using BazosWatchdog.Models;
using Microsoft.EntityFrameworkCore;

namespace BazosWatchdog.Services;

public sealed class ScrapeService(
    WatchdogDbContext database,
    BazosScraper scraper,
    NtfyNotifier notifier,
    ILogger<ScrapeService> logger)
{
    public async Task RunAsync(
        CancellationToken cancellationToken,
        int? userQueryId = null,
        bool sendNotifications = true)
    {
        await DeleteExpiredResultsAsync(cancellationToken);
        var queries = await database.UserQueries.AsNoTracking()
            .Where(query => userQueryId == null || query.Id == userQueryId)
            .ToListAsync(cancellationToken);

        foreach (var query in queries)
        {
            try
            {
                var listings = await scraper.ScrapeAsync(query.SearchUrl, cancellationToken);
                var seenTitles = await database.QueryResultEntries
                    .Where(entry => entry.UserQueryId == query.Id)
                    .Select(entry => entry.NormalizedTitle)
                    .ToHashSetAsync(cancellationToken);

                foreach (var listing in listings)
                {
                    var normalizedTitle = NormalizeTitle(listing.Title);
                    if (!seenTitles.Add(normalizedTitle))
                    {
                        continue;
                    }

                    database.QueryResultEntries.Add(new QueryResultEntry
                    {
                        UserQueryId = query.Id,
                        Title = listing.Title,
                        NormalizedTitle = normalizedTitle,
                        ListingUrl = listing.ListingUrl,
                        Price = listing.Price,
                        Location = listing.Location,
                        Description = listing.Description,
                        ImageUrl = listing.ImageUrl,
                        DatePosted = listing.DatePosted,
                        Notified = !sendNotifications
                    });
                }

                await database.SaveChangesAsync(cancellationToken);
            }
            catch (HttpRequestException exception)
            {
                logger.LogError(exception, "Failed to scrape query {QueryName}", query.Name);
            }
            catch (DbUpdateException exception)
            {
                database.ChangeTracker.Clear();
                logger.LogError(exception, "Failed to store listings for query {QueryName}", query.Name);
            }
        }

        if (!sendNotifications)
        {
            return;
        }

        var pendingNotifications = await database.QueryResultEntries
            .Where(entry => !entry.Notified)
            .OrderBy(entry => entry.FirstSeenAt)
            .ToListAsync(cancellationToken);

        foreach (var entry in pendingNotifications)
        {
            if (await notifier.NotifyAsync(entry, cancellationToken))
            {
                entry.Notified = true;
                await database.SaveChangesAsync(cancellationToken);
            }
        }
    }

    private async Task DeleteExpiredResultsAsync(CancellationToken cancellationToken)
    {
        var cutoff = DateTime.UtcNow.AddDays(-14);
        var deleted = await database.QueryResultEntries
            .Where(entry => entry.FirstSeenAt < cutoff)
            .ExecuteDeleteAsync(cancellationToken);
        if (deleted > 0)
        {
            logger.LogInformation("Deleted {Count} result entries older than 14 days", deleted);
        }
    }

    private static string NormalizeTitle(string title)
        => string.Join(' ', title.Trim().ToUpperInvariant().Normalize(NormalizationForm.FormKC).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
