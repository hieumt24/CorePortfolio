using CorePortfolio.Domain.Models.Telegram;

namespace CorePortfolio.Domain.Interfaces;

public interface ITelegramCommandProcessor
{
    Task<string> ProcessCashflowAsync(CashflowCommandData data, CancellationToken cancellationToken = default);
    Task<string> ProcessTransactionAsync(TransactionCommandData data, CancellationToken cancellationToken = default);
}
