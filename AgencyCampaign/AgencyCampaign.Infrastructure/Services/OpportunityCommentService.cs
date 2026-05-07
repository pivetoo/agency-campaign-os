using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Models.Commercial;
using AgencyCampaign.Application.Requests.Opportunities;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Archon.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class OpportunityCommentService : IOpportunityCommentService
    {
        private readonly DbContext dbContext;
        private readonly ICurrentUser currentUser;
        private readonly IStringLocalizer<AgencyCampaignResource> localizer;

        public OpportunityCommentService(DbContext dbContext, ICurrentUser currentUser, IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.dbContext = dbContext;
            this.currentUser = currentUser;
            this.localizer = localizer;
        }

        public async Task<IReadOnlyCollection<OpportunityCommentModel>> GetByOpportunityId(long opportunityId, CancellationToken cancellationToken = default)
        {
            await EnsureOpportunityExists(opportunityId, cancellationToken);

            return await dbContext.Set<OpportunityComment>()
                .AsNoTracking()
                .Where(item => item.OpportunityId == opportunityId)
                .OrderByDescending(item => item.CreatedAt)
                .Select(item => Map(item))
                .ToArrayAsync(cancellationToken);
        }

        public async Task<OpportunityCommentModel> CreateComment(long opportunityId, CreateOpportunityCommentRequest request, CancellationToken cancellationToken = default)
        {
            await EnsureOpportunityExists(opportunityId, cancellationToken);

            string authorName = currentUser.UserName ?? currentUser.Email ?? "Sistema";

            OpportunityComment comment = new(opportunityId, request.Body, currentUser.UserId, authorName);
            dbContext.Set<OpportunityComment>().Add(comment);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Map(comment);
        }

        public async Task<OpportunityCommentModel> UpdateComment(long commentId, UpdateOpportunityCommentRequest request, CancellationToken cancellationToken = default)
        {
            OpportunityComment comment = await GetTrackedComment(commentId, cancellationToken);
            comment.Update(request.Body, currentUser.UserId);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Map(comment);
        }

        public async Task DeleteComment(long commentId, CancellationToken cancellationToken = default)
        {
            OpportunityComment comment = await GetTrackedComment(commentId, cancellationToken);

            if (!comment.CanBeDeletedBy(currentUser.UserId))
            {
                throw new InvalidOperationException("Only the comment author can delete it.");
            }

            dbContext.Set<OpportunityComment>().Remove(comment);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task EnsureOpportunityExists(long opportunityId, CancellationToken cancellationToken)
        {
            bool exists = await dbContext.Set<Opportunity>()
                .AsNoTracking()
                .AnyAsync(item => item.Id == opportunityId, cancellationToken);

            if (!exists)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }
        }

        private async Task<OpportunityComment> GetTrackedComment(long commentId, CancellationToken cancellationToken)
        {
            OpportunityComment? comment = await dbContext.Set<OpportunityComment>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == commentId, cancellationToken);

            if (comment is null)
            {
                throw new InvalidOperationException(localizer["record.notFound"]);
            }

            return comment;
        }

        private static OpportunityCommentModel Map(OpportunityComment comment)
        {
            return new OpportunityCommentModel
            {
                Id = comment.Id,
                OpportunityId = comment.OpportunityId,
                AuthorUserId = comment.AuthorUserId,
                AuthorName = comment.AuthorName,
                Body = comment.Body,
                CreatedAt = comment.CreatedAt,
                UpdatedAt = comment.UpdatedAt
            };
        }
    }
}
