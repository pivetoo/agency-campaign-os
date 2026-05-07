using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Notifications;
using AgencyCampaign.Application.Requests.CampaignCreators;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Archon.Application.Abstractions;
using Archon.Application.Services;
using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using Archon.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class CampaignCreatorService : CrudService<CampaignCreator>, ICampaignCreatorService
    {
        private readonly IStringLocalizer<AgencyCampaignResource> localizer;
        private readonly ICurrentUser currentUser;
        private readonly INotificationService notificationService;

        public CampaignCreatorService(DbContext dbContext, IStringLocalizer<AgencyCampaignResource> localizer, ICurrentUser currentUser, INotificationService notificationService) : base(dbContext)
        {
            this.localizer = localizer;
            this.currentUser = currentUser;
            this.notificationService = notificationService;
        }

        public async Task<PagedResult<CampaignCreator>> GetCampaignCreators(PagedRequest request, CancellationToken cancellationToken = default)
        {
            return await QueryWithDetails()
                .OrderByDescending(item => item.Id)
                .ToPagedResultAsync(request, cancellationToken);
        }

        public async Task<CampaignCreator?> GetCampaignCreatorById(long id, CancellationToken cancellationToken = default)
        {
            return await QueryWithDetails()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        }

        public async Task<List<CampaignCreator>> GetByCampaign(long campaignId, CancellationToken cancellationToken = default)
        {
            return await QueryWithDetails()
                .Where(item => item.CampaignId == campaignId)
                .OrderByDescending(item => item.Id)
                .ToListAsync(cancellationToken);
        }

        public async Task<CampaignCreator> CreateCampaignCreator(CreateCampaignCreatorRequest request, CancellationToken cancellationToken = default)
        {
            await EnsureReferencesExist(request.CampaignId, request.CreatorId, cancellationToken);
            await EnsureUniqueCreatorPerCampaign(request.CampaignId, request.CreatorId, cancellationToken);

            Creator creator = await DbContext.Set<Creator>()
                .AsNoTracking()
                .FirstAsync(item => item.Id == request.CreatorId, cancellationToken);

            long statusId = request.CampaignCreatorStatusId > 0
                ? request.CampaignCreatorStatusId
                : await ResolveInitialStatusId(cancellationToken);

            CampaignCreator campaignCreator = new(
                request.CampaignId,
                request.CreatorId,
                statusId,
                request.AgreedAmount,
                request.AgencyFeePercent > 0 ? request.AgencyFeePercent : creator.DefaultAgencyFeePercent,
                request.Notes);

            bool success = await Insert(cancellationToken, campaignCreator);
            if (!success)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            await RegisterStatusHistory(campaignCreator.Id, null, statusId, cancellationToken);

            return await GetCampaignCreatorById(campaignCreator.Id, cancellationToken) ?? campaignCreator;
        }

        public async Task<CampaignCreator> UpdateCampaignCreator(long id, UpdateCampaignCreatorRequest request, CancellationToken cancellationToken = default)
        {
            if (id != request.Id)
            {
                throw new InvalidOperationException(localizer["request.route.idMismatch"]);
            }

            CampaignCreator? campaignCreator = await DbContext.Set<CampaignCreator>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (campaignCreator is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            campaignCreator.Update(request.AgreedAmount, request.Notes);

            long previousStatusId = campaignCreator.CampaignCreatorStatusId;
            bool statusChanged = previousStatusId != request.CampaignCreatorStatusId;
            CampaignCreatorStatus? newStatus = null;

            if (statusChanged)
            {
                newStatus = await DbContext.Set<CampaignCreatorStatus>()
                    .AsTracking()
                    .FirstOrDefaultAsync(s => s.Id == request.CampaignCreatorStatusId, cancellationToken);

                if (newStatus is null)
                {
                    throw new InvalidOperationException("Status não encontrado.");
                }

                campaignCreator.ChangeStatus(newStatus);
            }

            CampaignCreator? result = await Update(campaignCreator, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            if (statusChanged)
            {
                await RegisterStatusHistory(campaignCreator.Id, previousStatusId, campaignCreator.CampaignCreatorStatusId, cancellationToken);
                await TryNotifyStatusChange(campaignCreator, newStatus!, cancellationToken);
            }

            return await GetCampaignCreatorById(result.Id, cancellationToken) ?? result;
        }

        private async Task TryNotifyStatusChange(CampaignCreator campaignCreator, CampaignCreatorStatus newStatus, CancellationToken cancellationToken)
        {
            if (!newStatus.MarksAsConfirmed && !newStatus.MarksAsCancelled)
            {
                return;
            }

            try
            {
                var info = await DbContext.Set<CampaignCreator>()
                    .AsNoTracking()
                    .Where(item => item.Id == campaignCreator.Id)
                    .Select(item => new
                    {
                        CreatorName = item.Creator!.StageName ?? item.Creator!.Name,
                        CampaignName = item.Campaign!.Name
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                if (info is null)
                {
                    return;
                }

                var notification = newStatus.MarksAsConfirmed
                    ? KanvasNotifications.CampaignCreatorConfirmed(campaignCreator, info.CreatorName, info.CampaignName)
                    : KanvasNotifications.CampaignCreatorCancelled(campaignCreator, info.CreatorName, info.CampaignName);

                await notificationService.Create(notification, cancellationToken);
            }
            catch (Exception exception)
            {
                Console.WriteLine($"[CampaignCreatorService] failed to create notification: {exception.Message}");
            }
        }

        public async Task<IReadOnlyCollection<CampaignCreatorStatusHistory>> GetStatusHistory(long campaignCreatorId, CancellationToken cancellationToken = default)
        {
            return await DbContext.Set<CampaignCreatorStatusHistory>()
                .AsNoTracking()
                .Include(item => item.FromStatus)
                .Include(item => item.ToStatus)
                .Where(item => item.CampaignCreatorId == campaignCreatorId)
                .OrderByDescending(item => item.ChangedAt)
                .ToArrayAsync(cancellationToken);
        }

        private async Task RegisterStatusHistory(long campaignCreatorId, long? fromStatusId, long toStatusId, CancellationToken cancellationToken)
        {
            CampaignCreatorStatusHistory history = new(campaignCreatorId, fromStatusId, toStatusId, currentUser.UserId, currentUser.UserName);
            DbContext.Set<CampaignCreatorStatusHistory>().Add(history);
            await DbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task<long> ResolveInitialStatusId(CancellationToken cancellationToken = default)
        {
            CampaignCreatorStatus? status = await DbContext.Set<CampaignCreatorStatus>()
                .AsNoTracking()
                .Where(s => s.IsActive && s.IsInitial)
                .OrderBy(s => s.DisplayOrder)
                .FirstOrDefaultAsync(cancellationToken);

            if (status is null)
            {
                throw new InvalidOperationException("Nenhum status inicial configurado.");
            }

            return status.Id;
        }

        private async Task EnsureReferencesExist(long campaignId, long creatorId, CancellationToken cancellationToken)
        {
            bool campaignExists = await DbContext.Set<Campaign>()
                .AsNoTracking()
                .AnyAsync(item => item.Id == campaignId, cancellationToken);

            if (!campaignExists)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            bool creatorExists = await DbContext.Set<Creator>()
                .AsNoTracking()
                .AnyAsync(item => item.Id == creatorId, cancellationToken);

            if (!creatorExists)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }
        }

        private async Task EnsureUniqueCreatorPerCampaign(long campaignId, long creatorId, CancellationToken cancellationToken)
        {
            bool exists = await DbContext.Set<CampaignCreator>()
                .AsNoTracking()
                .AnyAsync(item => item.CampaignId == campaignId && item.CreatorId == creatorId, cancellationToken);

            if (exists)
            {
                throw new InvalidOperationException(localizer["campaignCreator.duplicate"]);
            }
        }

        private IQueryable<CampaignCreator> QueryWithDetails()
        {
            return DbContext.Set<CampaignCreator>()
                .AsNoTracking()
                .Include(item => item.Campaign)
                .Include(item => item.Creator)
                .Include(item => item.CampaignCreatorStatus);
        }
    }
}
