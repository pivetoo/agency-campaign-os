using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Models.Campaigns;
using AgencyCampaign.Application.Requests.Campaigns;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using Archon.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class CampaignService : CrudService<Campaign>, ICampaignService
    {
        private readonly IStringLocalizer<AgencyCampaignResource> localizer;

        public CampaignService(DbContext dbContext, IStringLocalizer<AgencyCampaignResource> localizer) : base(dbContext)
        {
            this.localizer = localizer;
        }

        public async Task<PagedResult<Campaign>> GetCampaigns(PagedRequest request, CancellationToken cancellationToken = default)
        {
            return await QueryWithDetails()
                .OrderByDescending(item => item.IsActive)
                .ThenByDescending(item => item.Id)
                .ToPagedResultAsync(request, cancellationToken);
        }

        public async Task<Campaign?> GetCampaignById(long id, CancellationToken cancellationToken = default)
        {
            return await QueryWithDetails()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        }

        public async Task<Campaign> CreateCampaign(CreateCampaignRequest request, CancellationToken cancellationToken = default)
        {
            await EnsureBrandExists(request.BrandId, cancellationToken);

            string? internalOwnerName = await ResolveCommercialResponsibleName(request.CommercialResponsibleId, cancellationToken);

            Campaign campaign = new(
                request.BrandId,
                request.Name,
                request.Budget,
                request.StartsAt,
                request.Description,
                request.Objective,
                request.Briefing,
                request.EndsAt,
                internalOwnerName,
                request.Notes);

            campaign.ChangeStatus(request.Status);

            bool success = await Insert(cancellationToken, campaign);
            if (!success)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return await GetCampaignById(campaign.Id, cancellationToken) ?? campaign;
        }

        public async Task<Campaign> UpdateCampaign(long id, UpdateCampaignRequest request, CancellationToken cancellationToken = default)
        {
            if (id != request.Id)
            {
                throw new InvalidOperationException(localizer["request.route.idMismatch"]);
            }

            Campaign? campaign = await DbContext.Set<Campaign>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (campaign is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            await EnsureBrandExists(request.BrandId, cancellationToken);

            string? internalOwnerName = await ResolveCommercialResponsibleName(request.CommercialResponsibleId, cancellationToken);

            campaign.Update(
                request.BrandId,
                request.Name,
                request.Budget,
                request.StartsAt,
                request.EndsAt,
                request.Description,
                request.Objective,
                request.Briefing,
                request.Status,
                internalOwnerName,
                request.Notes,
                request.IsActive);

            Campaign? result = await Update(campaign, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return await GetCampaignById(result.Id, cancellationToken) ?? result;
        }

        public async Task<CampaignSummaryModel?> GetSummary(long id, CancellationToken cancellationToken = default)
        {
            Campaign? campaign = await DbContext.Set<Campaign>()
                .AsNoTracking()
                .Include(item => item.Brand)
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (campaign is null)
            {
                return null;
            }

            List<CampaignCreator> campaignCreators = await DbContext.Set<CampaignCreator>()
                .AsNoTracking()
                .Where(item => item.CampaignId == id)
                .ToListAsync(cancellationToken);

            List<CampaignDeliverable> deliverables = await DbContext.Set<CampaignDeliverable>()
                .AsNoTracking()
                .Where(item => item.CampaignId == id)
                .ToListAsync(cancellationToken);

            List<DeliverableApproval> approvals = await DbContext.Set<DeliverableApproval>()
                .AsNoTracking()
                .Where(item => deliverables.Select(deliverable => deliverable.Id).Contains(item.CampaignDeliverableId))
                .ToListAsync(cancellationToken);

            decimal grossAmountTotal = deliverables.Sum(item => item.GrossAmount);
            decimal creatorAmountTotal = deliverables.Sum(item => item.CreatorAmount);
            decimal agencyFeeAmountTotal = deliverables.Sum(item => item.AgencyFeeAmount);

            return new CampaignSummaryModel
            {
                CampaignId = campaign.Id,
                CampaignName = campaign.Name,
                BrandId = campaign.BrandId,
                BrandName = campaign.Brand?.Name ?? string.Empty,
                Status = campaign.Status,
                Budget = campaign.Budget,
                CampaignCreatorsCount = campaignCreators.Count,
                ConfirmedCampaignCreatorsCount = campaignCreators.Count(item => item.CampaignCreatorStatus is not null && item.CampaignCreatorStatus.Category == CampaignCreatorStatusCategory.Success),
                DeliverablesCount = deliverables.Count,
                PendingDeliverablesCount = deliverables.Count(item => item.Status == DeliverableStatus.Pending || item.Status == DeliverableStatus.InReview),
                PublishedDeliverablesCount = deliverables.Count(item => item.Status == DeliverableStatus.Published),
                PendingApprovalsCount = approvals.Count(item => item.Status == DeliverableApprovalStatus.Pending),
                GrossAmountTotal = grossAmountTotal,
                CreatorAmountTotal = creatorAmountTotal,
                AgencyFeeAmountTotal = agencyFeeAmountTotal,
                RemainingBudget = campaign.Budget - grossAmountTotal
            };
        }

        private async Task<string?> ResolveCommercialResponsibleName(long? commercialResponsibleId, CancellationToken cancellationToken)
        {
            if (!commercialResponsibleId.HasValue)
            {
                return null;
            }

            CommercialResponsible? responsible = await DbContext.Set<CommercialResponsible>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == commercialResponsibleId.Value, cancellationToken);

            return responsible?.Name;
        }

        private async Task EnsureBrandExists(long brandId, CancellationToken cancellationToken)
        {
            bool exists = await DbContext.Set<Brand>()
                .AsNoTracking()
                .AnyAsync(item => item.Id == brandId, cancellationToken);

            if (!exists)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }
        }

        private IQueryable<Campaign> QueryWithDetails()
        {
            return DbContext.Set<Campaign>()
                .AsNoTracking()
                .Include(item => item.Brand);
        }
    }
}
