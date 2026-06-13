namespace CorePortfolio.Domain.Entities;

public class AssetCategory
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DefaultCurrency { get; set; } = "VND";
}
