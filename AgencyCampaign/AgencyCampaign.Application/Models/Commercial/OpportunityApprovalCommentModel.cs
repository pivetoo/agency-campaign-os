namespace AgencyCampaign.Application.Models.Commercial
{
    public sealed class OpportunityApprovalCommentModel
    {
        public long Id { get; init; }

        public long OpportunityApprovalRequestId { get; init; }

        public long? UserId { get; init; }

        public string UserName { get; init; } = string.Empty;

        public string Role { get; init; } = string.Empty;

        public string Body { get; init; } = string.Empty;

        public DateTimeOffset CreatedAt { get; init; }

        public DateTimeOffset? UpdatedAt { get; init; }
    }
}
