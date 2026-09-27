namespace DevERP.Core.Models;

public class DeveloperProfile
{
    public int Id { get; set; } = 1;
    public string FullName { get; set; } = "Ibrahim Tarek";
    public string ProfessionalTitle { get; set; } = "Software Engineer & Consultant";
    public string Email { get; set; } = "ibrahim@deverp.local";
    public string Phone { get; set; } = "+20 100 123 4567";
    public string? Address { get; set; } = "Cairo, Egypt";
    public string? TaxNumber { get; set; } = "EG-TAX-982143";

    // Bank wire coordinates
    public string BankName { get; set; } = "National Bank of Egypt (NBE)";
    public string BankAccountHolder { get; set; } = "Ibrahim Tarek";
    public string Iban { get; set; } = "EG380001000100000012345678901";
    public string SwiftBic { get; set; } = "NBEGEGCX001";

    // InstaPay Egypt (IPN) coordinates
    public string InstaPayAddress { get; set; } = "ibrahim@instapay";
    public string InstaPayPhone { get; set; } = "+20 101 980 4919";

    // Currency & FX settings
    public string BaseCurrency { get; set; } = "EGP";
    public decimal UsdToEgpRate { get; set; } = 48.50m;
    public decimal EurToEgpRate { get; set; } = 52.00m;
    public decimal SarToEgpRate { get; set; } = 12.95m;

    // Custom Logo for Invoices
    public string? LogoPath { get; set; }

    // Security & Authentication (4-Digit PIN)
    public bool IsPinEnabled { get; set; } = true;
    public string SecurityPin { get; set; } = "1234";
    public int AutoLockMinutes { get; set; } = 0; // 0 = disabled
}
