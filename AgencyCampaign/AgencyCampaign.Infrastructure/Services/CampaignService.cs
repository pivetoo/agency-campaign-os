using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Models.Campaigns;
using AgencyCampaign.Application.Requests.Campaigns;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using Archon.Application.Abstractions;
using Archon.Core.Pagination;
using Archon.Infrastructure.IdentityManagement;
using Archon.Infrastructure.Persistence.EF;
using Archon.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class CampaignService : CrudService<Campaign>, ICampaignService
    {
        private readonly IStringLocalizer<AgencyCampaignResource> localizer;
        private readonly ICurrentUser currentUser;
        private readonly IdentityUsersClient identityUsersClient;

        public CampaignService(DbContext dbContext, IStringLocalizer<AgencyCampaignResource> localizer, ICurrentUser currentUser, IdentityUsersClient identityUsersClient) : base(dbContext)
        {
            this.localizer = localizer;
            this.currentUser = currentUser;
            this.identityUsersClient = identityUsersClient;
        }

        public async Task<PagedResult<Campaign>> GetCampaigns(PagedRequest request, string? search, bool includeInactive, CancellationToken cancellationToken = default)
        {
            var query = QueryWithDetails();
            if (!includeInactive)
            {
                query = query.Where(item => item.IsActive);
            }
            if (!string.IsNullOrWhiteSpace(search))
            {
                var lower = search.ToLower();
                query = query.Where(item => item.Name.ToLower().Contains(lower));
            }
            return await query
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

            string? internalOwnerName = await ResolveCommercialResponsibleName(request.ResponsibleUserId, cancellationToken);

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

            await RegisterStatusHistory(campaign.Id, null, campaign.Status, cancellationToken);

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

            string? internalOwnerName = await ResolveCommercialResponsibleName(request.ResponsibleUserId, cancellationToken);

            CampaignStatus previousStatus = campaign.Status;

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

            if (previousStatus != campaign.Status)
            {
                await RegisterStatusHistory(campaign.Id, previousStatus, campaign.Status, cancellationToken);
            }

            return await GetCampaignById(result.Id, cancellationToken) ?? result;
        }

        public async Task<IReadOnlyCollection<CampaignStatusHistory>> GetStatusHistory(long campaignId, CancellationToken cancellationToken = default)
        {
            return await DbContext.Set<CampaignStatusHistory>()
                .AsNoTracking()
                .Where(item => item.CampaignId == campaignId)
                .OrderByDescending(item => item.ChangedAt)
                .ToArrayAsync(cancellationToken);
        }

        private async Task RegisterStatusHistory(long campaignId, CampaignStatus? fromStatus, CampaignStatus toStatus, CancellationToken cancellationToken)
        {
            CampaignStatusHistory history = new(campaignId, fromStatus, toStatus, currentUser.UserId, currentUser.UserName);
            DbContext.Set<CampaignStatusHistory>().Add(history);
            await DbContext.SaveChangesAsync(cancellationToken);
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

        private async Task<string?> ResolveCommercialResponsibleName(long? responsibleUserId, CancellationToken cancellationToken)
        {
            if (!responsibleUserId.HasValue)
            {
                return null;
            }

            try
            {
                IdentityUserDto? user = await identityUsersClient.GetUserByIdAsync(responsibleUserId.Value, cancellationToken);
                return user?.Name;
            }
            catch
            {
                return null;
            }
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
