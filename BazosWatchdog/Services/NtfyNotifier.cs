using System.Net.Http.Headers;
using BazosWatchdog.Models;

namespace BazosWatchdog.Services;

public sealed class NtfyNotifier(HttpClient httpClient, IConfiguration configuration, ILogger<NtfyNotifier> logger)
{
    public async Task<bool> NotifyAsync(QueryResultEntry listing, CancellationToken cancellationToken)
    {
        var ntfyUrl = configuration["Ntfy:Url"];
        if (string.IsNullOrWhiteSpace(ntfyUrl))
        {
            logger.LogWarning("NTFY_URL/Ntfy:Url is not configured; notification remains pending.");
            return false;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, ntfyUrl)
        {
            Content = new StringContent(BuildMessage(listing))
        };
        request.Headers.Add("Click", listing.ListingUrl);
        request.Headers.Add("Tags", "loudspeaker");
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("text/plain") { CharSet = "utf-8" };

        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            logger.LogError("ntfy returned {StatusCode} for listing {ListingUrl}", response.StatusCode, listing.ListingUrl);
            return false;
        }
        catch (HttpRequestException exception)
        {
            logger.LogError(exception, "Unable to notify ntfy about {ListingUrl}", listing.ListingUrl);
            return false;
        }
    }

    private static string BuildMessage(QueryResultEntry listing)
        => $"{listing.Title}\n{listing.Price ?? "Price unavailable"}{(string.IsNullOrWhiteSpace(listing.Location) ? string.Empty : $" · {listing.Location}")}\n{listing.ListingUrl}";
}
