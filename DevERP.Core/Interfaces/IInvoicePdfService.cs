using DevERP.Core.Models;

namespace DevERP.Core.Interfaces;

public interface IInvoicePdfService
{
    byte[] GenerateInvoicePdf(Invoice invoice, DeveloperProfile profile);
    Task GenerateInvoicePdfAsync(Invoice invoice, DeveloperProfile profile, string filePath, CancellationToken cancellationToken = default);
}
