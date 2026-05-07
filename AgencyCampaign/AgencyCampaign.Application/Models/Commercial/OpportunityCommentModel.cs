namespace AgencyCampaign.Application.Models.Commercial
{
    public sealed class OpportunityCommentModel
    {
        public long Id { get; init; }

        public long OpportunityId { get; init; }

        public long? AuthorUserId { get; init; }

        public string AuthorName { get; init; } = string.Empty;

        public string Body { get; init; } = string.Empty;

        public DateTimeOffset CreatedAt { get; init; }

        public DateTimeOffset? UpdatedAt { get; init; }
    }
}
