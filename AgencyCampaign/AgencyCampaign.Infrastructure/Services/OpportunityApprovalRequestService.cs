using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Models.Commercial;
using AgencyCampaign.Application.Notifications;
using AgencyCampaign.Application.Requests.Opportunities;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using Archon.Application.Services;
using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using Archon.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class OpportunityApprovalRequestService : CrudService<OpportunityApprovalRequest>, IOpportunityApprovalRequestService
    {
        private readonly IStringLocalizer<AgencyCampaignResource> localizer;
        private readonly INotificationService notificationService;

        public OpportunityApprovalRequestService(DbContext dbContext, IStringLocalizer<AgencyCampaignResource> localizer, INotificationService notificationService) : base(dbContext)
        {
            this.localizer = localizer;
            this.notificationService = notificationService;
        }

        public async Task<OpportunityApprovalRequest?> GetOpportunityApprovalRequestById(long id, CancellationToken cancellationToken = default)
        {
            return await QueryWithDetails()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        }

        public async Task<OpportunityApprovalRequest> CreateOpportunityApprovalRequest(CreateOpportunityApprovalRequest request, CancellationToken cancellationToken = default)
        {
            // Atualizar status da negociacao antes do Insert: Insert (via CrudService.ExecuteInTransaction)
            // limpa o ChangeTracker no finally, o que detacharia esta entidade e o SaveChanges seguinte
            // nao persistiria a transicao para PendingApproval.
            OpportunityNegotiation negotiation = await GetTrackedNegotiation(request.OpportunityNegotiationId, cancellationToken);
            negotiation.MarkPendingApproval();
            await DbContext.SaveChangesAsync(cancellationToken);

            OpportunityApprovalRequest approvalRequest = new(
                request.OpportunityNegotiationId,
                request.ApprovalType,
                request.Reason,
                request.RequestedByUserName,
                request.RequestedByUserId);

            bool success = await Insert(cancellationToken, approvalRequest);
            if (!success)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            (long? opportunityId, string opportunityName) = await ResolveOpportunityFromNegotiationAsync(negotiation.Id, cancellationToken);
            await TryNotify(KanvasNotifications.OpportunityApprovalRequested(approvalRequest, opportunityId, opportunityName), cancellationToken);

            return await GetOpportunityApprovalRequestById(approvalRequest.Id, cancellationToken) ?? approvalRequest;
        }

        public async Task<OpportunityApprovalRequest> Approve(long id, DecideOpportunityApprovalRequest request, CancellationToken cancellationToken = default)
        {
            OpportunityApprovalRequest approvalRequest = await GetTrackedApproval(id, cancellationToken);
            approvalRequest.Approve(request.ApprovedByUserName, request.DecisionNotes, request.ApprovedByUserId);

            OpportunityNegotiation negotiation = await GetTrackedNegotiation(approvalRequest.OpportunityNegotiationId, cancellationToken);
            negotiation.Approve();

            await DbContext.SaveChangesAsync(cancellationToken);

            (long? opportunityId, string opportunityName) = await ResolveOpportunityFromNegotiationAsync(negotiation.Id, cancellationToken);
            await TryNotify(KanvasNotifications.OpportunityApprovalDecided(approvalRequest, opportunityId, opportunityName, approved: true), cancellationToken);

            return await GetOpportunityApprovalRequestById(id, cancellationToken) ?? approvalRequest;
        }

        public async Task<OpportunityApprovalRequest> Reject(long id, DecideOpportunityApprovalRequest request, CancellationToken cancellationToken = default)
        {
            OpportunityApprovalRequest approvalRequest = await GetTrackedApproval(id, cancellationToken);
            approvalRequest.Reject(request.ApprovedByUserName, request.DecisionNotes, request.ApprovedByUserId);

            OpportunityNegotiation negotiation = await GetTrackedNegotiation(approvalRequest.OpportunityNegotiationId, cancellationToken);
            negotiation.Reject();

            await DbContext.SaveChangesAsync(cancellationToken);

            (long? opportunityId, string opportunityName) = await ResolveOpportunityFromNegotiationAsync(negotiation.Id, cancellationToken);
            await TryNotify(KanvasNotifications.OpportunityApprovalDecided(approvalRequest, opportunityId, opportunityName, approved: false), cancellationToken);

            return await GetOpportunityApprovalRequestById(id, cancellationToken) ?? approvalRequest;
        }

        private async Task<(long? opportunityId, string opportunityName)> ResolveOpportunityFromNegotiationAsync(long negotiationId, CancellationToken cancellationToken)
        {
            var info = await DbContext.Set<OpportunityNegotiation>()
                .AsNoTracking()
                .Where(item => item.Id == negotiationId)
                .Select(item => new { item.OpportunityId, OpportunityName = item.Opportunity!.Name })
                .FirstOrDefaultAsync(cancellationToken);

            return info is null ? (null, "oportunidade") : (info.OpportunityId, info.OpportunityName);
        }

        private async Task TryNotify(Archon.Core.Notifications.CreateNotificationRequest request, CancellationToken cancellationToken)
        {
            try
            {
                await notificationService.Create(request, cancellationToken);
            }
            catch (Exception exception)
            {
                Console.WriteLine($"[OpportunityApprovalRequestService] failed to create notification: {exception.Message}");
            }
        }

        public async Task<IReadOnlyCollection<OpportunityApprovalRequest>> GetApprovalsByNegotiationId(long opportunityNegotiationId, CancellationToken cancellationToken = default)
        {
            return await QueryWithDetails()
                .Where(item => item.OpportunityNegotiationId == opportunityNegotiationId)
                .OrderByDescending(item => item.RequestedAt)
                .ThenByDescending(item => item.Id)
                .ToListAsync(cancellationToken);
        }

        public async Task<PagedResult<OpportunityApprovalRequest>> GetAllApprovals(PagedRequest request, CancellationToken cancellationToken = default)
        {
            return await DbContext.Set<OpportunityApprovalRequest>()
                .AsNoTracking()
                .Include(item => item.OpportunityNegotiation)
                    .ThenInclude(n => n!.Opportunity)
                .OrderByDescending(item => item.RequestedAt)
                .ThenByDescending(item => item.Id)
                .ToPagedResultAsync(request, cancellationToken);
        }

        public async Task<ApprovalSummaryModel> GetApprovalsSummary(CancellationToken cancellationToken = default)
        {
            var counts = await DbContext.Set<OpportunityApprovalRequest>()
                .AsNoTracking()
                .GroupBy(item => item.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            return new ApprovalSummaryModel
            {
                Pending = counts.FirstOrDefault(c => c.Status == OpportunityApprovalStatus.Pending)?.Count ?? 0,
                Approved = counts.FirstOrDefault(c => c.Status == OpportunityApprovalStatus.Approved)?.Count ?? 0,
                Rejected = counts.FirstOrDefault(c => c.Status == OpportunityApprovalStatus.Rejected)?.Count ?? 0,
            };
        }

        private async Task<OpportunityApprovalRequest> GetTrackedApproval(long id, CancellationToken cancellationToken)
        {
            OpportunityApprovalRequest? approvalRequest = await DbContext.Set<OpportunityApprovalRequest>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (approvalRequest is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            if (approvalRequest.Status != OpportunityApprovalStatus.Pending)
            {
                throw new InvalidOperationException(localizer["opportunityApproval.notPending"]);
            }

            return approvalRequest;
        }

        private async Task<OpportunityNegotiation> GetTrackedNegotiation(long id, CancellationToken cancellationToken)
        {
            OpportunityNegotiation? negotiation = await DbContext.Set<OpportunityNegotiation>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (negotiation is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            return negotiation;
        }

        private IQueryable<OpportunityApprovalRequest> QueryWithDetails()
        {
            return DbContext.Set<OpportunityApprovalRequest>()
                .AsNoTracking()
                .Include(item => item.OpportunityNegotiation);
        }
    }
}
