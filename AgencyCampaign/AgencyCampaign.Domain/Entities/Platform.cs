using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class Platform : Entity
    {
        public string Name { get; private set; } = string.Empty;

        public bool IsActive { get; private set; } = true;

        public int DisplayOrder { get; private set; }

        public string? Identifier { get; private set; }

        public bool IsSystem { get; private set; }

        public string? LogoUrl { get; private set; }

        private Platform()
        {
        }

        public Platform(string name, int displayOrder = 0, string? logoUrl = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            DisplayOrder = displayOrder;
            Name = name.Trim();
            LogoUrl = string.IsNullOrWhiteSpace(logoUrl) ? null : logoUrl.Trim();
        }

        public void Update(string name, int displayOrder, bool isActive, string? logoUrl)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            Name = name.Trim();
            DisplayOrder = displayOrder;
            IsActive = isActive;
            LogoUrl = string.IsNullOrWhiteSpace(logoUrl) ? null : logoUrl.Trim();
        }
    }
}
