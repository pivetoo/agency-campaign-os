using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.Opportunities;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using Archon.Infrastructure.Persistence.EF;
using Archon.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class OpportunityApprovalRequestService : CrudService<OpportunityApprovalRequest>, IOpportunityApprovalRequestService
    {
        private readonly IStringLocalizer<AgencyCampaignResource> localizer;

        public OpportunityApprovalRequestService(DbContext dbContext, IStringLocalizer<AgencyCampaignResource> localizer) : base(dbContext)
        {
            this.localizer = localizer;
        }

        public async Task<OpportunityApprovalRequest?> GetOpportunityApprovalRequestById(long id, CancellationToken cancellationToken = default)
        {
            return await QueryWithDetails()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        }

        public async Task<OpportunityApprovalRequest> CreateOpportunityApprovalRequest(CreateOpportunityApprovalRequest request, CancellationToken cancellationToken = default)
        {
            OpportunityNegotiation negotiation = await GetTrackedNegotiation(request.OpportunityNegotiationId, cancellationToken);

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

            negotiation.MarkPendingApproval();
            await DbContext.SaveChangesAsync(cancellationToken);

            return await GetOpportunityApprovalRequestById(approvalRequest.Id, cancellationToken) ?? approvalRequest;
        }

        public async Task<OpportunityApprovalRequest> Approve(long id, DecideOpportunityApprovalRequest request, CancellationToken cancellationToken = default)
        {
            OpportunityApprovalRequest approvalRequest = await GetTrackedApproval(id, cancellationToken);
            approvalRequest.Approve(request.ApprovedByUserName, request.DecisionNotes, request.ApprovedByUserId);

            OpportunityNegotiation negotiation = await GetTrackedNegotiation(approvalRequest.OpportunityNegotiationId, cancellationToken);
            negotiation.Approve();

            await DbContext.SaveChangesAsync(cancellationToken);
            return await GetOpportunityApprovalRequestById(id, cancellationToken) ?? approvalRequest;
        }

        public async Task<OpportunityApprovalRequest> Reject(long id, DecideOpportunityApprovalRequest request, CancellationToken cancellationToken = default)
        {
            OpportunityApprovalRequest approvalRequest = await GetTrackedApproval(id, cancellationToken);
            approvalRequest.Reject(request.ApprovedByUserName, request.DecisionNotes, request.ApprovedByUserId);

            OpportunityNegotiation negotiation = await GetTrackedNegotiation(approvalRequest.OpportunityNegotiationId, cancellationToken);
            negotiation.Reject();

            await DbContext.SaveChangesAsync(cancellationToken);
            return await GetOpportunityApprovalRequestById(id, cancellationToken) ?? approvalRequest;
        }

        public async Task<IReadOnlyCollection<OpportunityApprovalRequest>> GetApprovalsByNegotiationId(long opportunityNegotiationId, CancellationToken cancellationToken = default)
        {
            return await QueryWithDetails()
                .Where(item => item.OpportunityNegotiationId == opportunityNegotiationId)
                .OrderByDescending(item => item.RequestedAt)
                .ThenByDescending(item => item.Id)
                .ToListAsync(cancellationToken);
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
                throw new InvalidOperationException("Only pending approval requests can be decided.");
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
