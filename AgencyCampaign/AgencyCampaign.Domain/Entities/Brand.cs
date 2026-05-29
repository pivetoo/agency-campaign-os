using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class Brand : Entity
    {
        public string Name { get; private set; } = string.Empty;

        public string? TradeName { get; private set; }

        public string? Document { get; private set; }

        public string? ContactName { get; private set; }

        public string? ContactEmail { get; private set; }

        public string? ContactPhone { get; private set; }

        public string? Notes { get; private set; }

        public string? LogoUrl { get; private set; }

        public bool IsActive { get; private set; } = true;

        private Brand()
        {
        }

        public Brand(string name, string? tradeName = null, string? document = null, string? contactName = null, string? contactEmail = null, string? notes = null, string? contactPhone = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            Name = name.Trim();
            TradeName = Normalize(tradeName);
            Document = Normalize(document);
            ContactName = Normalize(contactName);
            ContactEmail = Normalize(contactEmail);
            ContactPhone = Normalize(contactPhone);
            Notes = Normalize(notes);
        }

        public void Update(string name, string? tradeName, string? document, string? contactName, string? contactEmail, string? notes, bool isActive, string? contactPhone = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            Name = name.Trim();
            TradeName = Normalize(tradeName);
            Document = Normalize(document);
            ContactName = Normalize(contactName);
            ContactEmail = Normalize(contactEmail);
            ContactPhone = Normalize(contactPhone);
            Notes = Normalize(notes);
            IsActive = isActive;
        }

        public void SetLogo(string? logoUrl)
        {
            LogoUrl = Normalize(logoUrl);
        }

        public void SetContactChannels(string? primaryEmail, string? primaryPhone)
        {
            ContactEmail = Normalize(primaryEmail);
            ContactPhone = Normalize(primaryPhone);
        }

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
