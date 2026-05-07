namespace AgencyCampaign.Application.Models.Commercial
{
    public sealed class ProposalTemplateModel
    {
        public long Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public string? Description { get; init; }

        public bool IsActive { get; init; }

        public string? CreatedByUserName { get; init; }

        public DateTimeOffset CreatedAt { get; init; }

        public DateTimeOffset? UpdatedAt { get; init; }

        public IReadOnlyCollection<ProposalTemplateItemModel> Items { get; init; } = [];
    }

    public sealed class ProposalTemplateItemModel
    {
        public long Id { get; init; }

        public long ProposalTemplateId { get; init; }

        public string Description { get; init; } = string.Empty;

        public int DefaultQuantity { get; init; }

        public decimal DefaultUnitPrice { get; init; }

        public int? DefaultDeliveryDays { get; init; }

        public string? Observations { get; init; }

        public int DisplayOrder { get; init; }
    }

    public sealed class ProposalBlockModel
    {
        public long Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public string Body { get; init; } = string.Empty;

        public string Category { get; init; } = string.Empty;

        public bool IsActive { get; init; }

        public string? CreatedByUserName { get; init; }

        public DateTimeOffset CreatedAt { get; init; }

        public DateTimeOffset? UpdatedAt { get; init; }
    }
}
