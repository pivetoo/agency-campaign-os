namespace AgencyCampaign.Domain.ValueObjects
{
    public enum CampaignDocumentStatus
    {
        Draft = 1,
        ReadyToSend = 2,
        Sent = 3,
        Viewed = 4,
        Signed = 5,
        Rejected = 6,
        Cancelled = 7
    }
}
