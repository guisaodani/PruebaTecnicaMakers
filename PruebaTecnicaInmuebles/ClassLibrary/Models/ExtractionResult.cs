namespace PropertyExtractor.Domain.Models;

public class ExtractionResult
{
    public IReadOnlyList<Property> Properties { get; }
    public string City { get; }
    public int ExpectedCount { get; }

    public ExtractionResult(IReadOnlyList<Property> properties, string city, int expectedCount)
    {
        Properties = properties;
        City = city;
        ExpectedCount = expectedCount;
    }

    public int ExtractedCount => Properties.Count;

    public bool IsComplete => ExtractedCount >= ExpectedCount;
}