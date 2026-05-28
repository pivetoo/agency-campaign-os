using AgencyCampaign.Application.Requests.CampaignBriefings;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class CampaignBriefingService : ICampaignBriefingService
    {
        private readonly DbContext dbContext;

        public CampaignBriefingService(DbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<CampaignBriefingModel?> GetByCampaign(long campaignId, CancellationToken cancellationToken = default)
        {
            CampaignBriefing? briefing = await dbContext.Set<CampaignBriefing>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.CampaignId == campaignId, cancellationToken);

            return briefing is null ? null : CampaignBriefingModel.FromEntity(briefing);
        }

        public async Task<CampaignBriefingModel> Upsert(long campaignId, UpsertCampaignBriefingRequest request, CancellationToken cancellationToken = default)
        {
            bool campaignExists = await dbContext.Set<Campaign>()
                .AsNoTracking()
                .AnyAsync(item => item.Id == campaignId, cancellationToken);

            if (!campaignExists)
            {
                throw new InvalidOperationException("record.notFound");
            }

            CampaignBriefing? briefing = await dbContext.Set<CampaignBriefing>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.CampaignId == campaignId, cancellationToken);

            if (briefing is null)
            {
                briefing = new CampaignBriefing(campaignId, request.KeyMessage, request.Dos, request.Donts, request.Hashtags, request.Mentions, request.ReferenceLinks);
                dbContext.Set<CampaignBriefing>().Add(briefing);
            }
            else
            {
                briefing.Update(request.KeyMessage, request.Dos, request.Donts, request.Hashtags, request.Mentions, request.ReferenceLinks);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            return CampaignBriefingModel.FromEntity(briefing);
        }
    }
}
