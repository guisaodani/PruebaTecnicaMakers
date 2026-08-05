using System.Globalization;
using System.Text.RegularExpressions;
using PropertyExtractor.Domain.Exceptions;
using PropertyExtractor.Domain.Models;
using PropertyExtractor.Domain.Ports;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace PropertyExtractor.Infrastructure.Scraping;

/// <summary>
/// Implementación concreta de IPropertyScraper para Fotocasa, usando Playwright.
/// Esta es la ÚNICA capa que sabe que existe Playwright, HTML, selectores, etc.
///
/// Decisión de diseño: Fotocasa es una SPA (Next.js) con clases CSS
/// generadas/hasheadas que cambian entre despliegues. Por eso NO se ancla la
/// extracción a nombres de clase, sino a patrones estables:
///   1. La URL de cada anuncio (/alquiler/vivienda/{ciudad}/.../{id}/d)
///   2. Expresiones regulares sobre el texto visible (precio, m²)
/// Esto hace el scraper resiliente a rediseños visuales del portal.
/// </summary>
public class PlaywrightFotocasaScraper : IPropertyScraper
{
    private static readonly Regex DetailLinkPattern = new(@"/alquiler/vivienda/[^""']+/\d+/d", RegexOptions.Compiled);
    private static readonly Regex PricePattern = new(@"([\d.]+(?:,\d+)?)\s*€", RegexOptions.Compiled);
    private static readonly Regex AreaPattern = new(@"(\d+)\s*m²", RegexOptions.Compiled);

    private readonly AppConfig _config;
    private readonly ILogger<PlaywrightFotocasaScraper> _logger;

