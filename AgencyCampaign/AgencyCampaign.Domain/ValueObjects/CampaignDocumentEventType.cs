namespace AgencyCampaign.Domain.ValueObjects
{
    public enum CampaignDocumentEventType
    {
        Created = 1,
        ReadyToSend = 2,
        Sent = 3,
        Viewed = 4,
        SignerSigned = 5,
        Signed = 6,
        Rejected = 7,
        Cancelled = 8,
        ProviderSyncError = 9
    }
}
