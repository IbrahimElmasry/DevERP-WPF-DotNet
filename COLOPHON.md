# COLOPHON

## Project Identity & Architectural Specification

**Product Name:** DevERP Native Desktop  
**Release Version:** v1.0.0 LTS (Long-Term Support)  
**System Classification:** Autonomous Business Operating System for Independent Software Engineers & Technical Consultants  
**Target Environment:** Microsoft Windows 11 / Windows 10 (x64, ARM64)  
**Primary Repository:** [DevERP Solution](https://github.com/IbrahimElmasry)

---

## Technical Stack & Systems Engineering

DevERP is engineered from the ground up as a native, high-performance desktop application adhering strictly to **Clean Architecture**, **MVVM (Model-View-ViewModel)** design patterns, and **Offline-First Data Sovereignty principles**.

| Component / Layer | Technology & Framework | Purpose & Implementation Details |
| :--- | :--- | :--- |
| **Runtime & Core SDK** | Microsoft .NET 8 / .NET 9 LTS | C# 12/13, Ahead-of-Time (AOT) ready, Nullable Reference Types, Zero-allocation idioms |
| **Presentation Framework** | Windows Presentation Foundation (WPF) | High-performance DirectX hardware-accelerated rendering pipeline |
| **Design System & UI Library** | [WPF-UI](https://wpfui.lepo.co/) (v4.0.0) | Windows 11 Fluent 2 Design System with native Mica backdrop material and dark theme |
| **MVVM Architecture** | Microsoft CommunityToolkit.Mvvm (v8.3.2) | Source-generated `ObservableObject`, `RelayCommand`, `AsyncRelayCommand` |
| **Data Access & ORM** | Entity Framework Core (v8.0.11) | Code-First migrations, automated WAL-mode initialization, zero server dependency |
| **Local Database Engine** | SQLite 3 (`Microsoft.EntityFrameworkCore.Sqlite`) | Stored locally in `%LocalAppData%\DevERP\deverp.db` with automated migrations |
| **Document Generation Engine** | [QuestPDF](https://www.questpdf.com/) (v2024.12.1) | Declarative C# vector PDF document synthesizer with sub-second compilation |
| **Barcode & QR Synthesis** | [QRCoder](https://github.com/codebude/QRCoder) (v1.8.0) | Cryptographic-grade offline QR code matrix generation for invoice verification |
| **Desktop Shell Integration** | Windows Script Host (`WScript.Shell` COM) | One-click desktop shortcut creation with embedded multi-resolution icon assets |
| **Window State Memory** | Custom High-DPI Window State Serialization | Remembers bounds and window state across restarts with display boundary safety checks |

---

## Design Philosophy

1. **Zero External Dependencies / Complete Data Sovereignty:**  
   Unlike cloud-tethered SaaS platforms that charge recurring monthly subscriptions and risk client confidentiality, DevERP runs completely offline. All database records, clients, projects, milestones, financial ledger transactions, and banking coordinates remain strictly on the host workstation.

2. **Clean Separation of Concerns:**  
   The solution is strictly partitioned into three isolated tiers:
   - `DevERP.Core`: Pure domain abstractions, business entities, enumerations, and contract interfaces with zero third-party framework dependencies.
   - `DevERP.Infrastructure`: Data persistence with EF Core SQLite, connection management, schema seeding, and document generation via QuestPDF & QRCoder.
   - `DevERP.Wpf`: Presentation views, ViewModels, value converters, desktop services, and design assets.

3. **Pixel-Perfect Aesthetics & Modern Desktop Ergonomics:**  
   DevERP utilizes Windows 11's Mica material to softly incorporate the user's desktop wallpaper behind a deep slate dark palette (`#090D16`), complemented by vibrant status pills, gradient KPI borders, and non-blocking `ui:InfoBar` toast notifications.

---

## Lead Architect & Engineering Credentials

DevERP was designed, architected, and engineered by:

**Eng. Ibrahim Tarek**  
*Lead Software Engineer, Systems Architect & Full-Stack Consultant*

- **Core Specialization:** ASP.NET Core, Enterprise Web Platforms, Native Windows Desktop Systems Architecture, High-Concurrency Database Design
- **Portfolio:** [https://ibrahimelmasry.github.io/](https://ibrahimelmasry.github.io/)
- **GitHub:** [https://github.com/IbrahimElmasry](https://github.com/IbrahimElmasry)
- **LinkedIn:** [https://www.linkedin.com/in/ibrahim-tarek-62b0a62a4/](https://www.linkedin.com/in/ibrahim-tarek-62b0a62a4/)
- **Facebook:** [https://www.facebook.com/professur.ibrahim](https://www.facebook.com/professur.ibrahim)
- **WhatsApp:** [+201019804919](https://wa.me/201019804919)
- **Location:** Alexandria, Egypt

---

## Intellectual Property & Licensing

- **License:** MIT License (Open Source, Commercial Friendly)
- **Copyright:** (c) 2026 Eng. Ibrahim Tarek. All Rights Reserved.
- **QuestPDF License Compliance:** Community License for open-source and individual software developers.
