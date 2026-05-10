using AgencyCampaign.Domain.ValueObjects;
using Archon.Core.Entities;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class CampaignDeliverable : Entity
    {
        private const int DueSoonThresholdInDays = 3;

        [NotMapped]
        public int DaysUntilDue => (int)Math.Floor((DueAt - DateTimeOffset.UtcNow).TotalDays);

        [NotMapped]
        public DeliverableSlaStatus SlaStatus
        {
            get
            {
                if (Status == DeliverableStatus.Published || Status == DeliverableStatus.Cancelled)
                {
                    return DeliverableSlaStatus.Ok;
                }

                int days = DaysUntilDue;
                if (days < 0)
                {
                    return DeliverableSlaStatus.Overdue;
                }

                if (days <= DueSoonThresholdInDays)
                {
                    return DeliverableSlaStatus.DueSoon;
                }

                return DeliverableSlaStatus.Ok;
            }
        }

        private readonly List<DeliverableApproval> approvals = [];

        public long CampaignId { get; private set; }

        public Campaign? Campaign { get; private set; }

        public long CampaignCreatorId { get; private set; }

        public CampaignCreator? CampaignCreator { get; private set; }

        public string Title { get; private set; } = string.Empty;

        public string? Description { get; private set; }

        public long DeliverableKindId { get; private set; }

        public DeliverableKind? DeliverableKind { get; private set; }

        public long PlatformId { get; private set; }

        public Platform? Platform { get; private set; }

        public DateTimeOffset DueAt { get; private set; }

        public DateTimeOffset? PublishedAt { get; private set; }

        public string? PublishedUrl { get; private set; }

        public string? EvidenceUrl { get; private set; }

        public DeliverableStatus Status { get; private set; } = DeliverableStatus.Pending;

        public decimal GrossAmount { get; private set; }

        public decimal CreatorAmount { get; private set; }

        public decimal AgencyFeeAmount { get; private set; }

        public string? Notes { get; private set; }

        public IReadOnlyCollection<DeliverableApproval> Approvals => approvals.AsReadOnly();

        private CampaignDeliverable()
        {
        }

        public CampaignDeliverable(long campaignId, long campaignCreatorId, string title, long deliverableKindId, long platformId, DateTimeOffset dueAt, decimal grossAmount, decimal creatorAmount, decimal agencyFeeAmount, string? description = null, string? notes = null)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(campaignId);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(campaignCreatorId);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(deliverableKindId);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(platformId);
            ArgumentException.ThrowIfNullOrWhiteSpace(title);
            ArgumentOutOfRangeException.ThrowIfNegative(grossAmount);
            ArgumentOutOfRangeException.ThrowIfNegative(creatorAmount);
            ArgumentOutOfRangeException.ThrowIfNegative(agencyFeeAmount);
            EnsureAmountsConsistent(grossAmount, creatorAmount, agencyFeeAmount);

            CampaignId = campaignId;
            CampaignCreatorId = campaignCreatorId;
            Title = title.Trim();
            Description = Normalize(description);
            DeliverableKindId = deliverableKindId;
            PlatformId = platformId;
            DueAt = dueAt.ToUniversalTime();
            GrossAmount = grossAmount;
            CreatorAmount = creatorAmount;
            AgencyFeeAmount = agencyFeeAmount;
            Notes = Normalize(notes);
        }

        public void Update(string title, long deliverableKindId, long platformId, DateTimeOffset dueAt, decimal grossAmount, decimal creatorAmount, decimal agencyFeeAmount, string? description, string? notes)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(title);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(deliverableKindId);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(platformId);
            ArgumentOutOfRangeException.ThrowIfNegative(grossAmount);
            ArgumentOutOfRangeException.ThrowIfNegative(creatorAmount);
            ArgumentOutOfRangeException.ThrowIfNegative(agencyFeeAmount);
            EnsureAmountsConsistent(grossAmount, creatorAmount, agencyFeeAmount);

            Title = title.Trim();
            Description = Normalize(description);
            DeliverableKindId = deliverableKindId;
            PlatformId = platformId;
            DueAt = dueAt.ToUniversalTime();
            GrossAmount = grossAmount;
            CreatorAmount = creatorAmount;
            AgencyFeeAmount = agencyFeeAmount;
            Notes = Normalize(notes);
        }

        public void Publish(string publishedUrl, string? evidenceUrl, DateTimeOffset publishedAt)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(publishedUrl);

            PublishedUrl = publishedUrl.Trim();
            EvidenceUrl = Normalize(evidenceUrl);
            PublishedAt = publishedAt.ToUniversalTime();
            Status = DeliverableStatus.Published;
        }

        public void UpdateEvidence(string? evidenceUrl)
        {
            EvidenceUrl = Normalize(evidenceUrl);
        }

        public void ChangeStatus(DeliverableStatus status)
        {
            Status = status;

            if (status != DeliverableStatus.Published)
            {
                PublishedAt = null;
                PublishedUrl = null;
            }
        }

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static void EnsureAmountsConsistent(decimal grossAmount, decimal creatorAmount, decimal agencyFeeAmount)
        {
            if (creatorAmount + agencyFeeAmount > grossAmount)
            {
                throw new InvalidOperationException("Creator amount plus agency fee cannot exceed deliverable gross amount.");
            }
        }
    }
}
