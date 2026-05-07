namespace AgencyCampaign.Domain.ValueObjects
{
    public enum EmailEventType
    {
        ProposalSent = 1,
        ProposalApproved = 2,
        ProposalRejected = 3,
        ProposalConverted = 4,
        FollowUpDueSoon = 5,
        FollowUpOverdue = 6,
        OpportunityApprovalRequested = 7
    }
}
