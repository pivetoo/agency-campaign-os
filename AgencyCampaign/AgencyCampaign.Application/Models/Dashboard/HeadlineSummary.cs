namespace AgencyCampaign.Application.Models.Dashboard
{
    public sealed class HeadlineSummary
    {
        public int ActiveCampaigns { get; init; }

        public int ActiveBrands { get; init; }

        public int ActiveCreators { get; init; }

        public int PendingDeliverables { get; init; }

        public decimal MonthRevenue { get; init; }
    }
}
