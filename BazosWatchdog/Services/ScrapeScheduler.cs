namespace BazosWatchdog.Services;

public sealed class ScrapeScheduler(
    IServiceScopeFactory scopeFactory,
    ILogger<ScrapeScheduler> logger) : BackgroundService
{
    private static readonly TimeOnly[] RunTimes = [new(8, 0), new(14, 0), new(20, 0)];
    private static readonly TimeZoneInfo PragueTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Prague");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = GetDelayUntilNextRun();
            logger.LogInformation("Next Bazos scrape is scheduled in {Delay}", delay);
            await Task.Delay(delay, stoppingToken);
            await RunOnceAsync(stoppingToken);
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            await scope.ServiceProvider.GetRequiredService<ScrapeService>().RunAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Scheduled Bazos scrape failed");
        }
    }

    private static TimeSpan GetDelayUntilNextRun()
    {
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, PragueTimeZone);
        var next = RunTimes.Select(time => now.Date.Add(time.ToTimeSpan())).FirstOrDefault(candidate => candidate > now);
        if (next == default)
        {
            next = now.Date.AddDays(1).Add(RunTimes[0].ToTimeSpan());
        }

        return next - now;
    }
}
