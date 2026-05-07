namespace AgencyCampaign.Application.Models.Commercial
{
    public sealed class OpportunityStageHistoryModel
    {
        public long Id { get; init; }

        public long OpportunityId { get; init; }

        public long? FromStageId { get; init; }

        public string? FromStageName { get; init; }

        public string? FromStageColor { get; init; }

        public long ToStageId { get; init; }

        public string ToStageName { get; init; } = string.Empty;

        public string? ToStageColor { get; init; }

        public DateTimeOffset ChangedAt { get; init; }

        public long? ChangedByUserId { get; init; }

        public string? ChangedByUserName { get; init; }

        public string? Reason { get; init; }
    }
}
