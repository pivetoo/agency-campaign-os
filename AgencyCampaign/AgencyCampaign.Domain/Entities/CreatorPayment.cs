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

        // Imposto retido na fonte ao pagar o creator (IRRF/INSS/ISS conforme regime). Por ora informado
        // pelo operador (calculo automatico = fase 2); entra no liquido e no relatorio de retencoes.
        public decimal TaxWithheld { get; private set; }

        public decimal NetAmount { get; private set; }

        public string? Description { get; private set; }

        public PaymentMethod Method { get; private set; } = PaymentMethod.Pix;

        public PaymentStatus Status { get; private set; } = PaymentStatus.Pending;

        public string? Provider { get; private set; }

        public string? ProviderTransactionId { get; private set; }

        public string? EndToEndId { get; private set; }

        public string? IdempotencyKey { get; private set; }

        public string? PixKey { get; private set; }

        public PixKeyType? PixKeyType { get; private set; }

        public string? InvoiceNumber { get; private set; }

        public string? InvoiceUrl { get; private set; }

        public DateTimeOffset? InvoiceIssuedAt { get; private set; }

        public DateTimeOffset? ScheduledFor { get; private set; }

        public DateTimeOffset? PaidAt { get; private set; }

        public DateTimeOffset? FailedAt { get; private set; }

        public string? FailureReason { get; private set; }

        // Maker-checker: quem criou e quem aprovou o pagamento. O aprovador precisa ser diferente do criador
        // (segregacao de funcoes); acima do teto da agencia, o repasse so e agendado apos a aprovacao.
        public long? CreatedByUserId { get; private set; }

        public DateTimeOffset? ApprovedAt { get; private set; }

        public long? ApprovedByUserId { get; private set; }

        public bool IsApproved => ApprovedAt.HasValue;

        public IReadOnlyCollection<CreatorPaymentEvent> Events => events.AsReadOnly();

        private CreatorPayment()
        {
        }

        public CreatorPayment(long campaignCreatorId, long creatorId, decimal grossAmount, decimal discounts, PaymentMethod method, string? description = null, long? campaignDocumentId = null, decimal taxWithheld = 0)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(campaignCreatorId);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(creatorId);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(grossAmount);
            ArgumentOutOfRangeException.ThrowIfNegative(discounts);
            ArgumentOutOfRangeException.ThrowIfNegative(taxWithheld);

            if (discounts + taxWithheld > grossAmount)
            {
                throw new ArgumentException("Discounts and tax withheld cannot exceed gross amount.", nameof(discounts));
            }

            CampaignCreatorId = campaignCreatorId;
            CreatorId = creatorId;
            CampaignDocumentId = campaignDocumentId;
            GrossAmount = grossAmount;
            Discounts = discounts;
            TaxWithheld = taxWithheld;
            NetAmount = grossAmount - discounts - taxWithheld;
            Method = method;
            Description = Normalize(description);
        }

        public void Update(decimal grossAmount, decimal discounts, PaymentMethod method, string? description, decimal taxWithheld = 0)
        {
            EnsureMutable();

            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(grossAmount);
            ArgumentOutOfRangeException.ThrowIfNegative(discounts);
            ArgumentOutOfRangeException.ThrowIfNegative(taxWithheld);

            if (discounts + taxWithheld > grossAmount)
            {
                throw new ArgumentException("Discounts and tax withheld cannot exceed gross amount.", nameof(discounts));
            }

            GrossAmount = grossAmount;
            Discounts = discounts;
            TaxWithheld = taxWithheld;
            NetAmount = grossAmount - discounts - taxWithheld;
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

        // EndToEndId (e2eId) do Pix: identificador imutavel atribuido pelo Banco Central, usado para
        // conciliar o repasse contra o extrato e comprovar o pagamento ao creator.
        public void AttachEndToEndId(string endToEndId)
        {
            EndToEndId = Normalize(endToEndId);
        }

        // Chave de idempotencia do payout: gerada no agendamento e enviada ao IntegrationPlatform para
        // que um retry de rede/reprocessamento nao dispare o Pix duas vezes.
        public void AssignIdempotencyKey(string idempotencyKey)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
            IdempotencyKey = idempotencyKey.Trim();
        }

        public void Schedule(DateTimeOffset scheduledFor)
        {
            EnsureMutable();

            ScheduledFor = scheduledFor.ToUniversalTime();
            Status = PaymentStatus.Scheduled;
        }

        public void SetCreatedBy(long? userId)
        {
            CreatedByUserId = userId;
        }

        // Aprovacao (maker-checker): o aprovador deve ser diferente de quem criou; nao reaprova nem aprova
        // pagamento ja finalizado.
        public void Approve(long approverUserId)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(approverUserId);

            if (Status == PaymentStatus.Paid || Status == PaymentStatus.Cancelled)
            {
                throw new InvalidOperationException("creatorPayment.alreadyFinalized");
            }

            if (ApprovedAt.HasValue)
            {
                throw new InvalidOperationException("creatorPayment.alreadyApproved");
            }

            if (CreatedByUserId.HasValue && CreatedByUserId.Value == approverUserId)
            {
                throw new InvalidOperationException("creatorPayment.approverMustDiffer");
            }

            ApprovedAt = DateTimeOffset.UtcNow;
            ApprovedByUserId = approverUserId;
        }

        public void MarkPaid(DateTimeOffset paidAt)
        {
            if (Status == PaymentStatus.Cancelled)
            {
                throw new InvalidOperationException("creatorPayment.cannotPayCancelled");
            }

            PaidAt = paidAt.ToUniversalTime();
            Status = PaymentStatus.Paid;
            FailureReason = null;
            FailedAt = null;
        }

        public void MarkFailed(string? reason, DateTimeOffset? failedAt = null)
        {
            if (Status == PaymentStatus.Paid || Status == PaymentStatus.Cancelled)
            {
                throw new InvalidOperationException("creatorPayment.cannotFailFinalized");
            }

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
                throw new InvalidOperationException("creatorPayment.alreadyFinalized");
            }
        }

        private void EnsureNotFinalized()
        {
            if (Status == PaymentStatus.Paid)
            {
                throw new InvalidOperationException("creatorPayment.cannotCancelPaid");
            }
        }

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