    public PlaywrightFotocasaScraper(AppConfig config, ILogger<PlaywrightFotocasaScraper> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task<List<Property>> ExtractPropertiesAsync(string city, int resultCount)
    {
        var p = _config.Portal;
        var url = $"{p.BaseUrl}/es/{p.Operation}/{p.PropertyType}/{city}/{p.Zone}/l";

        var retries = _config.Search.Retries;
        var retryDelaySeconds = _config.Search.RetryDelaySeconds;

        Exception? lastError = null;

        for (int attempt = 1; attempt <= retries; attempt++)
        {
            _logger.LogInformation("Intento {Attempt}/{Total} de scraping en {Url}", attempt, retries, url);
            try
            {
                var results = await ScrapeWithBrowserAsync(url, resultCount);
                if (results.Count == 0)
                {
                    throw new ScraperException(
                        "No se extrajo ningún inmueble. Posible cambio de estructura del portal.");
                }

                _logger.LogInformation("Scraping exitoso: {Count} inmuebles extraídos", results.Count);
                return results;
            }
            catch (Exception ex) when (ex is TimeoutException or ScraperException or PlaywrightException)
            {
                lastError = ex;
                _logger.LogError(ex, "Fallo en intento {Attempt}", attempt);
                if (attempt < retries)
                {
                    await Task.Delay(TimeSpan.FromSeconds(retryDelaySeconds));
                }
            }
        }

        throw new ScraperException(
            $"No fue posible completar el scraping tras {retries} intentos. Último error: {lastError?.Message}",
            lastError ?? new Exception("Error desconocido"));
    }

    private async Task<List<Property>> ScrapeWithBrowserAsync(string url, int resultCount)
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = _config.Browser.Headless,
        });

        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            UserAgent = string.IsNullOrWhiteSpace(_config.Browser.UserAgent) ? null : _config.Browser.UserAgent,
            Locale = _config.Browser.Locale,
            ViewportSize = new ViewportSize { Width = 1366, Height = 900 },
        });

        var page = await context.NewPageAsync();
        return await ScrapePageAsync(page, url, resultCount);
    }

    private async Task AcceptCookiesAsync(IPage page)
    {
        string[] selectors =
        {
            "#onetrust-accept-btn-handler",
            "button:has-text('Aceptar')",
            "button:has-text('Aceptar todas')",
        };

        foreach (var selector in selectors)
        {
            try
            {
                await page.Locator(selector).First.ClickAsync(new LocatorClickOptions { Timeout = 3000 });
                _logger.LogInformation("Banner de cookies cerrado con selector: {Selector}", selector);
                return;
            }
            catch (TimeoutException)
            {
                continue;
            }
        }

        _logger.LogInformation("No se encontró banner de cookies visible (o ya estaba cerrado).");
    }

    private async Task<List<Property>> ScrapePageAsync(IPage page, string url, int resultCount)
    {
        var searchCfg = _config.Search;

        await page.GotoAsync(url, new PageGotoOptions
        {
            Timeout = searchCfg.NavigationTimeoutMs,
            WaitUntil = WaitUntilState.DOMContentLoaded,
        });

        await AcceptCookiesAsync(page);

        var linkSelector = $"a[href*='/{_config.Portal.Operation}/vivienda/']";
        await page.WaitForSelectorAsync(linkSelector, new PageWaitForSelectorOptions
        {
            Timeout = searchCfg.ElementTimeoutMs,
        });

        var anchors = page.Locator(linkSelector);
        var totalAnchors = await anchors.CountAsync();
        _logger.LogInformation("Se encontraron {Total} links candidatos de inmuebles", totalAnchors);

        var results = new List<Property>();
        var seenHrefs = new HashSet<string>();

        for (int i = 0; i < totalAnchors; i++)
        {
            if (results.Count >= resultCount) break;

            try
            {
                var anchor = anchors.Nth(i);
                var href = await anchor.GetAttributeAsync("href") ?? "";

                if (!DetailLinkPattern.IsMatch(href) || seenHrefs.Contains(href))
                    continue;

                seenHrefs.Add(href);

                string cardText;
                try
                {
                    var container = anchor.Locator("xpath=ancestor::article[1] | ancestor::li[1]").First;
                    cardText = await container.InnerTextAsync(new LocatorInnerTextOptions { Timeout = 3000 });
                }
                catch (TimeoutException)
                {
                    var fallback = anchor.Locator("xpath=ancestor::*[4]").First;
                    cardText = await fallback.InnerTextAsync(new LocatorInnerTextOptions { Timeout = 3000 });
                }
                var property = ParseCard(href, cardText);
                if (property.HasIncompleteData)
                {
                    _logger.LogWarning("Tarjeta {Index} con datos incompletos: {Errors}",
                        i, string.Join(", ", property.Errors));
                }

                results.Add(property);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando tarjeta índice {Index}", i);
                continue; // se aísla el error por tarjeta, no se aborta todo el proceso
            }
        }

        return results;
    }

    private Property ParseCard(string href, string text)
    {
        var baseUrl = _config.Portal.BaseUrl;
        var property = new Property
        {
            Link = href.StartsWith("http") ? href : $"{baseUrl}{href}",
        };

        var priceMatch = PricePattern.Match(text);
        if (priceMatch.Success)
        {
            var raw = priceMatch.Groups[1].Value.Replace(".", "").Replace(",", ".");
            if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var price))
            {
                property.Price = price;
            }
            else
            {
                property.Errors.Add("No se pudo convertir el precio a número");
            }
        }
        else
        {
            property.Errors.Add("Precio no encontrado en la tarjeta");
        }

        var areaMatch = AreaPattern.Match(text);
        if (areaMatch.Success)
        {
            if (double.TryParse(areaMatch.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var area))
            {
                property.AreaM2 = area;
            }
            else
            {
                property.Errors.Add("No se pudo convertir el área a número");
            }
        }
        else
        {
            property.Errors.Add("Área (m²) no encontrada en la tarjeta");
        }

        var lines = text.Split('\n')
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();

        var titleCandidates = lines
            .Where(l => !PricePattern.IsMatch(l) && l.Length > 8 && !l.All(char.IsDigit))
            .ToList();

        if (titleCandidates.Count > 0)
        {
            property.Name = titleCandidates[0][..Math.Min(150, titleCandidates[0].Length)];
        }
        else
        {
            property.Errors.Add("Nombre de la propiedad no encontrado");
        }

        var readableCity = _config.Portal.City.Replace("-", " ");
        var addressCandidates = lines
            .Where(l => l.Contains(readableCity, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (addressCandidates.Count > 0)
        {
            property.Address = addressCandidates[0][..Math.Min(150, addressCandidates[0].Length)];
        }
        else if (titleCandidates.Count > 1)
        {
            property.Address = titleCandidates[1][..Math.Min(150, titleCandidates[1].Length)];
        }
        else
        {
            property.Errors.Add("Dirección/barrio no encontrado");
        }

        return property;
    }
}