using AgencyCampaign.Application.Models.Creators;
using AgencyCampaign.Application.Requests.Creators;
using AgencyCampaign.Domain.Entities;
using Archon.Application.Services;
using Archon.Core.Pagination;

namespace AgencyCampaign.Application.Services
{
    public interface ICreatorService : ICrudService<Creator>
    {
        Task<PagedResult<Creator>> GetCreators(PagedRequest request, string? search, bool includeInactive, CancellationToken cancellationToken = default);

        IAsyncEnumerable<string> ExportAsync(CancellationToken cancellationToken = default);

        Task<Creator?> GetCreatorById(long id, CancellationToken cancellationToken = default);

        Task<Creator> CreateCreator(CreateCreatorRequest request, CancellationToken cancellationToken = default);

        Task<Creator> UpdateCreator(long id, UpdateCreatorRequest request, CancellationToken cancellationToken = default);

        Task<CreatorSummaryModel?> GetSummary(long id, CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<CampaignCreator>> GetCampaignsByCreator(long creatorId, CancellationToken cancellationToken = default);

        Task<Creator> SetCreatorPhoto(long id, string photoUrl, CancellationToken cancellationToken = default);

        Task<Creator> RemoveCreatorPhoto(long id, CancellationToken cancellationToken = default);
    }
}
