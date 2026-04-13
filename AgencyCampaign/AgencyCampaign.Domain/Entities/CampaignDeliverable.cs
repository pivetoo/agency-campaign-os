using Archon.Core.Entities;
using AgencyCampaign.Domain.ValueObjects;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class CampaignDeliverable : Entity
    {
        public long CampaignId { get; private set; }

        public Campaign? Campaign { get; private set; }

        public long CreatorId { get; private set; }

        public Creator? Creator { get; private set; }

        public string Title { get; private set; } = string.Empty;

        public string? Description { get; private set; }

        public DateTimeOffset DueAt { get; private set; }

        public DateTimeOffset? PublishedAt { get; private set; }

        public DeliverableStatus Status { get; private set; } = DeliverableStatus.Pending;

        public decimal GrossAmount { get; private set; }

        public decimal CreatorAmount { get; private set; }

        public decimal AgencyFeeAmount { get; private set; }

        private CampaignDeliverable()
        {
        }

        public CampaignDeliverable(long campaignId, long creatorId, string title, DateTimeOffset dueAt, decimal grossAmount, decimal creatorAmount, decimal agencyFeeAmount, string? description = null)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(campaignId);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(creatorId);
            ArgumentException.ThrowIfNullOrWhiteSpace(title);
            ArgumentOutOfRangeException.ThrowIfNegative(grossAmount);
            ArgumentOutOfRangeException.ThrowIfNegative(creatorAmount);
            ArgumentOutOfRangeException.ThrowIfNegative(agencyFeeAmount);

            CampaignId = campaignId;
            CreatorId = creatorId;
            Title = title.Trim();
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
            DueAt = dueAt;
            GrossAmount = grossAmount;
            CreatorAmount = creatorAmount;
            AgencyFeeAmount = agencyFeeAmount;
        }

        public void Update(string title, DateTimeOffset dueAt, decimal grossAmount, decimal creatorAmount, decimal agencyFeeAmount, string? description)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(title);
            ArgumentOutOfRangeException.ThrowIfNegative(grossAmount);
            ArgumentOutOfRangeException.ThrowIfNegative(creatorAmount);
            ArgumentOutOfRangeException.ThrowIfNegative(agencyFeeAmount);

            Title = title.Trim();
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
            DueAt = dueAt;
            GrossAmount = grossAmount;
            CreatorAmount = creatorAmount;
            AgencyFeeAmount = agencyFeeAmount;
        }

        public void ChangeStatus(DeliverableStatus status, DateTimeOffset? publishedAt = null)
        {
            Status = status;
            PublishedAt = status == DeliverableStatus.Published ? publishedAt : null;
        }
    }
}
