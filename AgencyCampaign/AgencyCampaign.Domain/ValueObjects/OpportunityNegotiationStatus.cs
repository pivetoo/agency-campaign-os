namespace AgencyCampaign.Domain.ValueObjects
{
    public enum OpportunityNegotiationStatus
    {
        Draft = 1,
        PendingApproval = 2,
        Approved = 3,
        Rejected = 4,
        SentToClient = 5,
        AcceptedByClient = 6,
        Cancelled = 7
    }
}
