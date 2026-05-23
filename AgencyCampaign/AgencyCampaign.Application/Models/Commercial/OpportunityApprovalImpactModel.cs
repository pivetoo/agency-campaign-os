namespace AgencyCampaign.Application.Models.Commercial
{
    public sealed class OpportunityApprovalImpactModel
    {
        public long Id { get; init; }

        public long OpportunityApprovalRequestId { get; init; }

        public string Label { get; init; } = string.Empty;

        public string Value { get; init; } = string.Empty;

        public bool IsGood { get; init; }

        public int DisplayOrder { get; init; }
    }
}
