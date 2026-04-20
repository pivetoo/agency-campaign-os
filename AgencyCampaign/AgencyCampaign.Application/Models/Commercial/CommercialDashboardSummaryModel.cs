namespace AgencyCampaign.Application.Models.Commercial
{
    public sealed class CommercialDashboardSummaryModel
    {
        public int TotalOpportunities { get; init; }

        public int OpenOpportunities { get; init; }

        public int WonOpportunities { get; init; }

        public int LostOpportunities { get; init; }

        public int NegotiationsCount { get; init; }

        public int PendingFollowUpsCount { get; init; }

        public int OverdueFollowUpsCount { get; init; }

        public decimal TotalPipelineValue { get; init; }

        public decimal WonValue { get; init; }
    }
}
