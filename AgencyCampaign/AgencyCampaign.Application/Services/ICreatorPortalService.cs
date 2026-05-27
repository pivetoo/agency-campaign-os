using AgencyCampaign.Application.Requests.ContentReview;
using AgencyCampaign.Application.Requests.CreatorPortal;
using AgencyCampaign.Domain.Entities;

namespace AgencyCampaign.Application.Services
{
    public interface ICreatorPortalService
    {
        Task<CreatorPortalContext> ResolveContext(string token, CancellationToken cancellationToken = default);
        Task<List<CampaignCreator>> GetCampaigns(long creatorId, CancellationToken cancellationToken = default);
        Task<List<CampaignDocument>> GetDocuments(long creatorId, CancellationToken cancellationToken = default);
        Task<List<CampaignDeliverable>> GetDeliverables(long creatorId, CancellationToken cancellationToken = default);
        Task<CampaignDeliverable> SubmitInsights(long creatorId, long deliverableId, SubmitDeliverableInsightsRequest request, CancellationToken cancellationToken = default);
        Task<List<CreatorPayment>> GetPayments(long creatorId, CancellationToken cancellationToken = default);
        Task<Creator> UpdateBankInfo(long creatorId, UpdateCreatorBankInfoRequest request, CancellationToken cancellationToken = default);
        Task<CreatorPayment> UploadInvoice(long creatorId, UploadInvoiceRequest request, CancellationToken cancellationToken = default);
        Task<ContentReviewModel> GetDeliverableReview(long creatorId, long deliverableId, CancellationToken cancellationToken = default);
        Task<ContentReviewModel> SubmitContentVersion(long creatorId, long deliverableId, AddContentVersionRequest request, CancellationToken cancellationToken = default);
        Task<ContentReviewModel> AddReviewComment(long creatorId, long deliverableId, string body, CancellationToken cancellationToken = default);
    }

    public sealed record CreatorPortalContext(Creator Creator, CreatorAccessToken Token);
}
