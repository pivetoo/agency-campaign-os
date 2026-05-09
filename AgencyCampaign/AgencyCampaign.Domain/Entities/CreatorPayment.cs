using AgencyCampaign.Domain.ValueObjects;
using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class CreatorPayment : Entity
    {
        private readonly List<CreatorPaymentEvent> events = [];

        public long CampaignCreatorId { get; private set; }

        public CampaignCreator? CampaignCreator { get; private set; }

        public long CreatorId { get; private set; }

        public Creator? Creator { get; private set; }

        public long? CampaignDocumentId { get; private set; }

        public CampaignDocument? CampaignDocument { get; private set; }

        public decimal GrossAmount { get; private set; }

        public decimal Discounts { get; private set; }

        public decimal NetAmount { get; private set; }

        public string? Description { get; private set; }

        public PaymentMethod Method { get; private set; } = PaymentMethod.Pix;

        public PaymentStatus Status { get; private set; } = PaymentStatus.Pending;

        public string? Provider { get; private set; }

        public string? ProviderTransactionId { get; private set; }

        public string? PixKey { get; private set; }

        public PixKeyType? PixKeyType { get; private set; }

        public string? InvoiceNumber { get; private set; }

        public string? InvoiceUrl { get; private set; }

        public DateTimeOffset? InvoiceIssuedAt { get; private set; }

        public DateTimeOffset? ScheduledFor { get; private set; }

        public DateTimeOffset? PaidAt { get; private set; }

        public DateTimeOffset? FailedAt { get; private set; }

        public string? FailureReason { get; private set; }

        public IReadOnlyCollection<CreatorPaymentEvent> Events => events.AsReadOnly();

        private CreatorPayment()
        {
        }

        public CreatorPayment(long campaignCreatorId, long creatorId, decimal grossAmount, decimal discounts, PaymentMethod method, string? description = null, long? campaignDocumentId = null)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(campaignCreatorId);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(creatorId);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(grossAmount);
            ArgumentOutOfRangeException.ThrowIfNegative(discounts);

            if (discounts > grossAmount)
            {
                throw new ArgumentException("Discounts cannot exceed gross amount.", nameof(discounts));
            }

            CampaignCreatorId = campaignCreatorId;
            CreatorId = creatorId;
            CampaignDocumentId = campaignDocumentId;
            GrossAmount = grossAmount;
            Discounts = discounts;
            NetAmount = grossAmount - discounts;
            Method = method;
            Description = Normalize(description);
        }

        public void Update(decimal grossAmount, decimal discounts, PaymentMethod method, string? description)
        {
            EnsureMutable();

            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(grossAmount);
            ArgumentOutOfRangeException.ThrowIfNegative(discounts);

            if (discounts > grossAmount)
            {
                throw new ArgumentException("Discounts cannot exceed gross amount.", nameof(discounts));
            }

            GrossAmount = grossAmount;
            Discounts = discounts;
            NetAmount = grossAmount - discounts;
            Method = method;
            Description = Normalize(description);
        }

        public void SnapshotPixDestination(string pixKey, PixKeyType pixKeyType)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(pixKey);
            PixKey = pixKey.Trim();
            PixKeyType = pixKeyType;
        }

        public void AttachInvoice(string? invoiceNumber, string? invoiceUrl, DateTimeOffset? issuedAt)
        {
            InvoiceNumber = Normalize(invoiceNumber);
            InvoiceUrl = Normalize(invoiceUrl);
            InvoiceIssuedAt = issuedAt?.ToUniversalTime();
        }

        public void AttachToProvider(string provider, string providerTransactionId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(provider);
            ArgumentException.ThrowIfNullOrWhiteSpace(providerTransactionId);

            Provider = provider.Trim();
            ProviderTransactionId = providerTransactionId.Trim();
        }

        public void Schedule(DateTimeOffset scheduledFor)
        {
            EnsureMutable();

            ScheduledFor = scheduledFor.ToUniversalTime();
            Status = PaymentStatus.Scheduled;
        }

        public void MarkPaid(DateTimeOffset paidAt)
        {
            PaidAt = paidAt.ToUniversalTime();
            Status = PaymentStatus.Paid;
            FailureReason = null;
            FailedAt = null;
        }

        public void MarkFailed(string? reason, DateTimeOffset? failedAt = null)
        {
            FailureReason = Normalize(reason);
            FailedAt = (failedAt ?? DateTimeOffset.UtcNow).ToUniversalTime();
            Status = PaymentStatus.Failed;
        }

        public void Cancel(string? reason = null)
        {
            EnsureNotFinalized();
            FailureReason = Normalize(reason);
            Status = PaymentStatus.Cancelled;
        }

        public CreatorPaymentEvent RegisterEvent(CreatorPaymentEventType eventType, string? description = null, string? metadata = null, DateTimeOffset? occurredAt = null)
        {
            CreatorPaymentEvent evt = new(Id, eventType, description, metadata, occurredAt);
            events.Add(evt);
            return evt;
        }

        private void EnsureMutable()
        {
            if (Status == PaymentStatus.Paid || Status == PaymentStatus.Cancelled)
            {
                throw new InvalidOperationException("Payment is already finalized and cannot be modified.");
            }
        }

        private void EnsureNotFinalized()
        {
            if (Status == PaymentStatus.Paid)
            {
                throw new InvalidOperationException("Cannot cancel a payment already marked as Paid.");
            }
        }

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
