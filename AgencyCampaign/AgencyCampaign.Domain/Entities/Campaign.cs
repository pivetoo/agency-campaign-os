using AgencyCampaign.Domain.ValueObjects;
using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class Campaign : Entity
    {
        private readonly List<CampaignCreator> campaignCreators = [];
        private readonly List<CampaignDeliverable> deliverables = [];

        public long BrandId { get; private set; }

        public Brand? Brand { get; private set; }

        public string Name { get; private set; } = string.Empty;

        public string? Description { get; private set; }

        public string? Objective { get; private set; }

        public string? Briefing { get; private set; }

        public decimal Budget { get; private set; }

        public DateTimeOffset StartsAt { get; private set; }

        public DateTimeOffset? EndsAt { get; private set; }

        public CampaignStatus Status { get; private set; } = CampaignStatus.Draft;

        public string? InternalOwnerName { get; private set; }

        public string? Notes { get; private set; }

        public bool IsActive { get; private set; } = true;

        public IReadOnlyCollection<CampaignCreator> CampaignCreators => campaignCreators.AsReadOnly();

        public IReadOnlyCollection<CampaignDeliverable> Deliverables => deliverables.AsReadOnly();

        private Campaign()
        {
        }

        public Campaign(long brandId, string name, decimal budget, DateTimeOffset startsAt, string? description = null, string? objective = null, string? briefing = null, DateTimeOffset? endsAt = null, string? internalOwnerName = null, string? notes = null)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(brandId);
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentOutOfRangeException.ThrowIfNegative(budget);

            BrandId = brandId;
            Name = name.Trim();
            Description = Normalize(description);
            Objective = Normalize(objective);
            Briefing = Normalize(briefing);
            Budget = budget;
            StartsAt = startsAt.ToUniversalTime();
            EndsAt = endsAt?.ToUniversalTime();
            InternalOwnerName = Normalize(internalOwnerName);
            Notes = Normalize(notes);
        }

        public void Update(long brandId, string name, decimal budget, DateTimeOffset startsAt, DateTimeOffset? endsAt, string? description, string? objective, string? briefing, CampaignStatus status, string? internalOwnerName, string? notes, bool isActive)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(brandId);
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentOutOfRangeException.ThrowIfNegative(budget);

            BrandId = brandId;
            Name = name.Trim();
            Description = Normalize(description);
            Objective = Normalize(objective);
            Briefing = Normalize(briefing);
            Budget = budget;
            StartsAt = startsAt.ToUniversalTime();
            EndsAt = endsAt?.ToUniversalTime();
            Status = status;
            InternalOwnerName = Normalize(internalOwnerName);
            Notes = Normalize(notes);
            IsActive = isActive;
        }

        public void ChangeStatus(CampaignStatus status)
        {
            Status = status;

            if (status == CampaignStatus.Cancelled || status == CampaignStatus.Completed)
            {
                IsActive = false;
            }
        }

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
