namespace DevERP.Core.Models;

public class InvoiceItem
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }

    public int InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }

    public void CalculateTotal()
    {
        TotalPrice = Math.Round(Quantity * UnitPrice, 2);
    }
}
