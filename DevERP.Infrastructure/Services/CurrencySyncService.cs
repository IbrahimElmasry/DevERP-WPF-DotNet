using System.Net.Http;
using System.Text.Json;
using DevERP.Core.Interfaces;

namespace DevERP.Infrastructure.Services;

public class CurrencySyncService : ICurrencySyncService
{
    private readonly HttpClient _httpClient;

    public CurrencySyncService(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
    }

    public async Task<ExchangeRatesResult> FetchLatestRatesToEgpAsync()
    {
        try
        {
            var url = "https://open.er-api.com/v6/latest/USD";
            var json = await _httpClient.GetStringAsync(url);
            using var doc = JsonDocument.Parse(json);

            var root = doc.RootElement;
            if (root.TryGetProperty("rates", out var rates))
            {
                if (rates.TryGetProperty("EGP", out var egpElement))
                {
                    decimal usdToEgp = egpElement.GetDecimal();
                    decimal eurRate = rates.TryGetProperty("EUR", out var eurEl) ? eurEl.GetDecimal() : 0.92m;
                    decimal sarRate = rates.TryGetProperty("SAR", out var sarEl) ? sarEl.GetDecimal() : 3.75m;

                    decimal eurToEgp = eurRate > 0 ? Math.Round(usdToEgp / eurRate, 2) : 52.00m;
                    decimal sarToEgp = sarRate > 0 ? Math.Round(usdToEgp / sarRate, 2) : 12.95m;

                    return new ExchangeRatesResult(
                        Math.Round(usdToEgp, 2),
                        eurToEgp,
                        sarToEgp,
                        DateTime.UtcNow,
                        true
                    );
                }
            }

            return new ExchangeRatesResult(48.50m, 52.00m, 12.95m, DateTime.UtcNow, false, "Rate property EGP not found in response.");
        }
        catch (Exception ex)
        {
            return new ExchangeRatesResult(48.50m, 52.00m, 12.95m, DateTime.UtcNow, false, ex.Message);
        }
    }
}
