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

        public int? Likes { get; private set; }

        public int? Comments { get; private set; }

        public long? Views { get; private set; }

        public long? Reach { get; private set; }

        public long? Impressions { get; private set; }

        public int? Saves { get; private set; }

        public int? Shares { get; private set; }

        public decimal? EngagementRate { get; private set; }

        public DateTimeOffset? MetricsCollectedAt { get; private set; }

        public DeliverableMetricsSource MetricsSource { get; private set; } = DeliverableMetricsSource.None;

        public DateTimeOffset? DeadlineReminderSentAt { get; private set; }

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
            EnsureDueAtNotInPast(dueAt);

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

            DateTimeOffset normalizedDueAt = dueAt.ToUniversalTime();
            if (DueAt != normalizedDueAt)
            {
                EnsureDueAtNotInPast(dueAt);
                DeadlineReminderSentAt = null;
            }

            Title = title.Trim();
            Description = Normalize(description);
            DeliverableKindId = deliverableKindId;
            PlatformId = platformId;
            DueAt = normalizedDueAt;
            GrossAmount = grossAmount;
            CreatorAmount = creatorAmount;
            AgencyFeeAmount = agencyFeeAmount;
            Notes = Normalize(notes);
        }

        public void MarkDeadlineReminderSent()
        {
            DeadlineReminderSentAt = DateTimeOffset.UtcNow;
        }

        public void Publish(string publishedUrl, string? evidenceUrl, DateTimeOffset publishedAt)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(publishedUrl);
            EnsureTransitionAllowed(DeliverableStatus.Published);

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
            EnsureTransitionAllowed(status);

            Status = status;

            if (status != DeliverableStatus.Published)
            {
                PublishedAt = null;
                PublishedUrl = null;
            }
        }

        // Maquina de estados do entregavel (D3i): estados ativos (Pending/InReview/Approved) transitam
        // livremente entre si, para Published (com gate de aprovacao separado) ou Cancelled. Published e
        // terminal exceto por Cancelamento - NAO pode reverter para estado ativo (lastro/repasse ja
        // podem ter ocorrido). Cancelled e terminal (nao reabre).
        private void EnsureTransitionAllowed(DeliverableStatus target)
        {
            if (!IsTransitionAllowed(Status, target))
            {
                throw new InvalidOperationException("deliverable.status.invalidTransition");
            }
        }

        private static bool IsTransitionAllowed(DeliverableStatus from, DeliverableStatus to)
        {
            if (from == to)
            {
                return true;
            }

            return from switch
            {
                DeliverableStatus.Pending => to is DeliverableStatus.InReview or DeliverableStatus.Approved or DeliverableStatus.Published or DeliverableStatus.Cancelled,
                DeliverableStatus.InReview => to is DeliverableStatus.Pending or DeliverableStatus.Approved or DeliverableStatus.Published or DeliverableStatus.Cancelled,
                DeliverableStatus.Approved => to is DeliverableStatus.Pending or DeliverableStatus.InReview or DeliverableStatus.Published or DeliverableStatus.Cancelled,
                DeliverableStatus.Published => to is DeliverableStatus.Cancelled,
                DeliverableStatus.Cancelled => false,
                _ => false,
            };
        }

        public void RegisterMetrics(int? likes, int? comments, long? views, long? reach, long? impressions, int? saves, int? shares, DeliverableMetricsSource source)
        {
            EnsureNonNegative(likes);
            EnsureNonNegative(comments);
            EnsureNonNegative(views);
            EnsureNonNegative(reach);
            EnsureNonNegative(impressions);
            EnsureNonNegative(saves);
            EnsureNonNegative(shares);

            Likes = likes;
            Comments = comments;
            Views = views;
            Reach = reach;
            Impressions = impressions;
            Saves = saves;
            Shares = shares;
            EngagementRate = ComputeEngagementRate();
            MetricsCollectedAt = DateTimeOffset.UtcNow;
            MetricsSource = source;
        }

        public void RegisterPublicMetrics(int? likes, int? comments, long? views, int? shares)
        {
            EnsureNonNegative(likes);
            EnsureNonNegative(comments);
            EnsureNonNegative(views);
            EnsureNonNegative(shares);

            Likes = likes;
            Comments = comments;
            Views = views;
            Shares = shares;
            EngagementRate = ComputeEngagementRate();
            MetricsCollectedAt = DateTimeOffset.UtcNow;
            MetricsSource = Reach.HasValue || Impressions.HasValue || Saves.HasValue
                ? DeliverableMetricsSource.Mixed
                : DeliverableMetricsSource.Auto;
        }

        public void RegisterCreatorInsights(long? reach, long? impressions, int? saves)
        {
            EnsureNonNegative(reach);
            EnsureNonNegative(impressions);
            EnsureNonNegative(saves);

            Reach = reach;
            Impressions = impressions;
            Saves = saves;
            EngagementRate = ComputeEngagementRate();
            MetricsCollectedAt = DateTimeOffset.UtcNow;
            MetricsSource = MetricsSource == DeliverableMetricsSource.None || MetricsSource == DeliverableMetricsSource.Manual
                ? DeliverableMetricsSource.Manual
                : DeliverableMetricsSource.Mixed;
        }

        private decimal? ComputeEngagementRate()
        {
            long? denominator = Reach ?? Impressions;
            if (!denominator.HasValue || denominator.Value <= 0)
            {
                return null;
            }

            long interactions = (Likes ?? 0) + (Comments ?? 0) + (Shares ?? 0) + (Saves ?? 0);
            return Math.Round((decimal)interactions / denominator.Value * 100m, 2);
        }

        private static void EnsureNonNegative(int? value)
        {
            if (value.HasValue)
            {
                ArgumentOutOfRangeException.ThrowIfNegative(value.Value);
            }
        }

        private static void EnsureNonNegative(long? value)
        {
            if (value.HasValue)
            {
                ArgumentOutOfRangeException.ThrowIfNegative(value.Value);
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
                throw new InvalidOperationException("campaignDeliverable.amountsExceedGross");
            }
        }

        // Prazo de entrega nao pode nascer (ou ser movido) para um dia anterior a hoje, evitando que o
        // SLA marque como "Atrasado" um entregavel recem-criado. Comparacao por dia (UTC), tolera hoje.
        private static void EnsureDueAtNotInPast(DateTimeOffset dueAt)
        {
            if (dueAt.ToUniversalTime().Date < DateTimeOffset.UtcNow.Date)
            {
                throw new InvalidOperationException("campaignDeliverable.dueAtInPast");
            }
        }
    }
}
