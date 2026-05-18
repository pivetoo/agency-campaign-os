namespace AgencyCampaign.Application.Models.Financial
{
    public sealed class BankModel
    {
        public long Id { get; set; }
        public string Compe { get; set; } = string.Empty;
        public string? Ispb { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ShortName { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
        public bool IsActive { get; set; }
        public bool IsSystem { get; set; }
        public string? CreatedByUserName { get; set; }
    }
}
