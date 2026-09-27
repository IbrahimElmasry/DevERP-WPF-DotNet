# DevERP — Business Operating System for Developers

[![Platform](https://img.shields.io/badge/Platform-Windows%2011%20%7C%2010%20(x64)-blue.svg?style=flat-square&logo=windows)](https://microsoft.com/windows)
[![.NET](https://img.shields.io/badge/.NET-8.0%20%7C%209.0%20LTS-512BD4.svg?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)
[![UI Framework](https://img.shields.io/badge/UI-WPF--UI%20Mica%20Fluent-0078D4.svg?style=flat-square)](https://wpfui.lepo.co/)
[![Database](https://img.shields.io/badge/Database-SQLite%20(Local%20WAL)-003B57.svg?style=flat-square&logo=sqlite)](https://sqlite.org/)
[![License](https://img.shields.io/badge/License-MIT-green.svg?style=flat-square)](LICENSE)
[![Architect](https://img.shields.io/badge/Architect-Eng.%20Ibrahim%20Tarek-059669.svg?style=flat-square)](https://ibrahimelmasry.github.io/)

**DevERP** is a high-performance, native Windows desktop application engineered for independent software engineers, technical consultants, and digital agencies. Designed with an **Offline-First**, **Zero-SaaS** philosophy, DevERP gives developers total data sovereignty over their business relationships, milestones, billing pipelines, and financial cash flows.

---

## Key Features

### 1. Offline-First Client & Project CRM
- **Client Portfolio Management**: Track client organizations, points of contact, emails, phone numbers, billing addresses, and private notes.
- **Project Workspaces**: Organize projects under clients with defined budget caps, billing schemes (Fixed Milestone vs. Hourly), and status lifecycles (`Active`, `OnHold`, `Completed`).
- **Milestone Tracking**: Break complex contracts into actionable milestones with target due dates, deliverable amounts, and completion timestamps.

### 2. Milestone-to-Invoice Pipeline
- **One-Click Billing**: Select any combination of completed, unbilled milestones and instantly transform them into a standardized, numbered invoice (`INV-YYYY-XXX`).
- **Manual Invoice Composer**: Direct drafting capability with custom multi-line items, quantities, unit prices, tax rates, currencies, and exchange rates.
- **Payment Reconciliation**: Marking an invoice as `PAID` automatically records a corresponding Inflow transaction in the Cash Flow ledger, recalculating exchange rates in real-time.

### 3. Multi-Currency Cash Flow Ledger
- **Inflows & Outflows**: Comprehensive ledger for client payments, software subscriptions, hosting fees, cloud infrastructure, hardware purchases, and tax outlays.
- **Multi-Currency Normalization**: Support for `EGP`, `USD`, `EUR`, and custom fiat currencies with real-time conversion into the developer's base currency.
- **30-Day Financial Analytics**: Live calculations of gross inflows, operating expenses, net cash flow, and outstanding receivables on the executive dashboard.

### 4. Vector QuestPDF Invoicing Engine & QR Verification
- **Pixel-Perfect A4 Documents**: Instant C# vector PDF document generation using QuestPDF, featuring professional typography, invoice status badges, and itemized billing tables.
- **Embedded Banking Coordinates**: Automatically includes the developer's bank name, beneficiary name, IBAN, and SWIFT/BIC codes for wire transfers.
- **Verification QR Code**: Generates a 50x50pt QR code using QRCoder encoding the architect's portfolio (`https://ibrahimelmasry.github.io/`) for instant client verification and credential inspection.

### 5. Windows 11 Mica & Ergonomic UI Polish
- **Fluent 2 Design System**: Utilizes [WPF-UI](https://wpfui.lepo.co/) with the native Windows 11 `Mica` backdrop effect, dark mode palette (`#090D16`), and custom gradient accents:
  - Inflow: Emerald (`#059669` → `#34D399`)
  - Outflow: Rose (`#E11D48` → `#FB7185`)
  - Net Flow: Cyan (`#0284C7` → `#38BDF8`)
  - Pending Receivables: Amber (`#D97706` → `#FBBF24`)
- **Alternating Row Tints**: Subtle alternation for invoices and ledger transactions to ensure high legibility.
- **Non-Blocking InfoBar Alerts**: Action notifications for invoice settlement, PDF exports, and database backups with auto-dismiss timers.
- **Window State Persistence**: Remembers window dimensions, coordinates, and maximized state across reboots with monitor bounds validation.
- **One-Click Desktop Shortcut**: Utility in the Settings view to create a Windows shell shortcut (`DevERP.lnk`) on the user's Desktop.

---

## Architectural Layout

DevERP adheres strictly to **Clean Architecture** and the **MVVM (Model-View-ViewModel)** design pattern, structured into three isolated projects:

```
DevERP/
├── DevERP.Core/                    # Pure Domain Layer (Framework Agnostic)
│   ├── Enums/                     # ProjectStatus, BillingType, InvoiceStatus, TransactionType
│   ├── Models/                    # Client, Project, Milestone, Invoice, CashFlowTransaction, DeveloperProfile
│   └── Interfaces/                # IAppDbContext, IInvoicePdfService
│
├── DevERP.Infrastructure/          # Data Access & External Services Layer
│   ├── Data/                      # AppDbContext (EF Core SQLite), Migrations, Auto-seeders
│   └── Services/                  # InvoicePdfService (QuestPDF + QRCoder)
│
└── DevERP.Wpf/                     # Presentation Layer (WPF-UI + MVVM)
    ├── Assets/                    # app.ico (multi-res Windows icon)
    ├── Converters/                # Value converters (StatusToBrush, CurrencyFormat, etc.)
    ├── Services/                  # WindowStateService, DesktopShortcutService
    ├── ViewModels/                # MainViewModel, DashboardViewModel, ClientsViewModel, etc.
    └── Views/                     # DashboardView, ClientsView, InvoicesView, CashFlowView, SettingsView
```

### Data Storage & Portability
- **Database Engine**: SQLite 3 with Write-Ahead Logging (WAL) enabled for safe concurrent writes.
- **Database Path**: `%LocalAppData%\DevERP\deverp.db`
- **Backup & Export**: One-click database snapshot exporter in **Settings** allows effortless backup creation (`DevERP_Backup_YYYYMMDD_HHMMSS.db`).

---

## Getting Started

### Prerequisites
- Windows 10 (Build 19041+) or Windows 11
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Visual Studio 2022 (with *.NET desktop development* workload) or VS Code / JetBrains Rider

### Build from Source

Clone the repository and build the solution:

```pwsh
# Clone repository
git clone https://github.com/IbrahimElmasry/DevERP.git
cd DevERP

# Restore and build solution
dotnet restore DevERP.sln
dotnet build DevERP.sln --configuration Release
```

### Run the Application

Launch the WPF desktop application:

```pwsh
dotnet run --project "DevERP.Wpf\DevERP.Wpf.csproj"
```

Or execute the compiled binary:
```pwsh
& "DevERP.Wpf\bin\Release\net8.0-windows\DevERP.Wpf.exe"
```

### Publish Self-Contained Executable

To generate a standalone executable that runs without requiring .NET pre-installed on the host machine:

```pwsh
dotnet publish DevERP.Wpf\DevERP.Wpf.csproj `
  --configuration Release `
  --runtime win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -o .\publish
```

---

## Lead Architect & Engineering Credentials

**Eng. Ibrahim Tarek**  
*Lead Software Engineer, Systems Architect & Full-Stack Consultant*

- **Specialization**: ASP.NET Core, Enterprise Web Platforms, Native Systems Architecture, High-Concurrency Database Design
- **Portfolio**: [https://ibrahimelmasry.github.io/](https://ibrahimelmasry.github.io/)
- **GitHub**: [@IbrahimElmasry](https://github.com/IbrahimElmasry)
- **LinkedIn**: [in/ibrahim-tarek-62b0a62a4](https://www.linkedin.com/in/ibrahim-tarek-62b0a62a4/)
- **WhatsApp**: [+201019804919](https://wa.me/201019804919)
- **Facebook**: [professur.ibrahim](https://www.facebook.com/professur.ibrahim)
- **Location**: Alexandria, Egypt

For architectural reviews, enterprise consulting, or full-stack software development engagements, please reach out via [WhatsApp](https://wa.me/201019804919) or [LinkedIn](https://www.linkedin.com/in/ibrahim-tarek-62b0a62a4/).

---

## License

This project is licensed under the **MIT License** — see the [LICENSE](LICENSE) file for details.

```
Copyright (c) 2026 Eng. Ibrahim Tarek. All Rights Reserved.
```
