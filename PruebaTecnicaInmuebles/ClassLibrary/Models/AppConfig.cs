using System.Text.Json.Serialization;

namespace PropertyExtractor.Domain.Models;

/// <summary>
/// Forma de la configuración que necesita el negocio. Vive en Domain porque
/// Application depende de ella; el detalle de "viene de un JSON" es de
/// Infrastructure (lo veremos más adelante en JsonConfigProvider).
/// Los [JsonPropertyName] mapean cada propiedad a la clave exacta del
/// archivo config.json.
/// </summary>
public class AppConfig
{
    [JsonPropertyName("portal")]
    public PortalConfig Portal { get; set; } = new();

    [JsonPropertyName("search")]
    public SearchConfig Search { get; set; } = new();

    [JsonPropertyName("business_rules")]
    public BusinessRulesConfig BusinessRules { get; set; } = new();

    [JsonPropertyName("output")]
    public OutputConfig Output { get; set; } = new();

    [JsonPropertyName("browser")]
    public BrowserConfig Browser { get; set; } = new();
}

public class PortalConfig
{
    [JsonPropertyName("base_url")]
    public string BaseUrl { get; set; } = "https://www.fotocasa.es";

    [JsonPropertyName("operation")]
    public string Operation { get; set; } = "alquiler";

    [JsonPropertyName("property_type")]
    public string PropertyType { get; set; } = "viviendas";

    [JsonPropertyName("city")]
    public string City { get; set; } = "";

    [JsonPropertyName("zone")]
    public string Zone { get; set; } = "todas-las-zonas";
}

public class SearchConfig
{
    [JsonPropertyName("result_count")]
    public int ResultCount { get; set; } = 5;

    [JsonPropertyName("navigation_timeout_ms")]
    public int NavigationTimeoutMs { get; set; } = 45000;

    [JsonPropertyName("element_timeout_ms")]
    public int ElementTimeoutMs { get; set; } = 15000;

    [JsonPropertyName("retries")]
    public int Retries { get; set; } = 3;

    [JsonPropertyName("retry_delay_seconds")]
    public int RetryDelaySeconds { get; set; } = 5;
}

public class BusinessRulesConfig
{
    [JsonPropertyName("rent_highlight_threshold")]
    public decimal RentHighlightThreshold { get; set; } = 900;

    [JsonPropertyName("highlight_color_hex")]
    public string HighlightColorHex { get; set; } = "FFC7CE";

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "EUR";
}

public class OutputConfig
{
    [JsonPropertyName("excel_path")]
    public string ExcelPath { get; set; } = "output/properties_result.xlsx";

    [JsonPropertyName("sheet_name")]
    public string SheetName { get; set; } = "Properties";

    [JsonPropertyName("log_path")]
    public string LogPath { get; set; } = "logs/execution.log";
}

public class BrowserConfig
{
    [JsonPropertyName("headless")]
    public bool Headless { get; set; } = true;

    [JsonPropertyName("user_agent")]
    public string UserAgent { get; set; } = "";

    [JsonPropertyName("locale")]
    public string Locale { get; set; } = "es-ES";
}