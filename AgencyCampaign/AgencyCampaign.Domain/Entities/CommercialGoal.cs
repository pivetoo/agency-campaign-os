using AgencyCampaign.Domain.ValueObjects;
using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class CommercialGoal : Entity
    {
        public long? UserId { get; private set; }

        public CommercialGoalPeriodType PeriodType { get; private set; }

        public DateTimeOffset PeriodStart { get; private set; }

        public decimal TargetAmount { get; private set; }

        public string? Notes { get; private set; }

        public bool IsActive { get; private set; } = true;

        private CommercialGoal()
        {
        }

        public CommercialGoal(long? userId, CommercialGoalPeriodType periodType, DateTimeOffset periodStart, decimal targetAmount, string? notes = null)
        {
            if (targetAmount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(targetAmount), "commercialGoal.targetAmount.invalid");
            }

            UserId = userId;
            PeriodType = periodType;
            PeriodStart = NormalizeStart(periodType, periodStart);
            TargetAmount = targetAmount;
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
            CreatedAt = DateTimeOffset.UtcNow;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void Update(long? userId, CommercialGoalPeriodType periodType, DateTimeOffset periodStart, decimal targetAmount, string? notes, bool isActive)
        {
            if (targetAmount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(targetAmount), "commercialGoal.targetAmount.invalid");
            }

            UserId = userId;
            PeriodType = periodType;
            PeriodStart = NormalizeStart(periodType, periodStart);
            TargetAmount = targetAmount;
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
            IsActive = isActive;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public DateTimeOffset PeriodEnd()
        {
            return PeriodType switch
            {
                CommercialGoalPeriodType.Month => PeriodStart.AddMonths(1),
                CommercialGoalPeriodType.Quarter => PeriodStart.AddMonths(3),
                CommercialGoalPeriodType.Year => PeriodStart.AddYears(1),
                _ => PeriodStart.AddMonths(1)
            };
        }

        private static DateTimeOffset NormalizeStart(CommercialGoalPeriodType periodType, DateTimeOffset periodStart)
        {
            DateTimeOffset utc = periodStart.ToUniversalTime();
            return periodType switch
            {
                CommercialGoalPeriodType.Month => new DateTimeOffset(utc.Year, utc.Month, 1, 0, 0, 0, TimeSpan.Zero),
                CommercialGoalPeriodType.Quarter => new DateTimeOffset(utc.Year, ((utc.Month - 1) / 3) * 3 + 1, 1, 0, 0, 0, TimeSpan.Zero),
                CommercialGoalPeriodType.Year => new DateTimeOffset(utc.Year, 1, 1, 0, 0, 0, TimeSpan.Zero),
                _ => utc
            };
        }
    }
}
