using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class OpportunityTag : Entity
    {
        public string Name { get; private set; } = string.Empty;

        public string Color { get; private set; } = "#6366f1";

        public bool IsActive { get; private set; } = true;

        private OpportunityTag()
        {
        }

        public OpportunityTag(string name, string color)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(color);

            Name = name.Trim();
            Color = color.Trim();
            CreatedAt = DateTimeOffset.UtcNow;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void Update(string name, string color, bool isActive)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(color);

            Name = name.Trim();
            Color = color.Trim();
            IsActive = isActive;
            UpdatedAt = DateTimeOffset.UtcNow;
        }
    }
}
