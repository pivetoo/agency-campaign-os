using AgencyCampaign.Application.Models.Commercial;
using AgencyCampaign.Application.Requests.Commercial;
using Archon.Core.Pagination;

namespace AgencyCampaign.Application.Services
{
    public interface ICommercialGoalService
    {
        Task<PagedResult<CommercialGoalModel>> GetAll(PagedRequest request, bool includeInactive, long? userId, int? periodType, CancellationToken cancellationToken = default);

        Task<CommercialGoalModel> Create(CreateCommercialGoalRequest request, CancellationToken cancellationToken = default);

        Task<CommercialGoalModel> Update(long id, UpdateCommercialGoalRequest request, CancellationToken cancellationToken = default);

        Task Delete(long id, CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<CommercialGoalProgressModel>> GetProgress(DateTimeOffset referenceDate, long? userId, int? periodType, CancellationToken cancellationToken = default);
    }
}
