using AgencyCampaign.Application.Models.Deliverables;
using AgencyCampaign.Application.Requests.DeliverableShareLinks;

namespace AgencyCampaign.Application.Services
{
    public interface IDeliverableShareLinkService
    {
        Task<IReadOnlyCollection<DeliverableShareLinkModel>> GetByDeliverable(long deliverableId, CancellationToken cancellationToken = default);

        Task<DeliverableShareLinkModel> Create(CreateDeliverableShareLinkRequest request, CancellationToken cancellationToken = default);

        Task Revoke(long id, CancellationToken cancellationToken = default);
    }

    public interface IDeliverablePublicService
    {
        Task<DeliverablePublicViewModel?> GetByToken(string token, CancellationToken cancellationToken = default);

        Task<DeliverablePublicViewModel> Approve(string token, PublicDeliverableDecisionRequest request, CancellationToken cancellationToken = default);

        Task<DeliverablePublicViewModel> Reject(string token, PublicDeliverableDecisionRequest request, CancellationToken cancellationToken = default);
    }

    public interface IDeliverableApprovalsService
    {
        Task<IReadOnlyCollection<PendingApprovalModel>> GetPending(CancellationToken cancellationToken = default);
    }
}
