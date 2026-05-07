namespace AgencyCampaign.Application.Requests.Proposals
{
    public sealed class ProposalListFilters
    {
        public string? Search { get; set; }

        public int? Status { get; set; }

        public long? OpportunityId { get; set; }

        public long? InternalOwnerId { get; set; }

        public DateTimeOffset? ValidityFrom { get; set; }

        public DateTimeOffset? ValidityTo { get; set; }
    }
}
