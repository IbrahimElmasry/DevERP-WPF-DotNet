using DevERP.Core.Enums;

namespace DevERP.Core.Models;

public class Project
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ProjectStatus Status { get; set; } = ProjectStatus.Active;
    public BillingType BillingType { get; set; } = BillingType.FixedMilestone;
    public decimal TotalBudget { get; set; }
    public decimal HourlyRate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int ClientId { get; set; }
    public Client? Client { get; set; }

    public ICollection<Milestone> Milestones { get; set; } = new List<Milestone>();
}
