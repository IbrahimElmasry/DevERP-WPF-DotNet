using System.IO;
using DevERP.Core.Interfaces;
using DevERP.Core.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace DevERP.Infrastructure.Services;

public class InvoicePdfService : IInvoicePdfService
{
    static InvoicePdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerateInvoicePdf(Invoice invoice, DeveloperProfile profile)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36); // 0.5 inch / ~1.27 cm
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontFamily("Segoe UI").FontSize(10).FontColor(Colors.Grey.Darken3));

                page.Header().Element(c => ComposeHeader(c, invoice, profile));
                page.Content().Element(c => ComposeContent(c, invoice, profile));
                page.Footer().Element(ComposeFooter);
            });
        });

        return document.GeneratePdf();
    }

    public async Task GenerateInvoicePdfAsync(Invoice invoice, DeveloperProfile profile, string filePath, CancellationToken cancellationToken = default)
    {
        var pdfBytes = GenerateInvoicePdf(invoice, profile);
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        await File.WriteAllBytesAsync(filePath, pdfBytes, cancellationToken);
    }

    private void ComposeHeader(IContainer container, Invoice invoice, DeveloperProfile profile)
    {
        container.Row(row =>
        {
            // Left: Developer identity & optional custom logo
            row.RelativeItem(7).Column(col =>
            {
                if (!string.IsNullOrWhiteSpace(profile.LogoPath) && File.Exists(profile.LogoPath))
                {
                    try
                    {
                        col.Item().PaddingBottom(6).MaxHeight(48).MaxWidth(140).Image(profile.LogoPath);
                    }
                    catch { }
                }

                col.Item().Text(profile.FullName)
                    .FontSize(20)
                    .Bold()
                    .FontColor(Colors.Blue.Darken3);

                col.Item().Text(profile.ProfessionalTitle)
                    .FontSize(11)
                    .FontColor(Colors.Grey.Darken1);

                col.Item().PaddingTop(4).Text($"{profile.Email}  |  {profile.Phone}")
                    .FontSize(9)
                    .FontColor(Colors.Grey.Darken2);

                if (!string.IsNullOrWhiteSpace(profile.Address))
                {
                    col.Item().Text(profile.Address).FontSize(9).FontColor(Colors.Grey.Darken2);
                }

                if (!string.IsNullOrWhiteSpace(profile.TaxNumber))
                {
                    col.Item().Text($"Tax ID: {profile.TaxNumber}").FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                }
            });

            // Right: Invoice number & status
            row.RelativeItem(5).AlignRight().Column(col =>
            {
                col.Item().Text("INVOICE")
                    .FontSize(24)
                    .ExtraBold()
                    .FontColor(Colors.Grey.Darken4);

                col.Item().Text($"#{invoice.InvoiceNumber}")
                    .FontSize(12)
                    .Bold()
                    .FontColor(Colors.Blue.Darken2);

                // Status pill
                var statusColor = invoice.Status switch
                {
                    Core.Enums.InvoiceStatus.Paid => Colors.Green.Darken2,
                    Core.Enums.InvoiceStatus.Sent => Colors.Blue.Darken2,
                    Core.Enums.InvoiceStatus.Overdue => Colors.Red.Darken2,
                    _ => Colors.Grey.Darken2
                };

                var statusBg = invoice.Status switch
                {
                    Core.Enums.InvoiceStatus.Paid => Colors.Green.Lighten5,
                    Core.Enums.InvoiceStatus.Sent => Colors.Blue.Lighten5,
                    Core.Enums.InvoiceStatus.Overdue => Colors.Red.Lighten5,
                    _ => Colors.Grey.Lighten4
                };

                col.Item().PaddingTop(4).Container()
                    .Background(statusBg)
                    .PaddingVertical(3)
                    .PaddingHorizontal(8)
                    .Text(invoice.Status.ToString().ToUpperInvariant())
                    .FontSize(9)
                    .Bold()
                    .FontColor(statusColor);
            });
        });
    }

    private void ComposeContent(IContainer container, Invoice invoice, DeveloperProfile profile)
    {
        container.PaddingVertical(16).Column(col =>
        {
            // Horizontal divider line
            col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

            // Invoice Dates & Client Information Box
            col.Item().PaddingTop(12).Row(row =>
            {
                // Bill To
                row.RelativeItem(6).Column(billTo =>
                {
                    billTo.Item().Text("BILL TO:")
                        .FontSize(9)
                        .Bold()
                        .FontColor(Colors.Grey.Darken1);

                    billTo.Item().PaddingTop(2).Text(invoice.Client?.Name ?? "Client")
                        .FontSize(12)
                        .Bold()
                        .FontColor(Colors.Grey.Darken3);

                    if (!string.IsNullOrWhiteSpace(invoice.Client?.Company))
                    {
                        billTo.Item().Text(invoice.Client.Company).FontSize(10).SemiBold();
                    }

                    if (!string.IsNullOrWhiteSpace(invoice.Client?.Email))
                    {
                        billTo.Item().Text(invoice.Client.Email).FontSize(9);
                    }

                    if (!string.IsNullOrWhiteSpace(invoice.Client?.Phone))
                    {
                        billTo.Item().Text(invoice.Client.Phone).FontSize(9);
                    }

                    if (!string.IsNullOrWhiteSpace(invoice.Client?.Address))
                    {
                        billTo.Item().Text(invoice.Client.Address).FontSize(9);
                    }
                });

                // Invoice metadata (Dates & Currency)
                row.RelativeItem(6).AlignRight().Column(meta =>
                {
                    meta.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Issue Date:").FontSize(9).FontColor(Colors.Grey.Darken1);
                        r.RelativeItem().AlignRight().Text(invoice.IssueDate.ToString("yyyy-MM-dd")).FontSize(9).Bold();
                    });

                    meta.Item().PaddingTop(2).Row(r =>
                    {
                        r.RelativeItem().Text("Due Date:").FontSize(9).FontColor(Colors.Grey.Darken1);
                        r.RelativeItem().AlignRight().Text(invoice.DueDate.ToString("yyyy-MM-dd")).FontSize(9).Bold();
                    });

                    meta.Item().PaddingTop(2).Row(r =>
                    {
                        r.RelativeItem().Text("Currency:").FontSize(9).FontColor(Colors.Grey.Darken1);
                        r.RelativeItem().AlignRight().Text(invoice.Currency).FontSize(9).Bold();
                    });

                    if (invoice.PaidAt.HasValue)
                    {
                        meta.Item().PaddingTop(2).Row(r =>
                        {
                            r.RelativeItem().Text("Paid On:").FontSize(9).FontColor(Colors.Green.Darken2);
                            r.RelativeItem().AlignRight().Text(invoice.PaidAt.Value.ToString("yyyy-MM-dd")).FontSize(9).Bold().FontColor(Colors.Green.Darken2);
                        });
                    }
                });
            });

            // Spacing
            col.Item().PaddingVertical(14);

            // Line items table
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(30);  // #
                    columns.RelativeColumn(6);   // Description
                    columns.RelativeColumn(2);   // Unit Price
                    columns.RelativeColumn(1.5f);// Qty
                    columns.RelativeColumn(2);   // Total
                });

                table.Header(header =>
                {
                    header.Cell().Background(Colors.Blue.Darken3).Padding(6).Text("#").Bold().FontColor(Colors.White).FontSize(9);
                    header.Cell().Background(Colors.Blue.Darken3).Padding(6).Text("Description").Bold().FontColor(Colors.White).FontSize(9);
                    header.Cell().Background(Colors.Blue.Darken3).Padding(6).AlignRight().Text("Unit Price").Bold().FontColor(Colors.White).FontSize(9);
                    header.Cell().Background(Colors.Blue.Darken3).Padding(6).AlignCenter().Text("Qty").Bold().FontColor(Colors.White).FontSize(9);
                    header.Cell().Background(Colors.Blue.Darken3).Padding(6).AlignRight().Text($"Total ({invoice.Currency})").Bold().FontColor(Colors.White).FontSize(9);
                });

                var idx = 1;
                foreach (var item in invoice.Items)
                {
                    var bg = idx % 2 == 0 ? Colors.Grey.Lighten4 : Colors.White;

                    table.Cell().Background(bg).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(6).Text(idx.ToString()).FontSize(9);
                    table.Cell().Background(bg).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(6).Text(item.Description).FontSize(9);
                    table.Cell().Background(bg).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(6).AlignRight().Text($"{item.UnitPrice:N2}").FontSize(9);
                    table.Cell().Background(bg).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(6).AlignCenter().Text($"{item.Quantity:N0}").FontSize(9);
                    table.Cell().Background(bg).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(6).AlignRight().Text($"{item.TotalPrice:N2}").FontSize(9).Bold();

                    idx++;
                }
            });

            // Summary Totals
            col.Item().PaddingTop(10).Row(row =>
            {
                row.RelativeItem(7); // Spacer
                row.RelativeItem(5).Column(totals =>
                {
                    totals.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).PaddingVertical(3).Row(r =>
                    {
                        r.RelativeItem().Text("Subtotal:").FontSize(9.5f).FontColor(Colors.Grey.Darken1);
                        r.RelativeItem().AlignRight().Text($"{invoice.SubTotal:N2} {invoice.Currency}").FontSize(9.5f).Bold();
                    });

                    if (invoice.TaxAmount > 0)
                    {
                        totals.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).PaddingVertical(3).Row(r =>
                        {
                            r.RelativeItem().Text($"Tax ({invoice.TaxRate}%):").FontSize(9.5f).FontColor(Colors.Grey.Darken1);
                            r.RelativeItem().AlignRight().Text($"{invoice.TaxAmount:N2} {invoice.Currency}").FontSize(9.5f);
                        });
                    }

                    totals.Item().PaddingTop(4).Container()
                        .Background(Colors.Blue.Lighten5)
                        .Padding(6)
                        .Row(r =>
                        {
                            r.RelativeItem().Text("TOTAL DUE:").FontSize(11).Bold().FontColor(Colors.Blue.Darken4);
                            r.RelativeItem().AlignRight().Text($"{invoice.TotalAmount:N2} {invoice.Currency}").FontSize(12).Bold().FontColor(Colors.Blue.Darken4);
                        });
                });
            });

            // Payment Coordinates (Bank Wire / InstaPay IPN / Both)
            col.Item().PaddingTop(18).Column(payCol =>
            {
                if (invoice.PaymentMethod == Core.Enums.PaymentMethod.BankWire || invoice.PaymentMethod == Core.Enums.PaymentMethod.Both)
                {
                    payCol.Item().PaddingBottom(invoice.PaymentMethod == Core.Enums.PaymentMethod.Both ? 8 : 0).Container()
                        .Background(Colors.Grey.Lighten4)
                        .Border(1)
                        .BorderColor(Colors.Grey.Lighten2)
                        .Padding(12)
                        .Column(bank =>
                        {
                            bank.Item().Text("BANK WIRE TRANSFER COORDINATES")
                                .FontSize(9.5f)
                                .Bold()
                                .FontColor(Colors.Blue.Darken3);

                            bank.Item().PaddingTop(4).Row(r =>
                            {
                                r.RelativeItem(4).Text("Beneficiary Name:").FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                                r.RelativeItem(8).Text(profile.BankAccountHolder).FontSize(8.5f).Bold();
                            });

                            bank.Item().PaddingTop(2).Row(r =>
                            {
                                r.RelativeItem(4).Text("Bank Name:").FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                                r.RelativeItem(8).Text(profile.BankName).FontSize(8.5f).Bold();
                            });

                            bank.Item().PaddingTop(2).Row(r =>
                            {
                                r.RelativeItem(4).Text("IBAN:").FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                                r.RelativeItem(8).Text(profile.Iban).FontSize(8.5f).Bold().FontColor(Colors.Blue.Darken3);
                            });

                            bank.Item().PaddingTop(2).Row(r =>
                            {
                                r.RelativeItem(4).Text("SWIFT / BIC:").FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                                r.RelativeItem(8).Text(profile.SwiftBic).FontSize(8.5f).Bold();
                            });
                        });
                }

                if (invoice.PaymentMethod == Core.Enums.PaymentMethod.InstaPay || invoice.PaymentMethod == Core.Enums.PaymentMethod.Both)
                {
                    payCol.Item().Container()
                        .Background(Colors.Green.Lighten5)
                        .Border(1)
                        .BorderColor(Colors.Green.Lighten2)
                        .Padding(12)
                        .Column(ipn =>
                        {
                            ipn.Item().Row(ipnRow =>
                            {
                                ipnRow.RelativeItem().Text("INSTAPAY EGYPT (IPN) — INSTANT PAYMENT COORDINATES")
                                    .FontSize(9.5f)
                                    .Bold()
                                    .FontColor(Colors.Green.Darken3);

                                ipnRow.AutoItem().Container()
                                    .Background(Colors.Green.Darken2)
                                    .PaddingVertical(2)
                                    .PaddingHorizontal(6)
                                    .Text("INSTANT")
                                    .FontSize(7.5f)
                                    .Bold()
                                    .FontColor(Colors.White);
                            });

                            ipn.Item().PaddingTop(4).Row(r =>
                            {
                                r.RelativeItem(4).Text("InstaPay Address (IPA):").FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                                r.RelativeItem(8).Text(profile.InstaPayAddress).FontSize(8.5f).Bold().FontColor(Colors.Green.Darken3);
                            });

                            ipn.Item().PaddingTop(2).Row(r =>
                            {
                                r.RelativeItem(4).Text("Registered Mobile:").FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                                r.RelativeItem(8).Text(profile.InstaPayPhone).FontSize(8.5f).Bold();
                            });

                            ipn.Item().PaddingTop(2).Row(r =>
                            {
                                r.RelativeItem(4).Text("Account Name:").FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                                r.RelativeItem(8).Text(profile.FullName).FontSize(8.5f).Bold();
                            });

                            ipn.Item().PaddingTop(3).Text("Pay instantly from any Egyptian bank or mobile wallet via the InstaPay application using the IPA address or mobile number above.")
                                .FontSize(7.5f)
                                .Italic()
                                .FontColor(Colors.Grey.Darken2);
                        });
                }
            });

            // Notes / Payment Instructions
            if (!string.IsNullOrWhiteSpace(invoice.Notes))
            {
                col.Item().PaddingTop(10).Column(notes =>
                {
                    notes.Item().Text("Notes & Payment Instructions:").FontSize(8.5f).Bold().FontColor(Colors.Grey.Darken2);
                    notes.Item().PaddingTop(2).Text(invoice.Notes).FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                });
            }
        });
    }

    private static readonly byte[] _portfolioQrBytes = GenerateQrCodeBytes("https://ibrahimelmasry.github.io/");

    private static byte[] GenerateQrCodeBytes(string url)
    {
        using var qrGenerator = new QRCoder.QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(url, QRCoder.QRCodeGenerator.ECCLevel.M);
        var qrCode = new QRCoder.PngByteQRCode(qrCodeData);
        return qrCode.GetGraphic(10);
    }

    private void ComposeFooter(IContainer container)
    {
        container.Column(footerCol =>
        {
            // Clean, professional two-column bordered block
            footerCol.Item()
                .Border(1)
                .BorderColor(Colors.Grey.Lighten2)
                .Background(Colors.Grey.Lighten5)
                .Padding(8)
                .Row(row =>
                {
                    // Left Column: Developer credentials & contact details
                    row.RelativeItem(9).Column(leftCol =>
                    {
                        leftCol.Item().Text("Engineered by: Eng. Ibrahim Tarek (Lead Software Engineer & Architect)")
                            .FontSize(8.5f)
                            .Bold()
                            .FontColor(Colors.Blue.Darken3);

                        leftCol.Item().PaddingTop(2).Row(r =>
                        {
                            r.AutoItem().Text("Portfolio: ").FontSize(8).Bold().FontColor(Colors.Grey.Darken2);
                            r.AutoItem().Text("https://ibrahimelmasry.github.io/").FontSize(8).Underline().FontColor(Colors.Blue.Darken2);
                        });

                        leftCol.Item().PaddingTop(1).Text("WhatsApp: +201019804919 | LinkedIn: in/ibrahim-tarek-62b0a62a4")
                            .FontSize(7.5f)
                            .FontColor(Colors.Grey.Darken2);

                        leftCol.Item().PaddingTop(1).Text("GitHub: github.com/IbrahimElmasry | Location: Alexandria, Egypt")
                            .FontSize(7.5f)
                            .FontColor(Colors.Grey.Darken2);
                    });

                    // Right Column: Sharp 50x50pt QR code navigating to portfolio + label
                    row.RelativeItem(3).AlignRight().Column(rightCol =>
                    {
                        rightCol.Item().AlignCenter().Width(50).Height(50).Image(_portfolioQrBytes);
                        rightCol.Item().AlignCenter().PaddingTop(2).Text("Scan for Portfolio & Verification")
                            .FontSize(6.5f)
                            .SemiBold()
                            .FontColor(Colors.Grey.Darken2);
                    });
                });

            // Sub-row: Generation notice & Page numbers
            footerCol.Item().PaddingTop(4).Row(row =>
            {
                row.RelativeItem().Text("Generated securely by DevERP Native Desktop — Thank you for your partnership!")
                    .FontSize(7.5f)
                    .FontColor(Colors.Grey.Darken1);

                row.RelativeItem().AlignRight().Text(text =>
                {
                    text.Span("Page ").FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                    text.CurrentPageNumber().FontSize(7.5f).Bold();
                    text.Span(" of ").FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                    text.TotalPages().FontSize(7.5f).Bold();
                });
            });
        });
    }
}
