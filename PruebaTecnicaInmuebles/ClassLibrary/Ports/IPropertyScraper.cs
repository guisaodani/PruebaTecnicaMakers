using PropertyExtractor.Domain.Models;

namespace PropertyExtractor.Domain.Ports;

/// <summary>
/// Contrato para cualquier fuente
/// La capa de Application depende SOLO de esta interfaz
/// </summary>
public interface IPropertyScraper
{
    /// <summary>
    /// Debe manejar sus propios reintentos internos y lanzar una excepción
    /// de dominio si falla de forma irrecuperable.
    /// </summary>
    Task<List<Property>> ExtractPropertiesAsync(string city, int resultCount);
}