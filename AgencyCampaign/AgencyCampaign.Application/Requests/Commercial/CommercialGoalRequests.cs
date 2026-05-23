using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.Commercial
{
    public sealed class CreateCommercialGoalRequest
    {
        public long? UserId { get; set; }

        [Required]
        [Range(1, 3)]
        public int PeriodType { get; set; } = 1;

        [Required]
        public DateTimeOffset PeriodStart { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal TargetAmount { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }
    }

    public sealed class UpdateCommercialGoalRequest
    {
        [Required]
        public long Id { get; set; }

        public long? UserId { get; set; }

        [Required]
        [Range(1, 3)]
        public int PeriodType { get; set; } = 1;

        [Required]
        public DateTimeOffset PeriodStart { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal TargetAmount { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
