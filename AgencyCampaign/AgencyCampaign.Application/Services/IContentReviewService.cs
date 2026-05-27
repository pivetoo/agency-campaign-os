using AgencyCampaign.Application.Requests.ContentReview;
using AgencyCampaign.Domain.ValueObjects;

namespace AgencyCampaign.Application.Services
{
    public interface IContentReviewService
    {
        Task<ContentReviewModel> GetByDeliverable(long deliverableId, CancellationToken cancellationToken = default);
        Task<ContentReviewModel> AddVersion(long deliverableId, ReviewParticipant role, string authorName, AddContentVersionRequest request, CancellationToken cancellationToken = default);
        Task<ContentReviewModel> RequestChanges(long versionId, string authorName, string comment, CancellationToken cancellationToken = default);
        Task<ContentReviewModel> SendToBrand(long versionId, CancellationToken cancellationToken = default);
        Task<ContentReviewModel> AddComment(long deliverableId, ReviewParticipant role, string authorName, AddReviewCommentRequest request, CancellationToken cancellationToken = default);
        Task<ContentReviewModel> BrandApprove(long deliverableId, CancellationToken cancellationToken = default);
        Task<ContentReviewModel> BrandRequestChanges(long deliverableId, string reviewerName, string comment, CancellationToken cancellationToken = default);
        Task<ContentReviewModel> BrandAddComment(long deliverableId, string reviewerName, string body, CancellationToken cancellationToken = default);
    }

    public sealed class ContentReviewModel
    {
        public long DeliverableId { get; init; }
        public IReadOnlyList<ContentVersionModel> Versions { get; init; } = Array.Empty<ContentVersionModel>();
        public IReadOnlyList<ReviewCommentModel> Comments { get; init; } = Array.Empty<ReviewCommentModel>();
    }

    public sealed class ContentVersionModel
    {
        public long Id { get; init; }
        public int RoundNumber { get; init; }
        public ReviewParticipant SubmittedByRole { get; init; }
        public string SubmittedByName { get; init; } = string.Empty;
        public string? Note { get; init; }
        public ContentVersionStatus Status { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        public IReadOnlyList<ContentAssetModel> Assets { get; init; } = Array.Empty<ContentAssetModel>();
    }

    public sealed class ContentAssetModel
    {
        public ContentAssetType Type { get; init; }
        public string Url { get; init; } = string.Empty;
        public string? FileName { get; init; }
    }

    public sealed class ReviewCommentModel
    {
        public long Id { get; init; }
        public long? VersionId { get; init; }
        public ReviewParticipant AuthorRole { get; init; }
        public string AuthorName { get; init; } = string.Empty;
        public string Body { get; init; } = string.Empty;
        public ReviewCommentVisibility Visibility { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
    }
}
