using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class ProposalTemplateItem : Entity
    {
        public long ProposalTemplateId { get; private set; }

        public ProposalTemplate? ProposalTemplate { get; private set; }

        public string Description { get; private set; } = string.Empty;

        public int DefaultQuantity { get; private set; } = 1;

        public decimal DefaultUnitPrice { get; private set; }

        public int? DefaultDeliveryDays { get; private set; }

        public string? Observations { get; private set; }

        public int DisplayOrder { get; private set; }

        private ProposalTemplateItem()
        {
        }

        public ProposalTemplateItem(long proposalTemplateId, string description, int defaultQuantity, decimal defaultUnitPrice, int? defaultDeliveryDays, string? observations, int displayOrder)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(proposalTemplateId);
            ArgumentException.ThrowIfNullOrWhiteSpace(description);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(defaultQuantity);
            ArgumentOutOfRangeException.ThrowIfNegative(defaultUnitPrice);

            ProposalTemplateId = proposalTemplateId;
            Description = description.Trim();
            DefaultQuantity = defaultQuantity;
            DefaultUnitPrice = defaultUnitPrice;
            DefaultDeliveryDays = defaultDeliveryDays.HasValue && defaultDeliveryDays.Value >= 0 ? defaultDeliveryDays : null;
            Observations = Normalize(observations);
            DisplayOrder = displayOrder;
            CreatedAt = DateTimeOffset.UtcNow;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void Update(string description, int defaultQuantity, decimal defaultUnitPrice, int? defaultDeliveryDays, string? observations, int displayOrder)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(description);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(defaultQuantity);
            ArgumentOutOfRangeException.ThrowIfNegative(defaultUnitPrice);

            Description = description.Trim();
            DefaultQuantity = defaultQuantity;
            DefaultUnitPrice = defaultUnitPrice;
            DefaultDeliveryDays = defaultDeliveryDays.HasValue && defaultDeliveryDays.Value >= 0 ? defaultDeliveryDays : null;
            Observations = Normalize(observations);
            DisplayOrder = displayOrder;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
