using ClosedXML.Excel;
using PropertyExtractor.Domain.Exceptions;
using PropertyExtractor.Domain.Models;
using PropertyExtractor.Domain.Ports;
using Microsoft.Extensions.Logging;

namespace PropertyExtractor.Infrastructure.Reporting;

/// <summary>
/// Implementación concreta de IReportWriter usando ClosedXML. Es la única
/// capa que sabe que existe Excel, celdas, colores, etc.
/// </summary>
public class ClosedXmlExcelWriter : IReportWriter
{
    private static readonly string[] Headers =
    {
        "Nombre de la propiedad",
        "Área (m²)",
        "Barrio / dirección",
        "Link",
        "Valor del alquiler (€)",
    };

    private readonly string _excelPath;
    private readonly string _sheetName;
    private readonly string _highlightColorHex;
    private readonly ILogger<ClosedXmlExcelWriter> _logger;

    public ClosedXmlExcelWriter(AppConfig config, ILogger<ClosedXmlExcelWriter> logger)
    {
        _excelPath = config.Output.ExcelPath;
        _sheetName = config.Output.SheetName;
        _highlightColorHex = config.BusinessRules.HighlightColorHex;
        _logger = logger;
    }

    public string Write(IReadOnlyList<Property> properties, decimal highlightThreshold)
    {
        try
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add(_sheetName);

            WriteHeader(ws);

            var highlightColor = XLColor.FromHtml($"#{_highlightColorHex}");
            var row = 2;

            foreach (var property in properties)
            {
                WriteRow(ws, row, property, highlightThreshold, highlightColor);
                row++;
            }

            ws.Columns().AdjustToContents();
            ws.Column(1).Width = 35;
            ws.Column(2).Width = 12;
            ws.Column(3).Width = 30;
            ws.Column(4).Width = 45;
            ws.Column(5).Width = 20;
            ws.SheetView.FreezeRows(1);

            var directory = Path.GetDirectoryName(_excelPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            workbook.SaveAs(_excelPath);
            _logger.LogInformation("Excel generado en: {Path}", _excelPath);
            return _excelPath;
        }
        catch (Exception ex)
        {
            throw new ReportGenerationException($"Error generando el Excel: {ex.Message}", ex);
        }
    }

    private static void WriteHeader(IXLWorksheet ws)
    {
        for (int i = 0; i < Headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = Headers[i];
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#303AB2");
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Font.Bold = true;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        }
    }

    private static void WriteRow(
        IXLWorksheet ws, int row, Property property, decimal threshold, XLColor highlightColor)
    {
        ws.Cell(row, 1).Value = property.Name ?? "N/A";
        ws.Cell(row, 2).Value = property.AreaM2.HasValue ? property.AreaM2.Value.ToString() : "N/A";
        ws.Cell(row, 3).Value = property.Address ?? "N/A";
        ws.Cell(row, 4).Value = property.Link ?? "N/A";
        ws.Cell(row, 5).Value = property.Price.HasValue ? property.Price.Value.ToString() : "N/A";

        // La regla "ExceedsThreshold" vive en el dominio (Property.cs), no aquí.
        if (property.ExceedsThreshold(threshold))
        {
            for (int col = 1; col <= Headers.Length; col++)
            {
                ws.Cell(row, col).Style.Fill.BackgroundColor = highlightColor;
            }
        }

        var linkCell = ws.Cell(row, 4);
        if (!string.IsNullOrEmpty(property.Link) && property.Link.StartsWith("http"))
        {
            linkCell.SetHyperlink(new XLHyperlink(property.Link));
            linkCell.Style.Font.FontColor = XLColor.FromHtml("#0563C1");
            linkCell.Style.Font.Underline = XLFontUnderlineValues.Single;
        }
    }
}