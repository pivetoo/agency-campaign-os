namespace AgencyCampaign.Application.Services
{
    public interface ICreatorAudienceSyncService
    {
        Task<int> SyncCreator(long creatorId, TimeSpan cooldown, CancellationToken cancellationToken = default);

        Task<int> SyncAll(TimeSpan cooldown, CancellationToken cancellationToken = default);
    }
}
