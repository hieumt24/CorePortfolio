using CorePortfolio.API.Common;
using CorePortfolio.Domain.Entities;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using CorePortfolio.API.Services;
using CorePortfolio.API.Features.Admin.ControlPlane;

namespace CorePortfolio.API.Features.Performance.Benchmarks;

public sealed record UpsertBenchmarkCommand(
    Guid? Id,
    string Name,
    string Symbol,
    Guid? MarketAssetId,
    string AssetGroup,
    bool IsDefault,
    string Currency,
    bool IsActive) : IRequest<BenchmarkDefinitionDto>, IAdminPermissionRequest
{
    public string RequiredPermission => AdminPermissionCatalog.MarketDataManage;
}

public sealed class UpsertBenchmarkHandler(AppDbContext dbContext, AuditWriter auditWriter)
    : IRequestHandler<UpsertBenchmarkCommand, BenchmarkDefinitionDto>
{
    public async Task<BenchmarkDefinitionDto> Handle(
        UpsertBenchmarkCommand request,
        CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        var symbol = request.Symbol.Trim().ToUpperInvariant();
        var assetGroup = request.AssetGroup.Trim();
        var currency = request.Currency.Trim().ToUpperInvariant();

        if (name.Length is < 2 or > 100)
            throw new RequestValidationException("Tên benchmark phải có từ 2 đến 100 ký tự.");
        if (symbol.Length is < 1 or > 30)
            throw new RequestValidationException("Mã benchmark phải có từ 1 đến 30 ký tự.");
        if (assetGroup is not ("All" or "Crypto" or "Stock" or "Fund"))
            throw new RequestValidationException("Asset group không hợp lệ.");
        if (currency is not ("VND" or "USD"))
            throw new RequestValidationException("Currency phải là VND hoặc USD.");
        if (request.MarketAssetId.HasValue &&
            !await dbContext.MarketAssets.AnyAsync(
                asset => asset.Id == request.MarketAssetId.Value,
                cancellationToken))
            throw new ResourceNotFoundException("Không tìm thấy Market Asset cho benchmark.");

        BenchmarkDefinition benchmark;
        if (request.Id.HasValue)
        {
            benchmark = await dbContext.BenchmarkDefinitions
                .SingleOrDefaultAsync(item => item.Id == request.Id.Value, cancellationToken)
                ?? throw new ResourceNotFoundException("Không tìm thấy benchmark.");
        }
        else
        {
            benchmark = new BenchmarkDefinition
            {
                Id = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow
            };
            dbContext.BenchmarkDefinitions.Add(benchmark);
        }

        if (request.IsDefault)
        {
            var otherDefaults = await dbContext.BenchmarkDefinitions
                .Where(item =>
                    item.Id != benchmark.Id &&
                    item.AssetGroup == assetGroup &&
                    item.IsDefault)
                .ToListAsync(cancellationToken);
            foreach (var other in otherDefaults)
                other.IsDefault = false;
        }

        benchmark.Name = name;
        benchmark.Symbol = symbol;
        benchmark.MarketAssetId = request.MarketAssetId;
        benchmark.AssetGroup = assetGroup;
        benchmark.IsDefault = request.IsDefault;
        benchmark.Currency = currency;
        benchmark.IsActive = request.IsActive;
        auditWriter.Add(
            request.Id.HasValue ? "BenchmarkUpdated" : "BenchmarkCreated",
            "BenchmarkDefinition",
            benchmark.Id.ToString(),
            new { benchmark.Symbol, benchmark.AssetGroup, benchmark.IsDefault, benchmark.IsActive });
        await dbContext.SaveChangesAsync(cancellationToken);

        var pointStats = await dbContext.BenchmarkPricePoints
            .Where(point => point.BenchmarkDefinitionId == benchmark.Id)
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Count = group.Count(),
                LastDate = group.Max(point => (DateTime?)point.Date)
            })
            .SingleOrDefaultAsync(cancellationToken);

        return new BenchmarkDefinitionDto(
            benchmark.Id,
            benchmark.Name,
            benchmark.Symbol,
            benchmark.MarketAssetId,
            benchmark.AssetGroup,
            benchmark.IsDefault,
            benchmark.Currency,
            benchmark.IsActive,
            pointStats?.Count ?? 0,
            pointStats?.LastDate);
    }
}
