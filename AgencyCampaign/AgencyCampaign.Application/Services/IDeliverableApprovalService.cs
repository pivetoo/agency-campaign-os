using AgencyCampaign.Application.Requests.DeliverableApprovals;
using AgencyCampaign.Domain.Entities;
using Archon.Application.Services;
using Archon.Core.Pagination;

namespace AgencyCampaign.Application.Services
{
    public interface IDeliverableApprovalService : ICrudService<DeliverableApproval>
    {
        Task<PagedResult<DeliverableApproval>> GetApprovals(PagedRequest request, CancellationToken cancellationToken = default);

        Task<DeliverableApproval?> GetApprovalById(long id, CancellationToken cancellationToken = default);

        Task<List<DeliverableApproval>> GetByDeliverable(long campaignDeliverableId, CancellationToken cancellationToken = default);

        Task<DeliverableApproval> CreateApproval(CreateDeliverableApprovalRequest request, CancellationToken cancellationToken = default);

        Task<DeliverableApproval> UpdateApproval(long id, UpdateDeliverableApprovalRequest request, CancellationToken cancellationToken = default);
    }
}
