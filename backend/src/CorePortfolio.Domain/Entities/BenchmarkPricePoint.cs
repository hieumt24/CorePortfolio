namespace CorePortfolio.Domain.Entities;

public sealed class BenchmarkPricePoint
{
    public Guid Id { get; set; }
    public Guid BenchmarkDefinitionId { get; set; }
    public BenchmarkDefinition BenchmarkDefinition { get; set; } = null!;
    public DateTime Date { get; set; }
    public decimal ClosePrice { get; set; }
    public string Currency { get; set; } = "VND";
    public string Source { get; set; } = "Manual";
    public string QualityStatus { get; set; } = "Complete";
    public DateTime CapturedAt { get; set; }
}
