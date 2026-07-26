namespace CorePortfolio.Domain.Entities;

public interface IConcurrencyTracked
{
    int Version { get; set; }
}
