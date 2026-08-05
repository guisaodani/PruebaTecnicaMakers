namespace PropertyExtractor.Domain.Models;

public class Property
{
    public string? Name { get; set; }
    public double? AreaM2 { get; set; }
    public string? Address { get; set; }
    public string? Link { get; set; }
    public decimal? Price { get; set; }
    public List<string> Errors { get; } = new();

    public bool HasIncompleteData => Errors.Count > 0;

    public bool ExceedsThreshold(decimal threshold) => Price.HasValue && Price.Value > threshold;
}