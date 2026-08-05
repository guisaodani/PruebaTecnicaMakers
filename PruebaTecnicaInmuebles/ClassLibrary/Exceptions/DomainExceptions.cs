namespace PropertyExtractor.Domain.Exceptions;

/// <summary>Excepción base de la que heredan todas las excepciones del dominio.</summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message)
    {
    }

    protected DomainException(string message, Exception inner) : base(message, inner)
    {
    }
}

/// <summary>
/// No fue posible completar la extracción de datos (portal caído,
/// cambio de estructura, timeout agotado tras reintentos, etc.).
/// </summary>
public class ScraperException : DomainException
{
    public ScraperException(string message) : base(message)
    {
    }

    public ScraperException(string message, Exception inner) : base(message, inner)
    {
    }
}

/// <summary>No fue posible generar el reporte de salida.</summary>
public class ReportGenerationException : DomainException
{
    public ReportGenerationException(string message) : base(message)
    {
    }

    public ReportGenerationException(string message, Exception inner) : base(message, inner)
    {
    }
}

/// <summary>La configuración es inválida o no se pudo cargar.</summary>
public class ConfigException : DomainException
{
    public ConfigException(string message) : base(message)
    {
    }

    public ConfigException(string message, Exception inner) : base(message, inner)
    {
    }
}