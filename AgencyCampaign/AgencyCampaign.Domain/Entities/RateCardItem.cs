using AgencyCampaign.Domain.ValueObjects;
using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    // Item de rate card: preco padrao de um entregavel para um creator (ex: "Reel" = R$ 3.000).
    // Usado para montar propostas escolhendo do catalogo em vez de redigitar valor toda vez.
    public sealed class RateCardItem : Entity
    {
        public long CreatorId { get; private set; }

        public Creator? Creator { get; private set; }

        public string Label { get; private set; } = string.Empty;

        public decimal UnitPrice { get; private set; }

        public int DisplayOrder { get; private set; }

        public bool IsActive { get; private set; } = true;

        private RateCardItem()
        {
        }

        public RateCardItem(long creatorId, string label, decimal unitPrice, int displayOrder)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(creatorId);
            ArgumentException.ThrowIfNullOrWhiteSpace(label);
            ArgumentOutOfRangeException.ThrowIfNegative(unitPrice);

            CreatorId = creatorId;
            Label = label.Trim();
            UnitPrice = Money.Round(unitPrice);
            DisplayOrder = displayOrder;
            CreatedAt = DateTimeOffset.UtcNow;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void Update(string label, decimal unitPrice, int displayOrder, bool isActive)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(label);
            ArgumentOutOfRangeException.ThrowIfNegative(unitPrice);

            Label = label.Trim();
            UnitPrice = Money.Round(unitPrice);
            DisplayOrder = displayOrder;
            IsActive = isActive;
            UpdatedAt = DateTimeOffset.UtcNow;
        }
    }
}
