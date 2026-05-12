using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class AgencySettings : Entity
    {
        public string AgencyName { get; private set; } = string.Empty;

        public string? TradeName { get; private set; }

        public string? Document { get; private set; }

        public string? PrimaryEmail { get; private set; }

        public string? Phone { get; private set; }

        public string? Address { get; private set; }

        public string? LogoUrl { get; private set; }

        public string? PrimaryColor { get; private set; }

        public long? DefaultEmailConnectorId { get; private set; }

        public string? ProposalHtmlTemplate { get; private set; }

        private AgencySettings()
        {
        }

        public AgencySettings(string agencyName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(agencyName);
            AgencyName = agencyName.Trim();
        }

        public void Update(string agencyName, string? tradeName, string? document, string? primaryEmail, string? phone, string? address, string? logoUrl, string? primaryColor, long? defaultEmailConnectorId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(agencyName);

            AgencyName = agencyName.Trim();
            TradeName = Normalize(tradeName);
            Document = Normalize(document);
            PrimaryEmail = Normalize(primaryEmail);
            Phone = Normalize(phone);
            Address = Normalize(address);
            LogoUrl = Normalize(logoUrl);
            PrimaryColor = Normalize(primaryColor);
            DefaultEmailConnectorId = defaultEmailConnectorId;
        }

        public void SetLogo(string? logoUrl)
        {
            LogoUrl = Normalize(logoUrl);
        }

        public void SetProposalHtmlTemplate(string? template)
        {
            ProposalHtmlTemplate = string.IsNullOrWhiteSpace(template) ? null : template.Trim();
        }

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
