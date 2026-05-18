using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class Bank : Entity
    {
        public string Compe { get; private set; } = string.Empty;

        public string? Ispb { get; private set; }

        public string Name { get; private set; } = string.Empty;

        public string ShortName { get; private set; } = string.Empty;

        public string? LogoUrl { get; private set; }

        public bool IsActive { get; private set; } = true;

        public bool IsSystem { get; private set; }

        public string? CreatedByUserName { get; private set; }

        private Bank()
        {
        }

        public Bank(string compe, string name, string shortName, string? ispb = null, string? logoUrl = null, bool isSystem = false, string? createdByUserName = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(compe);
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(shortName);

            Compe = compe.Trim();
            Name = name.Trim();
            ShortName = shortName.Trim();
            Ispb = string.IsNullOrWhiteSpace(ispb) ? null : ispb.Trim();
            LogoUrl = string.IsNullOrWhiteSpace(logoUrl) ? null : logoUrl.Trim();
            IsSystem = isSystem;
            CreatedByUserName = string.IsNullOrWhiteSpace(createdByUserName) ? null : createdByUserName.Trim();
        }

        public void Update(string compe, string name, string shortName, string? ispb, string? logoUrl, bool isActive)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(compe);
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(shortName);

            string normalizedCompe = compe.Trim();
            if (IsSystem && !string.Equals(normalizedCompe, Compe, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("bank.compe.systemReadonly");
            }

            Compe = normalizedCompe;
            Name = name.Trim();
            ShortName = shortName.Trim();
            Ispb = string.IsNullOrWhiteSpace(ispb) ? null : ispb.Trim();
            LogoUrl = string.IsNullOrWhiteSpace(logoUrl) ? null : logoUrl.Trim();
            IsActive = isActive;
        }

        public void SetLogoUrl(string logoUrl)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(logoUrl);
            LogoUrl = logoUrl.Trim();
        }

        public void ResetLogoUrl()
        {
            LogoUrl = IsSystem ? $"/banks/{Compe}.png" : null;
        }
    }
}
