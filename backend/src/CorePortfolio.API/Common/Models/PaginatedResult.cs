namespace CorePortfolio.API.Common.Models;

public record PaginatedResult<T>(List<T> Items, int TotalCount, int Page, int PageSize);
