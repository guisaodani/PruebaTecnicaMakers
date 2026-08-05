using System.Text.Json;
using PropertyExtractor.Domain.Exceptions;
using PropertyExtractor.Domain.Models;
using PropertyExtractor.Domain.Ports;

namespace PropertyExtractor.Infrastructure.Config;

/// <summary>
/// Implementación concreta de IConfigProvider que lee la configuración
/// desde un archivo JSON externo. Si mañana se necesita leer desde
/// variables de entorno o un servicio remoto, se crea otra clase que
/// implemente IConfigProvider, sin tocar el resto del sistema.
/// </summary>
public class JsonConfigProvider : IConfigProvider
{
    private readonly string _configPath;

    public JsonConfigProvider(string configPath)
    {
        _configPath = configPath;
    }

    public AppConfig Get()
    {
        if (!File.Exists(_configPath))
        {
            throw new ConfigException($"No se encontró el archivo de configuración: {_configPath}");
        }

        try
        {
            var json = File.ReadAllText(_configPath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            };
            var config = JsonSerializer.Deserialize<AppConfig>(json, options);

            if (config is null)
            {
                throw new ConfigException("El archivo de configuración está vacío o es inválido.");
            }

            return config;
        }
        catch (JsonException ex)
        {
            throw new ConfigException($"El archivo de configuración no es JSON válido: {ex.Message}", ex);
        }
    }
}