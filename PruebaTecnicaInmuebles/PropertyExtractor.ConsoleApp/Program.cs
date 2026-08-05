using PropertyExtractor.Application.UseCases;
using PropertyExtractor.Domain.Exceptions;
using PropertyExtractor.Domain.Models;
using PropertyExtractor.Domain.Ports;
using PropertyExtractor.Infrastructure.Config;
using PropertyExtractor.Infrastructure.Reporting;
using PropertyExtractor.Infrastructure.Scraping;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// -----------------------------------------------------------------------
// Composition root: el ÚNICO archivo que conoce todas las capas y las
// conecta mediante inyección de dependencias. Nada de lógica de negocio
// vive aquí.
// -----------------------------------------------------------------------

var configPath = args.Length > 0 ? args[0] : "config.json";

// 1. Configuración (Infrastructure)
AppConfig config;
try
{
    config = new JsonConfigProvider(configPath).Get();
}
catch (DomainException ex)
{
    Console.Error.WriteLine($"[ERROR] {ex.Message}");
    Environment.Exit(1);
    return;
}

// 2. Contenedor de dependencias + logging
var services = new ServiceCollection();

services.AddLogging(builder =>
{
    builder.AddSimpleConsole(o =>
    {
        o.TimestampFormat = "yyyy-MM-dd HH:mm:ss | ";
        o.SingleLine = true;
    });
    builder.SetMinimumLevel(LogLevel.Information);
});

services.AddSingleton(config);
services.AddSingleton<IPropertyScraper, PlaywrightFotocasaScraper>();
services.AddSingleton<IReportWriter, ClosedXmlExcelWriter>();
services.AddSingleton<ExtractPropertiesUseCase>();

await using var provider = services.BuildServiceProvider();

var logDirectory = Path.GetDirectoryName(config.Output.LogPath);
if (!string.IsNullOrEmpty(logDirectory))
{
    Directory.CreateDirectory(logDirectory);
}

var logger = provider.GetRequiredService<ILogger<Program>>();

// 3. Ejecución del caso de uso
try
{
    var useCase = provider.GetRequiredService<ExtractPropertiesUseCase>();
    var (excelPath, result) = await useCase.ExecuteAsync(
        city: config.Portal.City,
        resultCount: config.Search.ResultCount,
        highlightThreshold: config.BusinessRules.RentHighlightThreshold);

    Console.WriteLine($"\nListo. {result.ExtractedCount} inmuebles exportados a: {excelPath}");
}
catch (DomainException ex)
{
    logger.LogError(ex, "La automatización finalizó con error de negocio");
    Environment.Exit(2);
}
catch (Exception ex)
{
    logger.LogError(ex, "Error técnico inesperado");
    Environment.Exit(3);
}

internal partial class Program
{ }