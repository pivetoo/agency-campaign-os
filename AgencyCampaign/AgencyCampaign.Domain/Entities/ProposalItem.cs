using AgencyCampaign.Domain.ValueObjects;
using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class ProposalItem : Entity
    {
        public long ProposalId { get; private set; }

        public Proposal? Proposal { get; private set; }

        public long? CreatorId { get; private set; }

        public Creator? Creator { get; private set; }

        public string Description { get; private set; } = string.Empty;

        public int Quantity { get; private set; }

        public decimal UnitPrice { get; private set; }

        public DateTimeOffset? DeliveryDeadline { get; private set; }

        public ProposalItemStatus Status { get; private set; } = ProposalItemStatus.Pending;

        public string? Observations { get; private set; }

        public ProposalItemKind Kind { get; private set; } = ProposalItemKind.Deliverable;

        public int? UsageDurationMonths { get; private set; }

        public string? UsageScope { get; private set; }

        public ProposalItemPricingModel PricingModel { get; private set; } = ProposalItemPricingModel.Fixed;

        public decimal? VariableRate { get; private set; }

        public decimal? VariableBasis { get; private set; }

        public bool IsVariable => PricingModel != ProposalItemPricingModel.Fixed;

        public decimal Total => IsVariable
            ? Money.Round((VariableBasis ?? 0m) * (VariableRate ?? 0m) / 100m)
            : Money.Round(Quantity * UnitPrice);

        private ProposalItem()
        {
        }

        public ProposalItem(long proposalId, string description, int quantity, decimal unitPrice, DateTimeOffset? deliveryDeadline = null, long? creatorId = null, string? observations = null, ProposalItemKind kind = ProposalItemKind.Deliverable, int? usageDurationMonths = null, string? usageScope = null, ProposalItemPricingModel pricingModel = ProposalItemPricingModel.Fixed, decimal? variableRate = null, decimal? variableBasis = null)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(proposalId);
            ArgumentException.ThrowIfNullOrWhiteSpace(description);
            ArgumentOutOfRangeException.ThrowIfNegative(quantity);
            ArgumentOutOfRangeException.ThrowIfNegative(unitPrice);

            ProposalId = proposalId;
            Description = description.Trim();
            Quantity = quantity;
            UnitPrice = unitPrice;
            DeliveryDeadline = deliveryDeadline?.ToUniversalTime();
            CreatorId = creatorId;
            Observations = Normalize(observations);
            Status = ProposalItemStatus.Pending;
            SetUsage(kind, usageDurationMonths, usageScope);
            SetPricing(pricingModel, variableRate, variableBasis);
        }

        public void Update(string description, int quantity, decimal unitPrice, DateTimeOffset? deliveryDeadline, string? observations, ProposalItemKind kind = ProposalItemKind.Deliverable, int? usageDurationMonths = null, string? usageScope = null, ProposalItemPricingModel pricingModel = ProposalItemPricingModel.Fixed, decimal? variableRate = null, decimal? variableBasis = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(description);
            ArgumentOutOfRangeException.ThrowIfNegative(quantity);
            ArgumentOutOfRangeException.ThrowIfNegative(unitPrice);

            Description = description.Trim();
            Quantity = quantity;
            UnitPrice = unitPrice;
            DeliveryDeadline = deliveryDeadline?.ToUniversalTime();
            Observations = Normalize(observations);
            SetUsage(kind, usageDurationMonths, usageScope);
            SetPricing(pricingModel, variableRate, variableBasis);
        }

        private void SetUsage(ProposalItemKind kind, int? usageDurationMonths, string? usageScope)
        {
            Kind = kind;
            if (kind == ProposalItemKind.UsageRights)
            {
                UsageDurationMonths = usageDurationMonths.HasValue && usageDurationMonths.Value > 0 ? usageDurationMonths : null;
                UsageScope = Normalize(usageScope);
            }
            else
            {
                UsageDurationMonths = null;
                UsageScope = null;
            }
        }

        private void SetPricing(ProposalItemPricingModel pricingModel, decimal? variableRate, decimal? variableBasis)
        {
            PricingModel = pricingModel;
            if (pricingModel == ProposalItemPricingModel.Fixed)
            {
                VariableRate = null;
                VariableBasis = null;
            }
            else
            {
                VariableRate = variableRate.HasValue && variableRate.Value > 0m ? variableRate : null;
                VariableBasis = variableBasis.HasValue && variableBasis.Value > 0m ? variableBasis : null;
            }
        }

        public void AssignCreator(long creatorId)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(creatorId);
            CreatorId = creatorId;
        }

        public void RemoveCreator()
        {
            CreatorId = null;
        }

        public void ChangeStatus(ProposalItemStatus status)
        {
            Status = status;
        }

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}