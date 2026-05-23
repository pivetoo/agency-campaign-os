namespace AgencyCampaign.Application.Models.Commercial
{
    public sealed class OpportunityApprovalReviewerModel
    {
        public long Id { get; init; }

        public long OpportunityApprovalRequestId { get; init; }

        public long? UserId { get; init; }

        public string UserName { get; init; } = string.Empty;

        public string? Role { get; init; }

        public bool Required { get; init; }

        public int Status { get; init; }

        public DateTimeOffset? DecidedAt { get; init; }

        public string? DecisionNotes { get; init; }

        public DateTimeOffset CreatedAt { get; init; }
    }
}
