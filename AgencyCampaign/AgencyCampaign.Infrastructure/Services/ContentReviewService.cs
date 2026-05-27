using AgencyCampaign.Application.Requests.ContentReview;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class ContentReviewService : IContentReviewService
    {
        private readonly DbContext dbContext;
        private readonly IContentFileStorage fileStorage;

        public ContentReviewService(DbContext dbContext, IContentFileStorage fileStorage)
        {
            this.dbContext = dbContext;
            this.fileStorage = fileStorage;
        }

        public async Task<ContentReviewModel> GetByDeliverable(long deliverableId, CancellationToken cancellationToken = default)
        {
            return await BuildModel(deliverableId, includeInternal: true, cancellationToken);
        }

        public async Task<ContentReviewModel> AddVersion(long deliverableId, ReviewParticipant role, string authorName, AddContentVersionRequest request, CancellationToken cancellationToken = default)
        {
            DeliverableContentVersion? current = await dbContext.Set<DeliverableContentVersion>()
                .AsNoTracking()
                .Where(item => item.CampaignDeliverableId == deliverableId)
                .OrderByDescending(item => item.RoundNumber)
                .FirstOrDefaultAsync(cancellationToken);

            if (current is not null && current.Status != ContentVersionStatus.ChangesRequested)
            {
                throw new InvalidOperationException("contentReview.version.alreadyOpen");
            }

            int nextRound = (current?.RoundNumber ?? 0) + 1;
            DeliverableContentVersion version = new(deliverableId, nextRound, role, authorName, request.Note);
            int order = 0;
            foreach (ContentAssetInput asset in request.Assets)
            {
                version.AddAsset(asset.Type, asset.Url, asset.FileName, asset.ContentType, order);
                order++;
            }

            dbContext.Set<DeliverableContentVersion>().Add(version);
            await dbContext.SaveChangesAsync(cancellationToken);
            return await BuildModel(deliverableId, includeInternal: true, cancellationToken);
        }

        public async Task<ContentReviewModel> RequestChanges(long versionId, string authorName, string comment, CancellationToken cancellationToken = default)
        {
            DeliverableContentVersion version = await LoadVersion(versionId, cancellationToken);
            version.RequestChanges();
            dbContext.Set<DeliverableReviewComment>().Add(new DeliverableReviewComment(version.CampaignDeliverableId, version.Id, ReviewParticipant.Agency, authorName, comment, ReviewCommentVisibility.Shared));
            await dbContext.SaveChangesAsync(cancellationToken);
            return await BuildModel(version.CampaignDeliverableId, includeInternal: true, cancellationToken);
        }

        public async Task<ContentReviewModel> SendToBrand(long versionId, CancellationToken cancellationToken = default)
        {
            DeliverableContentVersion version = await LoadVersion(versionId, cancellationToken);
            version.SendToBrand();
            await dbContext.SaveChangesAsync(cancellationToken);
            return await BuildModel(version.CampaignDeliverableId, includeInternal: true, cancellationToken);
        }

        public async Task<ContentReviewModel> AddComment(long deliverableId, ReviewParticipant role, string authorName, AddReviewCommentRequest request, CancellationToken cancellationToken = default)
        {
            dbContext.Set<DeliverableReviewComment>().Add(new DeliverableReviewComment(deliverableId, request.VersionId, role, authorName, request.Body, request.Visibility));
            await dbContext.SaveChangesAsync(cancellationToken);
            return await BuildModel(deliverableId, includeInternal: role == ReviewParticipant.Agency, cancellationToken);
        }

        private async Task<DeliverableContentVersion> LoadVersion(long versionId, CancellationToken cancellationToken)
        {
            DeliverableContentVersion? version = await dbContext.Set<DeliverableContentVersion>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == versionId, cancellationToken);
            if (version is null)
            {
                throw new InvalidOperationException("record.notFound");
            }

            return version;
        }

        private async Task<ContentReviewModel> BuildModel(long deliverableId, bool includeInternal, CancellationToken cancellationToken)
        {
            List<DeliverableContentVersion> versions = await dbContext.Set<DeliverableContentVersion>()
                .AsNoTracking()
                .Include(item => item.Assets)
                .Where(item => item.CampaignDeliverableId == deliverableId)
                .OrderBy(item => item.RoundNumber)
                .ToListAsync(cancellationToken);

            IQueryable<DeliverableReviewComment> commentsQuery = dbContext.Set<DeliverableReviewComment>()
                .AsNoTracking()
                .Where(item => item.CampaignDeliverableId == deliverableId);
            if (!includeInternal)
            {
                commentsQuery = commentsQuery.Where(item => item.Visibility == ReviewCommentVisibility.Shared);
            }

            List<DeliverableReviewComment> comments = await commentsQuery.OrderBy(item => item.CreatedAt).ToListAsync(cancellationToken);

            return new ContentReviewModel
            {
                DeliverableId = deliverableId,
                Versions = versions.Select(version => new ContentVersionModel
                {
                    Id = version.Id,
                    RoundNumber = version.RoundNumber,
                    SubmittedByRole = version.SubmittedByRole,
                    SubmittedByName = version.SubmittedByName,
                    Note = version.Note,
                    Status = version.Status,
                    CreatedAt = version.CreatedAt,
                    Assets = version.Assets.OrderBy(asset => asset.DisplayOrder).Select(asset => new ContentAssetModel
                    {
                        Type = asset.Type,
                        Url = asset.Url,
                        FileName = asset.FileName
                    }).ToList()
                }).ToList(),
                Comments = comments.Select(comment => new ReviewCommentModel
                {
                    Id = comment.Id,
                    VersionId = comment.DeliverableContentVersionId,
                    AuthorRole = comment.AuthorRole,
                    AuthorName = comment.AuthorName,
                    Body = comment.Body,
                    Visibility = comment.Visibility,
                    CreatedAt = comment.CreatedAt
                }).ToList()
            };
        }
    }
}
