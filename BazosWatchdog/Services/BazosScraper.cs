using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace BazosWatchdog.Services;

public sealed partial class BazosScraper(HttpClient httpClient, ILogger<BazosScraper> logger)
{
    private const int MaxPages = 3;

    public async Task<IReadOnlyList<ScrapedListing>> ScrapeAsync(string searchUrl, CancellationToken cancellationToken)
    {
        var rootUri = new Uri(searchUrl);
        var listings = new List<ScrapedListing>();

        for (var page = 0; page < MaxPages; page++)
        {
            var pageUrl = GetPageUrl(searchUrl, page);
            logger.LogInformation("Scraping Bazos page {Page}: {Url}", page + 1, pageUrl);
            using var response = await httpClient.GetAsync(pageUrl, cancellationToken);
            if (page > 0 && response.StatusCode == HttpStatusCode.NotFound)
            {
                logger.LogInformation("Bazos page {Page} does not exist; pagination is complete.", page + 1);
                break;
            }

            response.EnsureSuccessStatusCode();
            var html = await response.Content.ReadAsStringAsync(cancellationToken);
            var pageListings = ParseListings(html, rootUri);
            if (pageListings.Count == 0)
            {
                break;
            }

            listings.AddRange(pageListings);
        }

        return listings;
    }

    private static string GetPageUrl(string searchUrl, int page)
    {
        if (page == 0)
        {
            return searchUrl;
        }

        var marker = searchUrl.IndexOf("/?", StringComparison.Ordinal);
        return marker < 0
            ? searchUrl
            : $"{searchUrl[..marker]}/{page * 20}/{searchUrl[marker..]}";
    }

    private static List<ScrapedListing> ParseListings(string html, Uri rootUri)
    {
        var document = new HtmlDocument();
        document.LoadHtml(html);
        var listingHeaders = document.DocumentNode.SelectNodes(
            "//div[contains(concat(' ', normalize-space(@class), ' '), ' inzeratynadpis ')]");
        var listings = new List<ScrapedListing>();

        if (listingHeaders is null)
        {
            return listings;
        }

        foreach (var header in listingHeaders)
        {
            var link = header.SelectSingleNode(".//a[contains(@href, '/inzerat/')]");
            var relativeUrl = link?.GetAttributeValue("href", string.Empty);
            if (string.IsNullOrWhiteSpace(relativeUrl))
            {
                continue;
            }

            var title = WebUtility.HtmlDecode(header.SelectSingleNode(".//h2[contains(@class, 'nadpis')]")?.InnerText ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                continue;
            }

            var siblingFields = EnumerateFollowingElements(header, 10).ToList();
            var dateText = header.SelectSingleNode(".//span[contains(@class, 'velikost10')]")?.InnerText ?? string.Empty;
            var imageUrl = header.SelectSingleNode(".//img[contains(@class, 'obrazek')]")?.GetAttributeValue("src", string.Empty);
            var description = siblingFields.FirstOrDefault(node => HasClass(node, "popis"))?.InnerText.Trim();

            listings.Add(new ScrapedListing(
                title,
                new Uri(rootUri, relativeUrl).ToString(),
                TextOf(siblingFields, "inzeratycena"),
                TextOf(siblingFields, "inzeratylok"),
                TruncateDescription(description),
                string.IsNullOrWhiteSpace(imageUrl) || imageUrl.Contains("no-image", StringComparison.OrdinalIgnoreCase)
                    ? null
                    : new Uri(rootUri, imageUrl).ToString(),
                ParsePostedDate(WebUtility.HtmlDecode(dateText))));
        }

        return listings;
    }

    private static IEnumerable<HtmlNode> EnumerateFollowingElements(HtmlNode node, int limit)
    {
        var current = node.NextSibling;
        for (var index = 0; current is not null && index < limit; current = current.NextSibling, index++)
        {
            if (current.NodeType == HtmlNodeType.Element)
            {
                yield return current;
            }
        }
    }

    private static string? TextOf(IEnumerable<HtmlNode> nodes, string className)
        => nodes.FirstOrDefault(node => HasClass(node, className)) is { } node
            ? WebUtility.HtmlDecode(node.InnerText).Trim()
            : null;

    private static bool HasClass(HtmlNode node, string className)
        => node.GetClasses().Contains(className, StringComparer.Ordinal);

    private static DateOnly? ParsePostedDate(string text)
    {
        var match = PostedDateRegex().Match(text);
        return match.Success && DateOnly.TryParseExact(
            match.Groups[1].Value,
            ["d.M.yyyy", "d.M. yyyy"],
            CultureInfo.GetCultureInfo("cs-CZ"),
            DateTimeStyles.None,
            out var date)
            ? date
            : null;
    }

    private static string? TruncateDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        var decoded = WebUtility.HtmlDecode(description).Trim();
        return decoded[..Math.Min(decoded.Length, 500)];
    }

    [GeneratedRegex(@"\[(\d{1,2}\.\d{1,2}\.\s*\d{4})\]")]
    private static partial Regex PostedDateRegex();
}

public sealed record ScrapedListing(
    string Title,
    string ListingUrl,
    string? Price,
    string? Location,
    string? Description,
    string? ImageUrl,
    DateOnly? DatePosted);
