namespace AgencyCampaign.Application.Models.Dashboard
{
    public sealed class DashboardChartsModel
    {
        public IReadOnlyCollection<MonthlyRevenueItem> MonthlyRevenue { get; init; } = [];

        public IReadOnlyCollection<PipelineStageItem> Pipeline { get; init; } = [];

        public IReadOnlyCollection<PlatformDistributionItem> PlatformDistribution { get; init; } = [];

        public IReadOnlyCollection<CreatorGrowthItem> CreatorGrowth { get; init; } = [];
    }
}
