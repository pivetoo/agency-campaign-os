using AgencyCampaign.Domain.ValueObjects;
using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class CampaignDocument : Entity
    {
        private readonly List<CampaignDocumentSignature> signatures = [];
        private readonly List<CampaignDocumentEvent> events = [];

        public long CampaignId { get; private set; }

        public Campaign? Campaign { get; private set; }

        public long? CampaignCreatorId { get; private set; }

        public CampaignCreator? CampaignCreator { get; private set; }

        public long? TemplateId { get; private set; }

        public CampaignDocumentTemplate? Template { get; private set; }

        public CampaignDocumentType DocumentType { get; private set; }

        public string Title { get; private set; } = string.Empty;

        public string? DocumentUrl { get; private set; }

        public string? Body { get; private set; }

        public string? Provider { get; private set; }

        public string? ProviderDocumentId { get; private set; }

        public string? SignedDocumentUrl { get; private set; }

        public CampaignDocumentStatus Status { get; private set; } = CampaignDocumentStatus.Draft;

        public string? RecipientEmail { get; private set; }

        public string? EmailSubject { get; private set; }

        public string? EmailBody { get; private set; }

        public DateTimeOffset? SentAt { get; private set; }

        public DateTimeOffset? SignedAt { get; private set; }

        public string? Notes { get; private set; }

        public IReadOnlyCollection<CampaignDocumentSignature> Signatures => signatures.AsReadOnly();

        public IReadOnlyCollection<CampaignDocumentEvent> Events => events.AsReadOnly();

        private CampaignDocument()
        {
        }

        public CampaignDocument(long campaignId, CampaignDocumentType documentType, string title, string? documentUrl = null, string? notes = null, long? campaignCreatorId = null, long? templateId = null, string? body = null)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(campaignId);
            ArgumentException.ThrowIfNullOrWhiteSpace(title);

            CampaignId = campaignId;
            CampaignCreatorId = campaignCreatorId;
            TemplateId = templateId;
            DocumentType = documentType;
            Title = title.Trim();
            DocumentUrl = Normalize(documentUrl);
            Body = string.IsNullOrWhiteSpace(body) ? null : body;
            Notes = Normalize(notes);
        }

        public void Update(CampaignDocumentType documentType, string title, string? documentUrl, string? notes, long? campaignCreatorId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(title);

            DocumentType = documentType;
            Title = title.Trim();
            DocumentUrl = Normalize(documentUrl);
            Notes = Normalize(notes);
            CampaignCreatorId = campaignCreatorId;
        }

        public void UpdateBody(string body)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(body);
            Body = body;
        }

        public void MarkReadyToSend()
        {
            Status = CampaignDocumentStatus.ReadyToSend;
        }

        public void MarkSent(string recipientEmail, string emailSubject, string? emailBody, DateTimeOffset sentAt)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(recipientEmail);
            ArgumentException.ThrowIfNullOrWhiteSpace(emailSubject);

            RecipientEmail = recipientEmail.Trim();
            EmailSubject = emailSubject.Trim();
            EmailBody = Normalize(emailBody);
            SentAt = sentAt.ToUniversalTime();
            Status = CampaignDocumentStatus.Sent;
        }

        public void AttachToProvider(string provider, string providerDocumentId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(provider);
            ArgumentException.ThrowIfNullOrWhiteSpace(providerDocumentId);

            Provider = provider.Trim();
            ProviderDocumentId = providerDocumentId.Trim();

            if (Status == CampaignDocumentStatus.Draft || Status == CampaignDocumentStatus.ReadyToSend)
            {
                Status = CampaignDocumentStatus.Sent;
                SentAt ??= DateTimeOffset.UtcNow;
            }
        }

        public CampaignDocumentSignature AddSignature(CampaignDocumentSignerRole role, string signerName, string signerEmail, string? signerDocumentNumber = null, string? providerSignerId = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(signerName);
            ArgumentException.ThrowIfNullOrWhiteSpace(signerEmail);

            CampaignDocumentSignature signature = new(Id, role, signerName, signerEmail, signerDocumentNumber, providerSignerId);
            signatures.Add(signature);
            return signature;
        }

        public CampaignDocumentSignature? RegisterSignerSigned(string signerEmail, DateTimeOffset signedAt, string? ipAddress = null, string? userAgent = null, string? providerSignerId = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(signerEmail);

            CampaignDocumentSignature? signature = signatures.FirstOrDefault(item =>
                string.Equals(item.SignerEmail, signerEmail.Trim(), StringComparison.OrdinalIgnoreCase));

            if (signature is null && !string.IsNullOrWhiteSpace(providerSignerId))
            {
                signature = signatures.FirstOrDefault(item => item.ProviderSignerId == providerSignerId);
            }

            signature?.MarkSigned(signedAt, ipAddress, userAgent);
            return signature;
        }

        public void MarkViewed(DateTimeOffset viewedAt)
        {
            if (Status == CampaignDocumentStatus.Sent)
            {
                Status = CampaignDocumentStatus.Viewed;
            }
        }

        public void MarkSigned(DateTimeOffset signedAt, string? signedDocumentUrl = null)
        {
            SignedAt = signedAt.ToUniversalTime();
            Status = CampaignDocumentStatus.Signed;

            if (!string.IsNullOrWhiteSpace(signedDocumentUrl))
            {
                SignedDocumentUrl = signedDocumentUrl.Trim();
            }
        }

        public void MarkRejected(string? notes = null)
        {
            Notes = Normalize(notes);
            Status = CampaignDocumentStatus.Rejected;
        }

        public void MarkCancelled(string? notes = null)
        {
            Notes = Normalize(notes);
            Status = CampaignDocumentStatus.Cancelled;
        }

        public CampaignDocumentEvent RegisterEvent(CampaignDocumentEventType eventType, string? description = null, string? metadata = null, DateTimeOffset? occurredAt = null)
        {
            CampaignDocumentEvent evt = new(Id, eventType, description, metadata, occurredAt);
            events.Add(evt);
            return evt;
        }

        public bool AllSigned()
        {
            return signatures.Count > 0 && signatures.All(item => item.IsSigned);
        }

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
