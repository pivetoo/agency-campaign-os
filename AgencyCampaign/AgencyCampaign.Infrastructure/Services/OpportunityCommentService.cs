using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Models.Commercial;
using AgencyCampaign.Application.Requests.Opportunities;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Archon.Application.Abstractions;
using Archon.Application.Services;
using Archon.Core.Notifications;
using Archon.Core.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class OpportunityCommentService : IOpportunityCommentService
    {
        private readonly DbContext dbContext;
        private readonly ICurrentUser currentUser;
        private readonly INotificationService notificationService;

        public OpportunityCommentService(DbContext dbContext, ICurrentUser currentUser, INotificationService notificationService)
        {
            this.dbContext = dbContext;
            this.currentUser = currentUser;
            this.notificationService = notificationService;
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

            await NotifyMentionedUsers(opportunityId, comment, authorName, request.MentionedUserIds, cancellationToken);

            return Map(comment);
        }

        private async Task NotifyMentionedUsers(long opportunityId, OpportunityComment comment, string authorName, List<long>? mentionedUserIds, CancellationToken cancellationToken)
        {
            if (mentionedUserIds is null || mentionedUserIds.Count == 0)
            {
                return;
            }

            HashSet<long> distinct = mentionedUserIds
                .Where(userId => userId > 0 && userId != currentUser.UserId)
                .ToHashSet();

            if (distinct.Count == 0)
            {
                return;
            }

            string excerpt = comment.Body.Length > 140 ? comment.Body[..140] + "..." : comment.Body;
            string link = $"/comercial/oportunidades/{opportunityId}?tab=activity#comment-{comment.Id}";

            foreach (long userId in distinct)
            {
                CreateNotificationRequest notification = new()
                {
                    UserId = userId,
                    Title = "Você foi mencionado em uma oportunidade",
                    Message = $"{authorName}: {excerpt}",
                    Type = NotificationType.Info,
                    Link = link,
                    Source = "AgencyCampaign.OpportunityComment",
                    ReferenceEntityName = "OpportunityComment",
                    ReferenceEntityId = comment.Id.ToString()
                };

                await notificationService.Create(notification, cancellationToken);
            }
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
                throw new InvalidOperationException("opportunityComment.delete.onlyAuthor");
            }

            comment.MarkAsDeleted();
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task EnsureOpportunityExists(long opportunityId, CancellationToken cancellationToken)
        {
            bool exists = await dbContext.Set<Opportunity>()
                .AsNoTracking()
                .AnyAsync(item => item.Id == opportunityId, cancellationToken);

            if (!exists)
            {
                throw new InvalidOperationException("record.notFound");
            }
        }

        private async Task<OpportunityComment> GetTrackedComment(long commentId, CancellationToken cancellationToken)
        {
            OpportunityComment? comment = await dbContext.Set<OpportunityComment>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == commentId, cancellationToken);

            if (comment is null)
            {
                throw new InvalidOperationException("record.notFound");
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
                Body = comment.IsDeleted ? string.Empty : comment.Body,
                IsDeleted = comment.IsDeleted,
                CreatedAt = comment.CreatedAt,
                UpdatedAt = comment.UpdatedAt
            };
        }
    }
}
