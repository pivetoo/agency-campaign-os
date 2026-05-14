using AgencyCampaign.Application.Requests.CommercialPipelineStages;
using AgencyCampaign.Domain.Entities;
using Archon.Application.Services;
using Archon.Core.Pagination;

namespace AgencyCampaign.Application.Services
{
    public interface ICommercialPipelineStageService : ICrudService<CommercialPipelineStage>
    {
        Task<PagedResult<CommercialPipelineStage>> GetStages(PagedRequest request, string? search, bool includeInactive, CancellationToken cancellationToken = default);

        Task<List<CommercialPipelineStage>> GetActiveStages(CancellationToken cancellationToken = default);

        Task<CommercialPipelineStage?> GetStageById(long id, CancellationToken cancellationToken = default);

        Task<CommercialPipelineStage> CreateStage(CreateCommercialPipelineStageRequest request, CancellationToken cancellationToken = default);

        Task<CommercialPipelineStage> UpdateStage(long id, UpdateCommercialPipelineStageRequest request, CancellationToken cancellationToken = default);
    }
}
