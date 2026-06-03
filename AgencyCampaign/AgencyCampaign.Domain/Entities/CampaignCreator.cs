using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class CampaignCreator : Entity
    {
        private readonly List<CampaignDeliverable> deliverables = [];

        public long CampaignId { get; private set; }

        public Campaign? Campaign { get; private set; }

        public long CreatorId { get; private set; }

        public Creator? Creator { get; private set; }

        public long CampaignCreatorStatusId { get; private set; }

        public CampaignCreatorStatus? CampaignCreatorStatus { get; private set; }

        public decimal AgreedAmount { get; private set; }

        public decimal AgencyFeePercent { get; private set; }

        public decimal AgencyFeeAmount { get; private set; }

        public string? Notes { get; private set; }

        public DateTimeOffset? ConfirmedAt { get; private set; }

        public DateTimeOffset? CancelledAt { get; private set; }

        public string? CouponCode { get; private set; }

        public string? TrackingUrl { get; private set; }

        public int? AttributedOrders { get; private set; }

        public decimal? AttributedRevenue { get; private set; }

        public IReadOnlyCollection<CampaignDeliverable> Deliverables => deliverables.AsReadOnly();

        private CampaignCreator()
        {
        }

        public CampaignCreator(long campaignId, long creatorId, long campaignCreatorStatusId, decimal agreedAmount, decimal agencyFeePercent, string? notes = null)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(campaignId);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(creatorId);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(campaignCreatorStatusId);
            ArgumentOutOfRangeException.ThrowIfNegative(agreedAmount);
            ArgumentOutOfRangeException.ThrowIfNegative(agencyFeePercent);

            CampaignId = campaignId;
            CreatorId = creatorId;
            CampaignCreatorStatusId = campaignCreatorStatusId;
            AgreedAmount = agreedAmount;
            AgencyFeePercent = agencyFeePercent;
            AgencyFeeAmount = CalculateAgencyFeeAmount(agreedAmount, agencyFeePercent);
            Notes = Normalize(notes);
        }

        public void Update(decimal agreedAmount, decimal agencyFeePercent, string? notes)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(agreedAmount);
            ArgumentOutOfRangeException.ThrowIfNegative(agencyFeePercent);

            AgreedAmount = agreedAmount;
            AgencyFeePercent = agencyFeePercent;
            AgencyFeeAmount = CalculateAgencyFeeAmount(agreedAmount, agencyFeePercent);
            Notes = Normalize(notes);
        }

        public void ChangeStatus(CampaignCreatorStatus status, DateTimeOffset? occurredAt = null)
        {
            ArgumentNullException.ThrowIfNull(status);

            CampaignCreatorStatusId = status.Id;
            DateTimeOffset timestamp = occurredAt?.ToUniversalTime() ?? DateTimeOffset.UtcNow;

            if (status.MarksAsConfirmed)
            {
                ConfirmedAt ??= timestamp;
                CancelledAt = null;
                return;
            }

            if (status.MarksAsCancelled)
            {
                CancelledAt = timestamp;
                return;
            }

            CancelledAt = null;
        }

        public void RegisterSalesAttribution(string? couponCode, string? trackingUrl, int? attributedOrders, decimal? attributedRevenue)
        {
            if (attributedOrders.HasValue && attributedOrders.Value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(attributedOrders));
            }

            if (attributedRevenue.HasValue && attributedRevenue.Value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(attributedRevenue));
            }

            CouponCode = Normalize(couponCode);
            TrackingUrl = Normalize(trackingUrl);
            AttributedOrders = attributedOrders;
            AttributedRevenue = attributedRevenue;
        }

        // Zera o fee da agencia para exposicao no portal do creator (o creator ve apenas o
        // proprio cache em AgreedAmount). Uso somente em entidades destacadas/somente-leitura;
        // nao deve ser chamado em instancia rastreada que sera persistida.
        public void RedactAgencyFee()
        {
            AgencyFeePercent = 0m;
            AgencyFeeAmount = 0m;
        }

        private static decimal CalculateAgencyFeeAmount(decimal agreedAmount, decimal agencyFeePercent)
        {
            return Math.Round(agreedAmount * agencyFeePercent / 100m, 2, MidpointRounding.AwayFromZero);
        }

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
