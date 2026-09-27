using DevERP.Core.Enums;

namespace DevERP.Core.Models;

public class Invoice
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; } = DateTime.UtcNow.Date;
    public DateTime DueDate { get; set; } = DateTime.UtcNow.Date.AddDays(14);
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    
    public decimal SubTotal { get; set; }
    public decimal TaxRate { get; set; } = 0m;
    public decimal TaxAmount { get; set; } = 0m;
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "EGP";
    public decimal ExchangeRateToBase { get; set; } = 1.0m;

    public string? Notes { get; set; }
    public DateTime? PaidAt { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.BankWire;

    public int ClientId { get; set; }
    public Client? Client { get; set; }

    public int? CashFlowTransactionId { get; set; }
    public CashFlowTransaction? CashFlowTransaction { get; set; }

    public ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
    public ICollection<Milestone> Milestones { get; set; } = new List<Milestone>();

    public void RecalculateTotals()
    {
        SubTotal = Items.Sum(i => i.TotalPrice);
        TaxAmount = Math.Round(SubTotal * (TaxRate / 100m), 2);
        TotalAmount = SubTotal + TaxAmount;
    }
}
