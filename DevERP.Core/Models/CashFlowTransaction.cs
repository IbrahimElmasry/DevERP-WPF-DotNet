using DevERP.Core.Enums;

namespace DevERP.Core.Models;

public class CashFlowTransaction
{
    public int Id { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow.Date;
    public TransactionType Type { get; set; } = TransactionType.Inflow;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EGP";
    public decimal ExchangeRate { get; set; } = 1.0m;
    public decimal AmountInBaseCurrency { get; set; }
    public string Category { get; set; } = "Client Payment";
    public string Description { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int? InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }

    public void RecalculateBaseAmount()
    {
        AmountInBaseCurrency = Math.Round(Amount * (ExchangeRate > 0 ? ExchangeRate : 1.0m), 2);
    }
}
