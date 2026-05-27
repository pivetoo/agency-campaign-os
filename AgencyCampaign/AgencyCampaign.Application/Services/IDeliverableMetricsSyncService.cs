namespace AgencyCampaign.Application.Services
{
    public interface IDeliverableMetricsSyncService
    {
        Task<int> SyncCampaign(long campaignId, TimeSpan cooldown, CancellationToken cancellationToken = default);

        Task<int> SyncAll(TimeSpan cooldown, CancellationToken cancellationToken = default);
    }
}
