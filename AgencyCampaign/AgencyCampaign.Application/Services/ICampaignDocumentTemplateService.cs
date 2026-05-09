using AgencyCampaign.Application.Requests.CampaignDocumentTemplates;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using Archon.Application.Services;
using Archon.Core.Pagination;

namespace AgencyCampaign.Application.Services
{
    public interface ICampaignDocumentTemplateService : ICrudService<CampaignDocumentTemplate>
    {
        Task<PagedResult<CampaignDocumentTemplate>> GetTemplates(PagedRequest request, CancellationToken cancellationToken = default);
        Task<CampaignDocumentTemplate?> GetTemplateById(long id, CancellationToken cancellationToken = default);
        Task<List<CampaignDocumentTemplate>> GetActiveByDocumentType(CampaignDocumentType documentType, CancellationToken cancellationToken = default);
        Task<CampaignDocumentTemplate> CreateTemplate(CreateCampaignDocumentTemplateRequest request, CancellationToken cancellationToken = default);
        Task<CampaignDocumentTemplate> UpdateTemplate(long id, UpdateCampaignDocumentTemplateRequest request, CancellationToken cancellationToken = default);
        Task<bool> DeleteTemplate(long id, CancellationToken cancellationToken = default);
    }
}
