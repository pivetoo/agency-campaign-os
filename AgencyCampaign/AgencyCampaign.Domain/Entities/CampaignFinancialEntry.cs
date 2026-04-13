using AgencyCampaign.Domain.ValueObjects;
using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class CampaignFinancialEntry : Entity
    {
        public long CampaignId { get; private set; }

        public Campaign? Campaign { get; private set; }

        public long? CampaignDeliverableId { get; private set; }

        public CampaignDeliverable? CampaignDeliverable { get; private set; }

        public CampaignFinancialEntryType Type { get; private set; }

        public string Description { get; private set; } = string.Empty;

        public decimal Amount { get; private set; }

        public DateTimeOffset DueAt { get; private set; }

        public DateTimeOffset? PaidAt { get; private set; }

        public CampaignFinancialEntryStatus Status { get; private set; } = CampaignFinancialEntryStatus.Pending;

        public string? CounterpartyName { get; private set; }

        public string? Notes { get; private set; }

        private CampaignFinancialEntry()
        {
        }

        public CampaignFinancialEntry(long campaignId, CampaignFinancialEntryType type, string description, decimal amount, DateTimeOffset dueAt, string? counterpartyName = null, string? notes = null, long? campaignDeliverableId = null)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(campaignId);
            ArgumentException.ThrowIfNullOrWhiteSpace(description);
            ArgumentOutOfRangeException.ThrowIfNegative(amount);

            CampaignId = campaignId;
            CampaignDeliverableId = campaignDeliverableId;
            Type = type;
            Description = description.Trim();
            Amount = amount;
            DueAt = dueAt.ToUniversalTime();
            CounterpartyName = string.IsNullOrWhiteSpace(counterpartyName) ? null : counterpartyName.Trim();
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        }

        public void Update(CampaignFinancialEntryType type, string description, decimal amount, DateTimeOffset dueAt, string? counterpartyName, string? notes, long? campaignDeliverableId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(description);
            ArgumentOutOfRangeException.ThrowIfNegative(amount);

            Type = type;
            Description = description.Trim();
            Amount = amount;
            DueAt = dueAt.ToUniversalTime();
            CounterpartyName = string.IsNullOrWhiteSpace(counterpartyName) ? null : counterpartyName.Trim();
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
            CampaignDeliverableId = campaignDeliverableId;
        }

        public void ChangeStatus(CampaignFinancialEntryStatus status, DateTimeOffset? paidAt = null)
        {
            Status = status;
            PaidAt = status == CampaignFinancialEntryStatus.Paid ? paidAt?.ToUniversalTime() : null;
        }
    }
}
