using PropertyExtractor.Domain.Models;
using PropertyExtractor.Domain.Ports;
using Microsoft.Extensions.Logging;

namespace PropertyExtractor.Application.UseCases;

/// <summary>
/// Caso de uso: "Extraer inmuebles de un portal y generar un reporte".
/// No sabe si el scraper usa Playwright, ni si el reporte es un
/// Excel o un CSV — solo conoce las interfaces (puertos) del dominio.
/// Esto permite testear el caso de uso con implementaciones falsas (fakes)
/// sin abrir un navegador real ni escribir archivos en disco.
/// </summary>
public class ExtractPropertiesUseCase
{
    private readonly IPropertyScraper _scraper;
    private readonly IReportWriter _writer;
    private readonly ILogger<ExtractPropertiesUseCase> _logger;

    public ExtractPropertiesUseCase(
        IPropertyScraper scraper,
        IReportWriter writer,
        ILogger<ExtractPropertiesUseCase> logger)
    {
        _scraper = scraper;
        _writer = writer;
        _logger = logger;
    }

    public async Task<(string ReportPath, ExtractionResult Result)> ExecuteAsync(
        string city, int resultCount, decimal highlightThreshold)
    {
        _logger.LogInformation(
            "Iniciando caso de uso: extraer {ResultCount} inmuebles de '{City}' (umbral={Threshold}€)",
            resultCount, city, highlightThreshold);

        var properties = await _scraper.ExtractPropertiesAsync(city, resultCount);

        var result = new ExtractionResult(properties, city, resultCount);
        if (!result.IsComplete)
        {
            _logger.LogWarning(
                "Extracción parcial: se obtuvieron {Extracted} de {Expected} inmuebles esperados",
                result.ExtractedCount, result.ExpectedCount);
        }

        var reportPath = _writer.Write(properties, highlightThreshold);

        _logger.LogInformation("Caso de uso finalizado. Reporte en: {Path}", reportPath);
        return (reportPath, result);
    }
}