using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class Campaign : Entity
    {
        private readonly List<CampaignDeliverable> deliverables = [];

        public long BrandId { get; private set; }

        public Brand? Brand { get; private set; }

        public string Name { get; private set; } = string.Empty;

        public string? Description { get; private set; }

        public decimal Budget { get; private set; }

        public DateTimeOffset StartsAt { get; private set; }

        public DateTimeOffset? EndsAt { get; private set; }

        public bool IsActive { get; private set; } = true;

        public IReadOnlyCollection<CampaignDeliverable> Deliverables => deliverables.AsReadOnly();

        private Campaign()
        {
        }

        public Campaign(long brandId, string name, decimal budget, DateTimeOffset startsAt, string? description = null, DateTimeOffset? endsAt = null)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(brandId);
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentOutOfRangeException.ThrowIfNegative(budget);

            BrandId = brandId;
            Name = name.Trim();
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
            Budget = budget;
            StartsAt = startsAt;
            EndsAt = endsAt;
        }

        public void Update(long brandId, string name, decimal budget, DateTimeOffset startsAt, DateTimeOffset? endsAt, string? description, bool isActive)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(brandId);
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentOutOfRangeException.ThrowIfNegative(budget);

            BrandId = brandId;
            Name = name.Trim();
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
            Budget = budget;
            StartsAt = startsAt;
            EndsAt = endsAt;
            IsActive = isActive;
        }
    }
}
