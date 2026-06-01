using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.Proposals
{
    public sealed class CreateRateCardItemRequest
    {
        [Required]
        [Range(1, long.MaxValue)]
        public long CreatorId { get; set; }

        [Required]
        [StringLength(160, MinimumLength = 1)]
        public string Label { get; set; } = string.Empty;

        [Range(0, double.MaxValue)]
        public decimal UnitPrice { get; set; }

        public int DisplayOrder { get; set; }
    }

    public sealed class UpdateRateCardItemRequest
    {
        [Required]
        public long Id { get; set; }

        [Required]
        [StringLength(160, MinimumLength = 1)]
        public string Label { get; set; } = string.Empty;

        [Range(0, double.MaxValue)]
        public decimal UnitPrice { get; set; }

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
