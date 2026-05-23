using AgencyCampaign.Application.Models.Commercial;
using AgencyCampaign.Application.Requests.Opportunities;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class OpportunityApprovalCommentService : IOpportunityApprovalCommentService
    {
        private readonly DbContext dbContext;

        public OpportunityApprovalCommentService(DbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<IReadOnlyCollection<OpportunityApprovalCommentModel>> GetByApprovalId(long approvalId, CancellationToken cancellationToken = default)
        {
            return await dbContext.Set<OpportunityApprovalComment>()
                .AsNoTracking()
                .Where(item => item.OpportunityApprovalRequestId == approvalId)
                .OrderBy(item => item.CreatedAt)
                .Select(item => Map(item))
                .ToListAsync(cancellationToken);
        }

        public async Task<OpportunityApprovalCommentModel> Create(long approvalId, CreateOpportunityApprovalCommentRequest request, CancellationToken cancellationToken = default)
        {
            OpportunityApprovalComment comment = new(approvalId, request.UserName, request.Body, request.Role ?? "observador", request.UserId);
            dbContext.Set<OpportunityApprovalComment>().Add(comment);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Map(comment);
        }

        public async Task<OpportunityApprovalCommentModel> Update(long id, UpdateOpportunityApprovalCommentRequest request, CancellationToken cancellationToken = default)
        {
            if (id != request.Id)
            {
                throw new InvalidOperationException("request.route.idMismatch");
            }

            OpportunityApprovalComment? comment = await dbContext.Set<OpportunityApprovalComment>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (comment is null)
            {
                throw new InvalidOperationException("record.notFound");
            }

            comment.Edit(request.Body);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Map(comment);
        }

        public async Task Delete(long id, CancellationToken cancellationToken = default)
        {
            OpportunityApprovalComment? comment = await dbContext.Set<OpportunityApprovalComment>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (comment is null)
            {
                throw new InvalidOperationException("record.notFound");
            }

            dbContext.Set<OpportunityApprovalComment>().Remove(comment);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        private static OpportunityApprovalCommentModel Map(OpportunityApprovalComment comment) => new()
        {
            Id = comment.Id,
            OpportunityApprovalRequestId = comment.OpportunityApprovalRequestId,
            UserId = comment.UserId,
            UserName = comment.UserName,
            Role = comment.Role,
            Body = comment.Body,
            CreatedAt = comment.CreatedAt,
            UpdatedAt = comment.UpdatedAt,
        };
    }
}
