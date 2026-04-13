using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.DeliverableApprovals;
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
    public sealed class DeliverableApprovalService : CrudService<DeliverableApproval>, IDeliverableApprovalService
    {
        private readonly IStringLocalizer<AgencyCampaignResource> localizer;

        public DeliverableApprovalService(DbContext dbContext, IStringLocalizer<AgencyCampaignResource> localizer) : base(dbContext)
        {
            this.localizer = localizer;
        }

        public async Task<PagedResult<DeliverableApproval>> GetApprovals(PagedRequest request, CancellationToken cancellationToken = default)
        {
            return await QueryWithDetails()
                .OrderByDescending(item => item.Id)
                .ToPagedResultAsync(request, cancellationToken);
        }

        public async Task<DeliverableApproval?> GetApprovalById(long id, CancellationToken cancellationToken = default)
        {
            return await QueryWithDetails()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        }

        public async Task<List<DeliverableApproval>> GetByDeliverable(long campaignDeliverableId, CancellationToken cancellationToken = default)
        {
            return await QueryWithDetails()
                .Where(item => item.CampaignDeliverableId == campaignDeliverableId)
                .OrderByDescending(item => item.Id)
                .ToListAsync(cancellationToken);
        }

        public async Task<DeliverableApproval> CreateApproval(CreateDeliverableApprovalRequest request, CancellationToken cancellationToken = default)
        {
            await EnsureDeliverableExists(request.CampaignDeliverableId, cancellationToken);
            await EnsureUniqueApprovalType(request.CampaignDeliverableId, request.ApprovalType, cancellationToken);

            DeliverableApproval approval = new(
                request.CampaignDeliverableId,
                request.ApprovalType,
                request.ReviewerName,
                request.Comment);

            bool success = await Insert(cancellationToken, approval);
            if (!success)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return await GetApprovalById(approval.Id, cancellationToken) ?? approval;
        }

        public async Task<DeliverableApproval> UpdateApproval(long id, UpdateDeliverableApprovalRequest request, CancellationToken cancellationToken = default)
        {
            if (id != request.Id)
            {
                throw new InvalidOperationException(localizer["request.route.idMismatch"]);
            }

            DeliverableApproval? approval = await DbContext.Set<DeliverableApproval>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (approval is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            approval.UpdateReviewer(request.ReviewerName);

            switch (request.Status)
            {
                case DeliverableApprovalStatus.Pending:
                    approval.Reset(request.Comment);
                    break;
                case DeliverableApprovalStatus.Approved:
                    approval.Approve(request.Comment);
                    break;
                case DeliverableApprovalStatus.Rejected:
                    approval.Reject(request.Comment);
                    break;
                default:
                    throw new InvalidOperationException(localizer["record.notFound"]);
            }

            DeliverableApproval? result = await Update(approval, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return await GetApprovalById(result.Id, cancellationToken) ?? result;
        }

        private async Task EnsureDeliverableExists(long campaignDeliverableId, CancellationToken cancellationToken)
        {
            bool exists = await DbContext.Set<CampaignDeliverable>()
                .AsNoTracking()
                .AnyAsync(item => item.Id == campaignDeliverableId, cancellationToken);

            if (!exists)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }
        }

        private async Task EnsureUniqueApprovalType(long campaignDeliverableId, DeliverableApprovalType approvalType, CancellationToken cancellationToken)
        {
            bool exists = await DbContext.Set<DeliverableApproval>()
                .AsNoTracking()
                .AnyAsync(item => item.CampaignDeliverableId == campaignDeliverableId && item.ApprovalType == approvalType, cancellationToken);

            if (exists)
            {
                throw new InvalidOperationException(localizer["deliverableApproval.duplicateType"]);
            }
        }

        private IQueryable<DeliverableApproval> QueryWithDetails()
        {
            return DbContext.Set<DeliverableApproval>()
                .AsNoTracking()
                .Include(item => item.CampaignDeliverable);
        }
    }
}
