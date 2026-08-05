using PropertyExtractor.Domain.Models;

namespace PropertyExtractor.Domain.Ports;

/// <summary>Contrato para cualquier formato de salida (Excel, CSV, etc.).</summary>
public interface IReportWriter
{
    /// <summary>Genera el reporte y devuelve la ruta del archivo generado.</summary>
    string Write(IReadOnlyList<Property> properties, decimal highlightThreshold);
}