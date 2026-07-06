using CorePortfolio.API.Common;
using CorePortfolio.API.Services;
using CorePortfolio.Domain.Accounting;
using CorePortfolio.Domain.Entities;
using CorePortfolio.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CorePortfolio.API.Features.CashAccounts;

public record CashAccountDto(Guid Id, Guid PortfolioId, string Currency, decimal Balance);
public record CashLedgerEntryDto(Guid Id, decimal Amount, CashLedgerEntryType Type, string Description,
    DateTime OccurredAt, Guid? TransactionId);
public record GetCashAccountsQuery(Guid? PortfolioId) : IRequest<List<CashAccountDto>>;
public record GetCashLedgerQuery(Guid CashAccountId) : IRequest<List<CashLedgerEntryDto>>;
public record AddOpeningBalanceCommand(Guid PortfolioId, string Currency, decimal Amount, string? Notes)
    : IRequest<CashAccountDto>;
public record AdjustCashBalanceCommand(Guid PortfolioId, string Currency, decimal Amount, bool IsDeposit,
    string? Description, DateTime OccurredAt) : IRequest<CashAccountDto>;

public sealed class CashAccountsHandler :
    IRequestHandler<GetCashAccountsQuery, List<CashAccountDto>>,
    IRequestHandler<GetCashLedgerQuery, List<CashLedgerEntryDto>>,
    IRequestHandler<AddOpeningBalanceCommand, CashAccountDto>,
    IRequestHandler<AdjustCashBalanceCommand, CashAccountDto>
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    public CashAccountsHandler(AppDbContext db, ICurrentUserService currentUser) { _db = db; _currentUser = currentUser; }

    public Task<List<CashAccountDto>> Handle(GetCashAccountsQuery request, CancellationToken cancellationToken) =>
        _db.CashAccounts.AsNoTracking()
            .Where(a => a.Portfolio.UserId == _currentUser.UserId &&
                (request.PortfolioId == null || a.PortfolioId == request.PortfolioId))
            .Select(a => new CashAccountDto(a.Id, a.PortfolioId, a.Currency, a.Entries.Sum(e => e.Amount)))
            .ToListAsync(cancellationToken);

    public async Task<List<CashLedgerEntryDto>> Handle(GetCashLedgerQuery request, CancellationToken cancellationToken)
    {
        var exists = await _db.CashAccounts.AnyAsync(a => a.Id == request.CashAccountId &&
            a.Portfolio.UserId == _currentUser.UserId, cancellationToken);
        if (!exists) throw new ResourceNotFoundException("Không tìm thấy tài khoản tiền.");
        return await _db.CashLedgerEntries.AsNoTracking().Where(e => e.CashAccountId == request.CashAccountId)
            .OrderByDescending(e => e.OccurredAt)
            .Select(e => new CashLedgerEntryDto(e.Id, e.Amount, e.Type, e.Description, e.OccurredAt, e.TransactionId))
            .ToListAsync(cancellationToken);
    }

    public async Task<CashAccountDto> Handle(AddOpeningBalanceCommand request, CancellationToken cancellationToken)
    {
        if (request.Amount < 0) throw new AccountingValidationException("Số dư đầu kỳ không được âm.");
        var currency = request.Currency.Trim().ToUpperInvariant();
        if (currency is not ("VND" or "USD")) throw new AccountingValidationException("Chỉ hỗ trợ VND và USD.");
        if (!await _db.Portfolios.AnyAsync(p => p.Id == request.PortfolioId && p.UserId == _currentUser.UserId, cancellationToken))
            throw new ResourceNotFoundException("Không tìm thấy danh mục.");

        var account = await _db.CashAccounts.SingleOrDefaultAsync(a => a.PortfolioId == request.PortfolioId &&
            a.Currency == currency, cancellationToken);
        if (account == null)
        {
            account = new CashAccount { Id = Guid.NewGuid(), PortfolioId = request.PortfolioId, Currency = currency };
            _db.CashAccounts.Add(account);
        }
        _db.CashLedgerEntries.Add(new CashLedgerEntry { Id = Guid.NewGuid(), CashAccount = account,
            Amount = request.Amount, Type = CashLedgerEntryType.OpeningBalance,
            Description = request.Notes?.Trim() ?? "Số dư đầu kỳ", OccurredAt = DateTime.UtcNow });
        await _db.SaveChangesAsync(cancellationToken);
        var balance = await _db.CashLedgerEntries.Where(e => e.CashAccountId == account.Id).SumAsync(e => e.Amount, cancellationToken);
        return new CashAccountDto(account.Id, account.PortfolioId, account.Currency, balance);
    }
    public async Task<CashAccountDto> Handle(AdjustCashBalanceCommand request, CancellationToken cancellationToken)
    {
        if (request.Amount <= 0) throw new AccountingValidationException("Số tiền phải lớn hơn 0.");
        var currency = request.Currency.Trim().ToUpperInvariant();
        if (currency is not ("VND" or "USD")) throw new AccountingValidationException("Chỉ hỗ trợ VND và USD.");
        if (!await _db.Portfolios.AnyAsync(p => p.Id == request.PortfolioId && p.UserId == _currentUser.UserId, cancellationToken))
            throw new ResourceNotFoundException("Không tìm thấy danh mục.");

        var account = await _db.CashAccounts.SingleOrDefaultAsync(a => a.PortfolioId == request.PortfolioId &&
            a.Currency == currency, cancellationToken);

        if (account == null)
        {
            if (!request.IsDeposit) throw new AccountingValidationException("Tài khoản tiền không đủ số dư để rút.");
            account = new CashAccount { Id = Guid.NewGuid(), PortfolioId = request.PortfolioId, Currency = currency };
            _db.CashAccounts.Add(account);
        }

        var entryAmount = request.IsDeposit ? request.Amount : -request.Amount;
        var entryType = request.IsDeposit ? CashLedgerEntryType.Deposit : CashLedgerEntryType.Withdrawal;

        _db.CashLedgerEntries.Add(new CashLedgerEntry
        {
            Id = Guid.NewGuid(),
            CashAccount = account,
            Amount = entryAmount,
            Type = entryType,
            Description = request.Description?.Trim() ?? (request.IsDeposit ? "Nạp tiền" : "Rút tiền"),
            OccurredAt = request.OccurredAt == default ? DateTime.UtcNow : request.OccurredAt
        });

        await _db.SaveChangesAsync(cancellationToken);

        var balance = await _db.CashLedgerEntries.Where(e => e.CashAccountId == account.Id).SumAsync(e => e.Amount, cancellationToken);
        if (balance < 0) throw new AccountingValidationException("Số dư không đủ để thực hiện giao dịch rút tiền.");

        return new CashAccountDto(account.Id, account.PortfolioId, account.Currency, balance);
    }
}

public static class CashAccountsEndpoints
{
    public static void MapCashAccountsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/cash-accounts").WithTags("Cash Accounts");
        group.MapGet("/", (Guid? portfolioId, IMediator mediator) => mediator.Send(new GetCashAccountsQuery(portfolioId)));
        group.MapGet("/{id:guid}/ledger", (Guid id, IMediator mediator) => mediator.Send(new GetCashLedgerQuery(id)));
        group.MapPost("/opening-balance", async (AddOpeningBalanceCommand command, IMediator mediator) =>
            Results.Ok(await mediator.Send(command)));
        group.MapPost("/adjust-balance", async (AdjustCashBalanceCommand command, IMediator mediator) =>
            Results.Ok(await mediator.Send(command)));
    }
}
