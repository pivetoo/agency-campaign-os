using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class OpportunityWinReason : Entity
    {
        public string Name { get; private set; } = string.Empty;

        public string Color { get; private set; } = "#15803d";

        public int DisplayOrder { get; private set; }

        public bool IsActive { get; private set; } = true;

        private OpportunityWinReason()
        {
        }

        public OpportunityWinReason(string name, string color, int displayOrder)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(color);

            Name = name.Trim();
            Color = color.Trim();
            DisplayOrder = displayOrder;
            CreatedAt = DateTimeOffset.UtcNow;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void Update(string name, string color, int displayOrder, bool isActive)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(color);

            Name = name.Trim();
            Color = color.Trim();
            DisplayOrder = displayOrder;
            IsActive = isActive;
            UpdatedAt = DateTimeOffset.UtcNow;
        }
    }
}
