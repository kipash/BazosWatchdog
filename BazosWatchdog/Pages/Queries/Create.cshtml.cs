using System.ComponentModel.DataAnnotations;
using BazosWatchdog.Data;
using BazosWatchdog.Models;
using BazosWatchdog.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BazosWatchdog.Pages.Queries;

public sealed class CreateModel(
    WatchdogDbContext database,
    ScrapeService scrapeService) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!Uri.TryCreate(Input.SearchUrl, UriKind.Absolute, out var uri)
            || (uri.Host != "bazos.cz" && !uri.Host.EndsWith(".bazos.cz", StringComparison.OrdinalIgnoreCase)))
        {
            ModelState.AddModelError("Input.SearchUrl", "Enter a complete URL from a Bazos.cz category or search page.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var query = new UserQuery
        {
            Name = Input.Name.Trim(),
            SearchUrl = Input.SearchUrl.Trim()
        };
        database.UserQueries.Add(query);

        try
        {
            await database.SaveChangesAsync();
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException)
        {
            ModelState.AddModelError("Input.SearchUrl", "This Bazos.cz URL is already being watched.");
            return Page();
        }

        await scrapeService.RunAsync(HttpContext.RequestAborted, query.Id, sendNotifications: false);
        return RedirectToPage("/Index");
    }

    public sealed class InputModel
    {
        [Required, StringLength(120)]
        [Display(Name = "Name")]
        public string Name { get; set; } = string.Empty;

        [Required, StringLength(2_000), Display(Name = "Bazos.cz search URL")]
        public string SearchUrl { get; set; } = string.Empty;
    }
}
