using AgencyCampaign.Domain.ValueObjects;

namespace AgencyCampaign.Application.Requests.ContentReview
{
    public sealed record ContentAssetInput(ContentAssetType Type, string Url, string? FileName, string? ContentType);

    public sealed record AddContentVersionRequest(IReadOnlyList<ContentAssetInput> Assets, string? Note);

    public sealed record AddReviewCommentRequest(long? VersionId, string Body, ReviewCommentVisibility Visibility);
}
