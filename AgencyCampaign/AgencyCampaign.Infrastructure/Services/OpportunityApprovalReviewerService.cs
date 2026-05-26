using AgencyCampaign.Application.Models.Commercial;
using AgencyCampaign.Application.Requests.Opportunities;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class OpportunityApprovalReviewerService : IOpportunityApprovalReviewerService
    {
        private readonly DbContext dbContext;

        public OpportunityApprovalReviewerService(DbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<IReadOnlyCollection<OpportunityApprovalReviewerModel>> GetByApprovalId(long approvalId, CancellationToken cancellationToken = default)
        {
            return await dbContext.Set<OpportunityApprovalReviewer>()
                .AsNoTracking()
                .Where(item => item.OpportunityApprovalRequestId == approvalId)
                .OrderByDescending(item => item.Required)
                .ThenBy(item => item.CreatedAt)
                .Select(item => Map(item))
                .ToListAsync(cancellationToken);
        }

        public async Task<OpportunityApprovalReviewerModel> Add(long approvalId, AddOpportunityApprovalReviewerRequest request, CancellationToken cancellationToken = default)
        {
            if (request.UserId is long userId)
            {
                bool alreadyReviewer = await dbContext.Set<OpportunityApprovalReviewer>()
                    .AsNoTracking()
                    .AnyAsync(item => item.OpportunityApprovalRequestId == approvalId && item.UserId == userId, cancellationToken);

                if (alreadyReviewer)
                {
                    throw new InvalidOperationException("opportunityApprovalReviewer.duplicate");
                }
            }

            OpportunityApprovalReviewer reviewer = new(approvalId, request.UserName, request.Role, request.Required, request.UserId);
            dbContext.Set<OpportunityApprovalReviewer>().Add(reviewer);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Map(reviewer);
        }

        public async Task Remove(long id, CancellationToken cancellationToken = default)
        {
            OpportunityApprovalReviewer? reviewer = await dbContext.Set<OpportunityApprovalReviewer>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (reviewer is null)
            {
                throw new InvalidOperationException("record.notFound");
            }

            dbContext.Set<OpportunityApprovalReviewer>().Remove(reviewer);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        private static OpportunityApprovalReviewerModel Map(OpportunityApprovalReviewer reviewer) => new()
        {
            Id = reviewer.Id,
            OpportunityApprovalRequestId = reviewer.OpportunityApprovalRequestId,
            UserId = reviewer.UserId,
            UserName = reviewer.UserName,
            Role = reviewer.Role,
            Required = reviewer.Required,
            Status = (int)reviewer.Status,
            DecidedAt = reviewer.DecidedAt,
            DecisionNotes = reviewer.DecisionNotes,
            CreatedAt = reviewer.CreatedAt,
        };
    }
}
