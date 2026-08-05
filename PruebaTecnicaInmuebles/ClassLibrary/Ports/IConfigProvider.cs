using PropertyExtractor.Domain.Models;

namespace PropertyExtractor.Domain.Ports;

/// <summary>Contrato para la fuente de configuración (JSON, variables de entorno, etc.).</summary>
public interface IConfigProvider
{
    AppConfig Get();
}