namespace AgencyCampaign.Application.Services
{
    public interface IDeliverableMetricsSyncService
    {
        Task<int> SyncCampaign(long campaignId, CancellationToken cancellationToken = default);
    }
}
