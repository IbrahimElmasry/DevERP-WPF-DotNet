namespace DevERP.Core.Models;

public class Milestone
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public DateTime? DueDate { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public bool IsInvoiced { get; set; }

    public int ProjectId { get; set; }
    public Project? Project { get; set; }

    public int? InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }
}
