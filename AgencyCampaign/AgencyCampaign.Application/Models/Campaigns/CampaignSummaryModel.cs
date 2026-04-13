namespace AgencyCampaign.Application.Models.Campaigns
{
    public sealed class CampaignSummaryModel
    {
        public long CampaignId { get; init; }

        public string CampaignName { get; init; } = string.Empty;

        public long BrandId { get; init; }

        public string BrandName { get; init; } = string.Empty;

        public decimal Budget { get; init; }

        public int DeliverablesCount { get; init; }

        public int PendingDeliverablesCount { get; init; }

        public int PublishedDeliverablesCount { get; init; }

        public decimal GrossAmountTotal { get; init; }

        public decimal CreatorAmountTotal { get; init; }

        public decimal AgencyFeeAmountTotal { get; init; }

        public decimal RemainingBudget { get; init; }
    }
}
