namespace AgencyCampaign.Application.Services
{
    public interface ICreatorAudienceSyncService
    {
        Task<int> SyncCreator(long creatorId, CancellationToken cancellationToken = default);
    }
}
