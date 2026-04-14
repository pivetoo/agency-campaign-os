using AgencyCampaign.Application.Requests.Platforms;
using AgencyCampaign.Domain.Entities;
using Archon.Application.Services;
using Archon.Core.Pagination;

namespace AgencyCampaign.Application.Services
{
    public interface IPlatformService : ICrudService<Platform>
    {
        Task<PagedResult<Platform>> GetPlatforms(PagedRequest request, CancellationToken cancellationToken = default);

        Task<Platform?> GetPlatformById(long id, CancellationToken cancellationToken = default);

        Task<List<Platform>> GetActivePlatforms(CancellationToken cancellationToken = default);

        Task<Platform> CreatePlatform(CreatePlatformRequest request, CancellationToken cancellationToken = default);

        Task<Platform> UpdatePlatform(long id, UpdatePlatformRequest request, CancellationToken cancellationToken = default);
    }
}
