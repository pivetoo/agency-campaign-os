using AgencyCampaign.Domain.ValueObjects;
using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class FinancialEntry : Entity
    {
        public long AccountId { get; private set; }

        public FinancialAccount? Account { get; private set; }

        public long? CampaignId { get; private set; }

        public Campaign? Campaign { get; private set; }

        public long? CampaignDeliverableId { get; private set; }

        public CampaignDeliverable? CampaignDeliverable { get; private set; }

        public long? SourceProposalId { get; private set; }

        public Proposal? SourceProposal { get; private set; }

        public FinancialEntryType Type { get; private set; }

        public FinancialEntryCategory Category { get; private set; }

        public string Description { get; private set; } = string.Empty;

        public decimal Amount { get; private set; }

        public DateTimeOffset DueAt { get; private set; }

        public DateTimeOffset OccurredAt { get; private set; }

        public string? PaymentMethod { get; private set; }

        public string? ReferenceCode { get; private set; }

        public DateTimeOffset? PaidAt { get; private set; }

        public FinancialEntryStatus Status { get; private set; } = FinancialEntryStatus.Pending;

        public string? CounterpartyName { get; private set; }

        public string? Notes { get; private set; }

        public long? SubcategoryId { get; private set; }

        public FinancialSubcategory? Subcategory { get; private set; }

        public long? ParentEntryId { get; private set; }

        public FinancialEntry? ParentEntry { get; private set; }

        public int? InstallmentNumber { get; private set; }

        public int? InstallmentTotal { get; private set; }

        public string? InvoiceNumber { get; private set; }

        public string? InvoiceUrl { get; private set; }

        public DateTimeOffset? InvoiceIssuedAt { get; private set; }

        private FinancialEntry()
        {
        }

        public FinancialEntry(long accountId, FinancialEntryType type, FinancialEntryCategory category, string description, decimal amount, DateTimeOffset dueAt, DateTimeOffset occurredAt, string? paymentMethod = null, string? referenceCode = null, string? counterpartyName = null, string? notes = null, long? campaignId = null, long? campaignDeliverableId = null)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(accountId);
            ArgumentException.ThrowIfNullOrWhiteSpace(description);
            ArgumentOutOfRangeException.ThrowIfNegative(amount);

            AccountId = accountId;
            CampaignId = campaignId;
            CampaignDeliverableId = campaignDeliverableId;
            Type = type;
            Category = category;
            Description = description.Trim();
            Amount = amount;
            DueAt = dueAt.ToUniversalTime();
            OccurredAt = occurredAt.ToUniversalTime();
            PaymentMethod = Normalize(paymentMethod);
            ReferenceCode = Normalize(referenceCode);
            CounterpartyName = Normalize(counterpartyName);
            Notes = Normalize(notes);
        }

        public void Update(long accountId, FinancialEntryType type, FinancialEntryCategory category, string description, decimal amount, DateTimeOffset dueAt, DateTimeOffset occurredAt, string? paymentMethod, string? referenceCode, string? counterpartyName, string? notes, long? campaignId, long? campaignDeliverableId)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(accountId);
            ArgumentException.ThrowIfNullOrWhiteSpace(description);
            ArgumentOutOfRangeException.ThrowIfNegative(amount);

            AccountId = accountId;
            CampaignId = campaignId;
            CampaignDeliverableId = campaignDeliverableId;
            Type = type;
            Category = category;
            Description = description.Trim();
            Amount = amount;
            DueAt = dueAt.ToUniversalTime();
            OccurredAt = occurredAt.ToUniversalTime();
            PaymentMethod = Normalize(paymentMethod);
            ReferenceCode = Normalize(referenceCode);
            CounterpartyName = Normalize(counterpartyName);
            Notes = Normalize(notes);
        }

        public void ChangeStatus(FinancialEntryStatus status, DateTimeOffset? paidAt = null)
        {
            Status = status;
            PaidAt = status == FinancialEntryStatus.Paid ? paidAt?.ToUniversalTime() : null;
        }

        public void LinkToProposal(long proposalId)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(proposalId);
            SourceProposalId = proposalId;
        }

        public void SetSubcategory(long? subcategoryId)
        {
            SubcategoryId = subcategoryId;
        }

        public void MarkAsInstallment(long? parentEntryId, int installmentNumber, int installmentTotal)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(installmentNumber);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(installmentTotal);
            if (installmentNumber > installmentTotal)
            {
                throw new ArgumentOutOfRangeException(nameof(installmentNumber), "Número da parcela não pode ser maior que o total.");
            }

            ParentEntryId = parentEntryId;
            InstallmentNumber = installmentNumber;
            InstallmentTotal = installmentTotal;
        }

        public void SetInvoice(string? invoiceNumber, string? invoiceUrl, DateTimeOffset? issuedAt)
        {
            InvoiceNumber = Normalize(invoiceNumber);
            InvoiceUrl = Normalize(invoiceUrl);
            InvoiceIssuedAt = issuedAt?.ToUniversalTime();
        }

        public void RecalculateOverdue(DateTimeOffset now)
        {
            if (Status == FinancialEntryStatus.Pending && DueAt < now)
            {
                Status = FinancialEntryStatus.Overdue;
                return;
            }

            if (Status == FinancialEntryStatus.Overdue && DueAt >= now)
            {
                Status = FinancialEntryStatus.Pending;
            }
        }

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
