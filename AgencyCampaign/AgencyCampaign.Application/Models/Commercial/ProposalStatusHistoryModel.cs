namespace AgencyCampaign.Application.Models.Commercial
{
    public sealed class ProposalStatusHistoryModel
    {
        public long Id { get; init; }

        public long ProposalId { get; init; }

        public int? FromStatus { get; init; }

        public int ToStatus { get; init; }

        public DateTimeOffset ChangedAt { get; init; }

        public long? ChangedByUserId { get; init; }

        public string? ChangedByUserName { get; init; }

        public string? Reason { get; init; }
    }
}
