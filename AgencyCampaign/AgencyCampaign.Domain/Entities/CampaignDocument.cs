using AgencyCampaign.Domain.ValueObjects;
using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class CampaignDocument : Entity
    {
        public long CampaignId { get; private set; }

        public Campaign? Campaign { get; private set; }

        public long? CampaignCreatorId { get; private set; }

        public CampaignCreator? CampaignCreator { get; private set; }

        public CampaignDocumentType DocumentType { get; private set; }

        public string Title { get; private set; } = string.Empty;

        public string? DocumentUrl { get; private set; }

        public CampaignDocumentStatus Status { get; private set; } = CampaignDocumentStatus.Draft;

        public string? RecipientEmail { get; private set; }

        public string? EmailSubject { get; private set; }

        public string? EmailBody { get; private set; }

        public DateTimeOffset? SentAt { get; private set; }

        public DateTimeOffset? SignedAt { get; private set; }

        public string? Notes { get; private set; }

        private CampaignDocument()
        {
        }

        public CampaignDocument(long campaignId, CampaignDocumentType documentType, string title, string? documentUrl = null, string? notes = null, long? campaignCreatorId = null)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(campaignId);
            ArgumentException.ThrowIfNullOrWhiteSpace(title);

            CampaignId = campaignId;
            CampaignCreatorId = campaignCreatorId;
            DocumentType = documentType;
            Title = title.Trim();
            DocumentUrl = Normalize(documentUrl);
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

        public void MarkSigned(DateTimeOffset signedAt)
        {
            SignedAt = signedAt.ToUniversalTime();
            Status = CampaignDocumentStatus.Signed;
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

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
