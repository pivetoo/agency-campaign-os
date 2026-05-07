using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.Proposals
{
    public sealed class CreateProposalTemplateRequest
    {
        [Required]
        [StringLength(150, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        public IReadOnlyCollection<ProposalTemplateItemRequest> Items { get; set; } = [];
    }

    public sealed class UpdateProposalTemplateRequest
    {
        [Required]
        public long Id { get; set; }

        [Required]
        [StringLength(150, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public IReadOnlyCollection<ProposalTemplateItemRequest> Items { get; set; } = [];
    }

    public sealed class ProposalTemplateItemRequest
    {
        [Required]
        [StringLength(1000, MinimumLength = 1)]
        public string Description { get; set; } = string.Empty;

        [Range(1, int.MaxValue)]
        public int DefaultQuantity { get; set; } = 1;

        [Range(0, double.MaxValue)]
        public decimal DefaultUnitPrice { get; set; }

        [Range(0, int.MaxValue)]
        public int? DefaultDeliveryDays { get; set; }

        [StringLength(1000)]
        public string? Observations { get; set; }

        public int DisplayOrder { get; set; }
    }

    public sealed class CreateProposalBlockRequest
    {
        [Required]
        [StringLength(150, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MinLength(1)]
        public string Body { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Category { get; set; } = string.Empty;
    }

    public sealed class UpdateProposalBlockRequest
    {
        [Required]
        public long Id { get; set; }

        [Required]
        [StringLength(150, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MinLength(1)]
        public string Body { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Category { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }
}
