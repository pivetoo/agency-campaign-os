namespace AgencyCampaign.Domain.ValueObjects
{
    public enum CreatorPaymentEventType
    {
        Created = 1,
        Updated = 2,
        Scheduled = 3,
        ProviderAccepted = 4,
        Paid = 5,
        Failed = 6,
        Cancelled = 7,
        InvoiceAttached = 8,
        ProviderSyncError = 9
    }
}
