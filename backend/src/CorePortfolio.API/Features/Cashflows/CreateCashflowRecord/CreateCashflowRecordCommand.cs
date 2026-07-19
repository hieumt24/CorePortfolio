using CorePortfolio.API.Services;
using MediatR;

namespace CorePortfolio.API.Features.Cashflows.CreateCashflowRecord;

public record CreateCashflowRecordCommand(
    Guid PortfolioId,
    Guid CategoryId,
    decimal Amount,
    string Currency,
    DateTime Date,
    string Description) : IRequest<Guid>;

public sealed class CreateCashflowRecordHandler(
    ICurrentUserService currentUserService,
    CashflowRecordWriter writer) : IRequestHandler<CreateCashflowRecordCommand, Guid>
{
    public Task<Guid> Handle(CreateCashflowRecordCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId ?? throw new UnauthorizedAccessException();
        return writer.CreateAsync(userId, request.PortfolioId, request.CategoryId, request.Amount,
            request.Currency, request.Date, request.Description, cancellationToken);
    }
}
