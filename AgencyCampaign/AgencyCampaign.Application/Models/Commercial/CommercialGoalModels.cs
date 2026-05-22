namespace AgencyCampaign.Application.Models.Commercial
{
    public sealed class CommercialGoalModel
    {
        public long Id { get; init; }

        public long? UserId { get; init; }

        public string? UserName { get; init; }

        public int PeriodType { get; init; }

        public DateTimeOffset PeriodStart { get; init; }

        public DateTimeOffset PeriodEnd { get; init; }

        public decimal TargetAmount { get; init; }

        public string? Notes { get; init; }

        public bool IsActive { get; init; }
    }

    public sealed class CommercialGoalProgressModel
    {
        public long Id { get; init; }

        public long? UserId { get; init; }

        public string? UserName { get; init; }

        public int PeriodType { get; init; }

        public DateTimeOffset PeriodStart { get; init; }

        public DateTimeOffset PeriodEnd { get; init; }

        public decimal TargetAmount { get; init; }

        public decimal AchievedAmount { get; init; }

        public int AchievedDealsCount { get; init; }

        public decimal PercentAchieved { get; init; }
    }
}
