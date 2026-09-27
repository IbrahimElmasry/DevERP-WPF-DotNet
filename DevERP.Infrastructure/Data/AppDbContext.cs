using System.IO;
using DevERP.Core.Enums;
using DevERP.Core.Interfaces;
using DevERP.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DevERP.Infrastructure.Data;

public class AppDbContext : DbContext, IAppDbContext
{
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Milestone> Milestones => Set<Milestone>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
    public DbSet<CashFlowTransaction> CashFlowTransactions => Set<CashFlowTransaction>();
    public DbSet<DeveloperProfile> DeveloperProfiles => Set<DeveloperProfile>();

    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public static string GetDatabasePath()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dir = Path.Combine(localAppData, "DevERP");
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        return Path.Combine(dir, "deverp.db");
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var dbPath = GetDatabasePath();
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Client configuration
        modelBuilder.Entity<Client>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Name).IsRequired().HasMaxLength(150);
            entity.Property(c => c.Email).HasMaxLength(150);
            entity.Property(c => c.Company).HasMaxLength(150);
        });

        // Project configuration
        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).IsRequired().HasMaxLength(200);
            entity.Property(p => p.TotalBudget).HasPrecision(18, 2);
            entity.Property(p => p.HourlyRate).HasPrecision(18, 2);

            entity.HasOne(p => p.Client)
                  .WithMany(c => c.Projects)
                  .HasForeignKey(p => p.ClientId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Milestone configuration
        modelBuilder.Entity<Milestone>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Title).IsRequired().HasMaxLength(200);
            entity.Property(m => m.Amount).HasPrecision(18, 2);

            entity.HasOne(m => m.Project)
                  .WithMany(p => p.Milestones)
                  .HasForeignKey(m => m.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(m => m.Invoice)
                  .WithMany(i => i.Milestones)
                  .HasForeignKey(m => m.InvoiceId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Invoice configuration
        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(i => i.Id);
            entity.Property(i => i.InvoiceNumber).IsRequired().HasMaxLength(50);
            entity.Property(i => i.Currency).HasMaxLength(10).HasDefaultValue("EGP");
            entity.Property(i => i.SubTotal).HasPrecision(18, 2);
            entity.Property(i => i.TaxRate).HasPrecision(18, 2);
            entity.Property(i => i.TaxAmount).HasPrecision(18, 2);
            entity.Property(i => i.TotalAmount).HasPrecision(18, 2);
            entity.Property(i => i.ExchangeRateToBase).HasPrecision(18, 4).HasDefaultValue(1.0m);

            entity.HasOne(i => i.Client)
                  .WithMany(c => c.Invoices)
                  .HasForeignKey(i => i.ClientId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(i => i.CashFlowTransaction)
                  .WithOne(t => t.Invoice)
                  .HasForeignKey<Invoice>(i => i.CashFlowTransactionId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // InvoiceItem configuration
        modelBuilder.Entity<InvoiceItem>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Description).IsRequired().HasMaxLength(300);
            entity.Property(item => item.Quantity).HasPrecision(18, 2);
            entity.Property(item => item.UnitPrice).HasPrecision(18, 2);
            entity.Property(item => item.TotalPrice).HasPrecision(18, 2);

            entity.HasOne(item => item.Invoice)
                  .WithMany(i => i.Items)
                  .HasForeignKey(item => item.InvoiceId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // CashFlowTransaction configuration
        modelBuilder.Entity<CashFlowTransaction>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Amount).HasPrecision(18, 2);
            entity.Property(t => t.ExchangeRate).HasPrecision(18, 4);
            entity.Property(t => t.AmountInBaseCurrency).HasPrecision(18, 2);
            entity.Property(t => t.Category).HasMaxLength(100).IsRequired();
            entity.Property(t => t.Currency).HasMaxLength(10).IsRequired();
        });

        // DeveloperProfile pre-seeding
        modelBuilder.Entity<DeveloperProfile>().HasData(new DeveloperProfile
        {
            Id = 1,
            FullName = "Ibrahim Tarek",
            ProfessionalTitle = "Software Engineer & Consultant",
            Email = "ibrahim@deverp.local",
            Phone = "+20 100 123 4567",
            Address = "Cairo, Egypt",
            TaxNumber = "EG-TAX-982143",
            BankName = "National Bank of Egypt (NBE)",
            BankAccountHolder = "Ibrahim Tarek",
            Iban = "EG380001000100000012345678901",
            SwiftBic = "NBEGEGCX001",
            InstaPayAddress = "ibrahim@instapay",
            InstaPayPhone = "+20 101 980 4919",
            BaseCurrency = "EGP",
            UsdToEgpRate = 48.50m,
            EurToEgpRate = 52.00m,
            SarToEgpRate = 12.95m,
            IsPinEnabled = true,
            SecurityPin = "1234"
        });
    }
}
