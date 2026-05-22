using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.Opportunities
{
    public sealed class CreateOpportunityWinReasonRequest
    {
        [Required]
        [StringLength(120, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(32)]
        public string Color { get; set; } = "#15803d";

        public int DisplayOrder { get; set; }
    }

    public sealed class UpdateOpportunityWinReasonRequest
    {
        [Required]
        public long Id { get; set; }

        [Required]
        [StringLength(120, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(32)]
        public string Color { get; set; } = "#15803d";

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public sealed class CreateOpportunityLossReasonRequest
    {
        [Required]
        [StringLength(120, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(32)]
        public string Color { get; set; } = "#b91c1c";

        public int DisplayOrder { get; set; }
    }

    public sealed class UpdateOpportunityLossReasonRequest
    {
        [Required]
        public long Id { get; set; }

        [Required]
        [StringLength(120, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(32)]
        public string Color { get; set; } = "#b91c1c";

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
