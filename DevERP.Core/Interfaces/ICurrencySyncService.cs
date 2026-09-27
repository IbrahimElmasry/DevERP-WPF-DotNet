namespace DevERP.Core.Interfaces;

public record ExchangeRatesResult(decimal UsdToEgp, decimal EurToEgp, decimal SarToEgp, DateTime LastUpdatedUtc, bool Success, string? ErrorMessage = null);

public interface ICurrencySyncService
{
    Task<ExchangeRatesResult> FetchLatestRatesToEgpAsync();
}
