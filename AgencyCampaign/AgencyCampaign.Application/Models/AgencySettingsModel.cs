namespace AgencyCampaign.Application.Models
{
    public sealed class AgencySettingsModel
    {
        public long Id { get; set; }
        public string AgencyName { get; set; } = string.Empty;
        public string? TradeName { get; set; }
        public string? Document { get; set; }
        public string? PrimaryEmail { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? LogoUrl { get; set; }
        public string? PrimaryColor { get; set; }
        public string? ProposalHtmlTemplate { get; set; }
        public long? WhatsAppConnectorId { get; set; }
    }
}
