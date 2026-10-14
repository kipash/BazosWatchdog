using BazosWatchdog.Data;
using BazosWatchdog.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var databasePath = builder.Configuration["Database:Path"] ?? "data/bazos-watchdog.db";
var databaseDirectory = Path.GetDirectoryName(Path.GetFullPath(databasePath));
Directory.CreateDirectory(databaseDirectory!);

builder.Services.AddDbContext<WatchdogDbContext>(options =>
    options.UseSqlite($"Data Source={databasePath}"));
builder.Services.AddRazorPages();
builder.Services.AddHttpClient<BazosScraper>(client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd(
        "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 Chrome/120.0 Safari/537.36");
    client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("cs-CZ,cs;q=0.9,en;q=0.8");
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddHttpClient<NtfyNotifier>(client => client.Timeout = TimeSpan.FromSeconds(15));
builder.Services.AddScoped<ScrapeService>();
builder.Services.AddHostedService<ScrapeScheduler>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var database = scope.ServiceProvider.GetRequiredService<WatchdogDbContext>();
    await database.Database.EnsureCreatedAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
