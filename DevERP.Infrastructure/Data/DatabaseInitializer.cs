using DevERP.Core.Enums;
using DevERP.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DevERP.Infrastructure.Data;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(AppDbContext context)
    {
        // Automatically create schema
        await context.Database.EnsureCreatedAsync();

        // Migrate columns if upgrading from earlier version without breaking data
        try { await context.Database.ExecuteSqlRawAsync("ALTER TABLE DeveloperProfiles ADD COLUMN IsPinEnabled INTEGER NOT NULL DEFAULT 1;"); } catch { }
        try { await context.Database.ExecuteSqlRawAsync("ALTER TABLE DeveloperProfiles ADD COLUMN SecurityPin TEXT NOT NULL DEFAULT '1234';"); } catch { }
        try { await context.Database.ExecuteSqlRawAsync("ALTER TABLE DeveloperProfiles ADD COLUMN InstaPayAddress TEXT NOT NULL DEFAULT 'ibrahim@instapay';"); } catch { }
        try { await context.Database.ExecuteSqlRawAsync("ALTER TABLE DeveloperProfiles ADD COLUMN InstaPayPhone TEXT NOT NULL DEFAULT '+20 101 980 4919';"); } catch { }
        try { await context.Database.ExecuteSqlRawAsync("ALTER TABLE DeveloperProfiles ADD COLUMN SarToEgpRate NUMERIC NOT NULL DEFAULT 12.95;"); } catch { }
        try { await context.Database.ExecuteSqlRawAsync("ALTER TABLE DeveloperProfiles ADD COLUMN LogoPath TEXT NULL;"); } catch { }
        try { await context.Database.ExecuteSqlRawAsync("ALTER TABLE DeveloperProfiles ADD COLUMN AutoLockMinutes INTEGER NOT NULL DEFAULT 0;"); } catch { }
        try { await context.Database.ExecuteSqlRawAsync("ALTER TABLE Invoices ADD COLUMN PaymentMethod INTEGER NOT NULL DEFAULT 0;"); } catch { }

        // Default Logo Auto-linking if not set
        try
        {
            var profile = await context.DeveloperProfiles.FirstOrDefaultAsync();
            if (profile != null && (string.IsNullOrWhiteSpace(profile.LogoPath) || !File.Exists(profile.LogoPath)))
            {
                var candidatePaths = new[]
                {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DevERP", "brand_logo.png"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "brand_logo.png")
                };

                foreach (var candidate in candidatePaths)
                {
                    if (File.Exists(candidate))
                    {
                        profile.LogoPath = candidate;
                        await context.SaveChangesAsync();
                        break;
                    }
                }
            }
        }
        catch { }

        // Automated Overdue Invoice Evaluation
        try
        {
            var now = DateTime.UtcNow.Date;
            var overdueInvoices = await context.Invoices
                .Where(i => i.Status == InvoiceStatus.Sent && i.DueDate < now)
                .ToListAsync();

            if (overdueInvoices.Count > 0)
            {
                foreach (var inv in overdueInvoices)
                {
                    inv.Status = InvoiceStatus.Overdue;
                }
                await context.SaveChangesAsync();
            }
        }
        catch { }

        var dbPath = AppDbContext.GetDatabasePath();
        var markerPath = Path.Combine(Path.GetDirectoryName(dbPath) ?? "", ".deverp_initialized");

        // If the marker exists, the system has already completed its first-run setup.
        // NEVER reset or re-seed sample data, even if the user deletes all clients or clears the ledger!
        if (File.Exists(markerPath))
        {
            return;
        }

        // If a developer profile already exists, this is an existing database from a prior session.
        // Stamp the marker file and do NOT re-seed sample clients or overwrite user state.
        bool hasProfile = await context.DeveloperProfiles.AnyAsync();
        if (hasProfile)
        {
            try { await File.WriteAllTextAsync(markerPath, DateTime.UtcNow.ToString("O")); } catch { }
            return;
        }

        // Fresh install: Ensure default developer profile exists
        context.DeveloperProfiles.Add(new DeveloperProfile
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
        await context.SaveChangesAsync();

        // Seed initial sample data only on the true first run
        if (!await context.Clients.AnyAsync())
        {
            var client1 = new Client
            {
                Name = "Acme Fintech Corp",
                Company = "Acme Global Solutions",
                Email = "billing@acmefintech.io",
                Phone = "+20 102 334 5566",
                Address = "Smart Village, Building B12, Giza, Egypt",
                Notes = "Enterprise client for microservices and cloud banking integrations.",
                CreatedAt = DateTime.UtcNow.AddMonths(-2)
            };

            var client2 = new Client
            {
                Name = "Nexora Labs Ltd",
                Company = "Nexora Technologies",
                Email = "ops@nexoralabs.io",
                Phone = "+44 20 7946 0912",
                Address = "100 Bishopsgate, London EC2N 4AG, UK",
                Notes = "SaaS client for real-time analytics engine and dashboard development.",
                CreatedAt = DateTime.UtcNow.AddMonths(-1)
            };

            context.Clients.AddRange(client1, client2);
            await context.SaveChangesAsync();

            var project1 = new Project
            {
                Name = "Cloud Banking Microservices",
                Description = "High-performance .NET 8 event-driven microservices architecture.",
                Status = ProjectStatus.Active,
                BillingType = BillingType.FixedMilestone,
                TotalBudget = 60000m,
                ClientId = client1.Id,
                CreatedAt = DateTime.UtcNow.AddMonths(-2)
            };

            var project2 = new Project
            {
                Name = "DevOps Pipeline & Desktop Shell",
                Description = "Custom desktop ERP management shell and CI/CD pipelines.",
                Status = ProjectStatus.Active,
                BillingType = BillingType.FixedMilestone,
                TotalBudget = 2700m,
                ClientId = client2.Id,
                CreatedAt = DateTime.UtcNow.AddMonths(-1)
            };

            context.Projects.AddRange(project1, project2);
            await context.SaveChangesAsync();

            var m1 = new Milestone
            {
                Title = "Phase 1: Architecture Blueprint & API Gateway",
                Description = "Architecture diagram, reverse proxy setup, and authentication provider.",
                Amount = 15000m,
                DueDate = DateTime.UtcNow.AddDays(-20),
                IsCompleted = true,
                CompletedAt = DateTime.UtcNow.AddDays(-21),
                IsInvoiced = true,
                ProjectId = project1.Id
            };

            var m2 = new Milestone
            {
                Title = "Phase 2: Payment Integration Service",
                Description = "Fawry and Stripe webhooks, settlement reconciliation engine.",
                Amount = 25000m,
                DueDate = DateTime.UtcNow.AddDays(5),
                IsCompleted = true,
                CompletedAt = DateTime.UtcNow.AddDays(-1),
                IsInvoiced = false,
                ProjectId = project1.Id
            };

            var m3 = new Milestone
            {
                Title = "Phase 3: High-Availability Production Rollout",
                Description = "Docker Swarm deployment, Grafana monitoring, and failover tests.",
                Amount = 20000m,
                DueDate = DateTime.UtcNow.AddDays(25),
                IsCompleted = false,
                IsInvoiced = false,
                ProjectId = project1.Id
            };

            var m4 = new Milestone
            {
                Title = "Milestone 1: Backend Database & Core APIs",
                Description = "Database schema, EF Core migrations, and REST endpoints.",
                Amount = 1200m,
                DueDate = DateTime.UtcNow.AddDays(-10),
                IsCompleted = true,
                CompletedAt = DateTime.UtcNow.AddDays(-10),
                IsInvoiced = true,
                ProjectId = project2.Id
            };

            var m5 = new Milestone
            {
                Title = "Milestone 2: Desktop Client Implementation",
                Description = "WPF Windows 11 UI with reactive data-binding and offline SQLite cache.",
                Amount = 1500m,
                DueDate = DateTime.UtcNow.AddDays(15),
                IsCompleted = false,
                IsInvoiced = false,
                ProjectId = project2.Id
            };

            context.Milestones.AddRange(m1, m2, m3, m4, m5);
            await context.SaveChangesAsync();

            // Seed sample invoice 1 (Paid)
            var invoice1 = new Invoice
            {
                InvoiceNumber = "INV-2026-001",
                IssueDate = DateTime.UtcNow.AddDays(-21),
                DueDate = DateTime.UtcNow.AddDays(-7),
                Status = InvoiceStatus.Paid,
                ClientId = client1.Id,
                Currency = "EGP",
                ExchangeRateToBase = 1.0m,
                SubTotal = 15000m,
                TaxRate = 0m,
                TaxAmount = 0m,
                TotalAmount = 15000m,
                PaidAt = DateTime.UtcNow.AddDays(-15),
                Notes = "Wire transfer received in full. Thank you for your partnership."
            };
            invoice1.Items.Add(new InvoiceItem
            {
                Description = "Phase 1: Architecture Blueprint & API Gateway (Milestone Completion)",
                Quantity = 1,
                UnitPrice = 15000m,
                TotalPrice = 15000m
            });
            context.Invoices.Add(invoice1);
            await context.SaveChangesAsync();

            m1.InvoiceId = invoice1.Id;

            // Matching CashFlow Inflow for Paid Invoice
            var t1 = new CashFlowTransaction
            {
                Date = DateTime.UtcNow.AddDays(-15),
                Type = TransactionType.Inflow,
                Amount = 15000m,
                Currency = "EGP",
                ExchangeRate = 1.0m,
                AmountInBaseCurrency = 15000m,
                Category = "Client Payment",
                Description = "Payment for INV-2026-001 - Acme Fintech Corp (Phase 1)",
                Reference = "INV-2026-001",
                InvoiceId = invoice1.Id
            };
            context.CashFlowTransactions.Add(t1);
            await context.SaveChangesAsync();

            invoice1.CashFlowTransactionId = t1.Id;

            // Seed sample invoice 2 (Sent/Pending)
            var invoice2 = new Invoice
            {
                InvoiceNumber = "INV-2026-002",
                IssueDate = DateTime.UtcNow.AddDays(-5),
                DueDate = DateTime.UtcNow.AddDays(9),
                Status = InvoiceStatus.Sent,
                ClientId = client2.Id,
                Currency = "USD",
                ExchangeRateToBase = 48.50m,
                SubTotal = 1200m,
                TaxRate = 0m,
                TaxAmount = 0m,
                TotalAmount = 1200m,
                Notes = "Payment terms: Net 14 via SWIFT wire transfer to our USD bank coordinates."
            };
            invoice2.Items.Add(new InvoiceItem
            {
                Description = "Milestone 1: Backend Database & Core APIs (Completed)",
                Quantity = 1,
                UnitPrice = 1200m,
                TotalPrice = 1200m
            });
            context.Invoices.Add(invoice2);
            await context.SaveChangesAsync();

            m4.InvoiceId = invoice2.Id;

            // Seed several expense / outflow ledger items
            var t2 = new CashFlowTransaction
            {
                Date = DateTime.UtcNow.AddDays(-12),
                Type = TransactionType.Outflow,
                Amount = 1850m,
                Currency = "EGP",
                ExchangeRate = 1.0m,
                AmountInBaseCurrency = 1850m,
                Category = "VPS/Tools",
                Description = "Hetzner Cloud Dedicated Server & DigitalOcean Cluster (Monthly)",
                Reference = "HZ-INV-9921"
            };

            var t3 = new CashFlowTransaction
            {
                Date = DateTime.UtcNow.AddDays(-8),
                Type = TransactionType.Outflow,
                Amount = 950m,
                Currency = "EGP",
                ExchangeRate = 1.0m,
                AmountInBaseCurrency = 950m,
                Category = "VPS/Tools",
                Description = "GitHub Copilot + JetBrains All Products Pack Renewal",
                Reference = "JB-SUB-2026"
            };

            var t4 = new CashFlowTransaction
            {
                Date = DateTime.UtcNow.AddDays(-4),
                Type = TransactionType.Outflow,
                Amount = 4000m,
                Currency = "EGP",
                ExchangeRate = 1.0m,
                AmountInBaseCurrency = 4000m,
                Category = "Tax",
                Description = "Freelance & Consulting Estimated Quarterly Tax Provision",
                Reference = "TAX-Q3-2026"
            };

            var t5 = new CashFlowTransaction
            {
                Date = DateTime.UtcNow.AddDays(-2),
                Type = TransactionType.Outflow,
                Amount = 2500m,
                Currency = "EGP",
                ExchangeRate = 1.0m,
                AmountInBaseCurrency = 2500m,
                Category = "Personal",
                Description = "Ergonomic Monitor Arm & Studio Lighting Setup",
                Reference = "AMZN-EG-4412"
            };

            context.CashFlowTransactions.AddRange(t2, t3, t4, t5);
            await context.SaveChangesAsync();

            // Stamp marker file
            try { await File.WriteAllTextAsync(markerPath, DateTime.UtcNow.ToString("O")); } catch { }
        }
    }
}
