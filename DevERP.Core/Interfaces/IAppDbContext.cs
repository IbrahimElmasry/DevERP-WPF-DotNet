using DevERP.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace DevERP.Core.Interfaces;

public interface IAppDbContext
{
    DbSet<Client> Clients { get; }
    DbSet<Project> Projects { get; }
    DbSet<Milestone> Milestones { get; }
    DbSet<Invoice> Invoices { get; }
    DbSet<InvoiceItem> InvoiceItems { get; }
    DbSet<CashFlowTransaction> CashFlowTransactions { get; }
    DbSet<DeveloperProfile> DeveloperProfiles { get; }

    DatabaseFacade Database { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
