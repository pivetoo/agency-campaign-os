using AgencyCampaign.Application.Requests.CreatorPortal;
using AgencyCampaign.Domain.Entities;

namespace AgencyCampaign.Application.Services
{
    public interface ICreatorPortalService
    {
        Task<CreatorPortalContext> ResolveContext(string token, CancellationToken cancellationToken = default);
        Task<List<CampaignCreator>> GetCampaigns(long creatorId, CancellationToken cancellationToken = default);
        Task<List<CampaignDocument>> GetDocuments(long creatorId, CancellationToken cancellationToken = default);
        Task<List<CreatorPayment>> GetPayments(long creatorId, CancellationToken cancellationToken = default);
        Task<Creator> UpdateBankInfo(long creatorId, UpdateCreatorBankInfoRequest request, CancellationToken cancellationToken = default);
        Task<CreatorPayment> UploadInvoice(long creatorId, UploadInvoiceRequest request, CancellationToken cancellationToken = default);
    }

    public sealed record CreatorPortalContext(Creator Creator, CreatorAccessToken Token);
}
