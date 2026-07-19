using MediatR;

namespace CorePortfolio.API.Features.Cashflows.CreateCashflowRecord;

public record CreateCashflowRecordForUserCommand(
    Guid UserId,
    Guid PortfolioId,
    Guid CategoryId,
    decimal Amount,
    string Currency,
    DateTime Date,
    string Description) : IRequest<Guid>;

public sealed class CreateCashflowRecordForUserHandler(CashflowRecordWriter writer)
    : IRequestHandler<CreateCashflowRecordForUserCommand, Guid>
{
    public Task<Guid> Handle(CreateCashflowRecordForUserCommand request, CancellationToken cancellationToken) =>
        writer.CreateAsync(request.UserId, request.PortfolioId, request.CategoryId, request.Amount,
            request.Currency, request.Date, request.Description, cancellationToken);
}
