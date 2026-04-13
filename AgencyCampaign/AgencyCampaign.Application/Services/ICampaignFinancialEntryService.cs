using AgencyCampaign.Application.Requests.CampaignFinancialEntries;
using AgencyCampaign.Domain.Entities;
using Archon.Application.Services;
using Archon.Core.Pagination;

namespace AgencyCampaign.Application.Services
{
    public interface ICampaignFinancialEntryService : ICrudService<CampaignFinancialEntry>
    {
        Task<PagedResult<CampaignFinancialEntry>> GetEntries(PagedRequest request, CancellationToken cancellationToken = default);

        Task<CampaignFinancialEntry?> GetEntryById(long id, CancellationToken cancellationToken = default);

        Task<List<CampaignFinancialEntry>> GetByCampaign(long campaignId, CancellationToken cancellationToken = default);

        Task<CampaignFinancialEntry> CreateEntry(CreateCampaignFinancialEntryRequest request, CancellationToken cancellationToken = default);

        Task<CampaignFinancialEntry> UpdateEntry(long id, UpdateCampaignFinancialEntryRequest request, CancellationToken cancellationToken = default);
    }
}
