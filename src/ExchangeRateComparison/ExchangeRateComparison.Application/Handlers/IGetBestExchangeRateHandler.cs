using ExchangeRateComparison.Domain.Entities;

namespace ExchangeRateComparison.Application.Handlers;

public interface IGetBestExchangeRateHandler
{
    Task<ExchangeComparisonResult> HandleAsync(
        ExchangeRequest request,
        CancellationToken cancellationToken = default);
}