using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using Archon.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class CampaignDeliverableService : CrudService<CampaignDeliverable>, ICampaignDeliverableService
    {
        public CampaignDeliverableService(DbContext dbContext) : base(dbContext)
        {
        }

        public async Task<PagedResult<CampaignDeliverable>> GetDeliverables(PagedRequest request, CancellationToken cancellationToken = default)
        {
            return await QueryWithDetails()
                .OrderBy(item => item.DueAt)
                .ToPagedResultAsync(request, cancellationToken);
        }

        public async Task<CampaignDeliverable?> GetDeliverableById(long id, CancellationToken cancellationToken = default)
        {
            return await QueryWithDetails()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        }

        public async Task<List<CampaignDeliverable>> GetByCampaign(long campaignId, CancellationToken cancellationToken = default)
        {
            return await QueryWithDetails()
                .Where(item => item.CampaignId == campaignId)
                .OrderBy(item => item.DueAt)
                .ToListAsync(cancellationToken);
        }

        private IQueryable<CampaignDeliverable> QueryWithDetails()
        {
            return DbContext.Set<CampaignDeliverable>()
                .AsNoTracking()
                .Include(item => item.Creator)
                .Include(item => item.Campaign);
        }
    }
}
