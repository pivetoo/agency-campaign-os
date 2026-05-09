namespace AgencyCampaign.Application.Requests.Opportunities
{
    public sealed class OpportunityListFilters
    {
        public string? Search { get; set; }

        public long? BrandId { get; set; }

        public long? CommercialPipelineStageId { get; set; }

        public long? ResponsibleUserId { get; set; }

        public string? Status { get; set; }

        public decimal? MinValue { get; set; }

        public decimal? MaxValue { get; set; }

        public long? OpportunitySourceId { get; set; }

        public long? OpportunityTagId { get; set; }
    }
}
