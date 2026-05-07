using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.Opportunities
{
    public sealed class CreateOpportunitySourceRequest
    {
        [Required]
        [StringLength(120, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(32)]
        public string Color { get; set; } = "#6366f1";

        public int DisplayOrder { get; set; }
    }

    public sealed class UpdateOpportunitySourceRequest
    {
        [Required]
        public long Id { get; set; }

        [Required]
        [StringLength(120, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(32)]
        public string Color { get; set; } = "#6366f1";

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public sealed class CreateOpportunityTagRequest
    {
        [Required]
        [StringLength(80, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(32)]
        public string Color { get; set; } = "#6366f1";
    }

    public sealed class UpdateOpportunityTagRequest
    {
        [Required]
        public long Id { get; set; }

        [Required]
        [StringLength(80, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(32)]
        public string Color { get; set; } = "#6366f1";

        public bool IsActive { get; set; } = true;
    }
}
