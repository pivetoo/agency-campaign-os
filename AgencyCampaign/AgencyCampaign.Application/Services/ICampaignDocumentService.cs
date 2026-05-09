using AgencyCampaign.Application.Requests.CampaignDocuments;
using AgencyCampaign.Domain.Entities;
using Archon.Application.Services;
using Archon.Core.Pagination;

namespace AgencyCampaign.Application.Services
{
    public interface ICampaignDocumentService : ICrudService<CampaignDocument>
    {
        Task<PagedResult<CampaignDocument>> GetDocuments(PagedRequest request, CancellationToken cancellationToken = default);
        Task<CampaignDocument?> GetDocumentById(long id, CancellationToken cancellationToken = default);
        Task<List<CampaignDocument>> GetByCampaign(long campaignId, CancellationToken cancellationToken = default);
        Task<CampaignDocument?> GetByProviderDocumentId(string provider, string providerDocumentId, CancellationToken cancellationToken = default);
        Task<CampaignDocument> CreateDocument(CreateCampaignDocumentRequest request, CancellationToken cancellationToken = default);
        Task<CampaignDocument> GenerateFromTemplate(GenerateCampaignDocumentFromTemplateRequest request, CancellationToken cancellationToken = default);
        Task<CampaignDocument> UpdateDocument(long id, UpdateCampaignDocumentRequest request, CancellationToken cancellationToken = default);
        Task<CampaignDocument> SendDocumentEmail(long id, SendCampaignDocumentEmailRequest request, CancellationToken cancellationToken = default);
        Task<CampaignDocument> SendForSignature(long id, SendCampaignDocumentForSignatureRequest request, CancellationToken cancellationToken = default);
        Task<CampaignDocument> HandleProviderCallback(CampaignDocumentProviderCallbackRequest request, CancellationToken cancellationToken = default);
        Task<CampaignDocument> MarkAsSigned(long id, MarkCampaignDocumentSignedRequest request, CancellationToken cancellationToken = default);
    }
}
