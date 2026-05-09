using AgencyCampaign.Domain.ValueObjects;
using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class CampaignDocumentSignature : Entity
    {
        public long CampaignDocumentId { get; private set; }

        public CampaignDocument? CampaignDocument { get; private set; }

        public CampaignDocumentSignerRole Role { get; private set; }

        public string SignerName { get; private set; } = string.Empty;

        public string SignerEmail { get; private set; } = string.Empty;

        public string? SignerDocumentNumber { get; private set; }

        public string? ProviderSignerId { get; private set; }

        public DateTimeOffset? SignedAt { get; private set; }

        public string? IpAddress { get; private set; }

        public string? UserAgent { get; private set; }

        private CampaignDocumentSignature()
        {
        }

        public CampaignDocumentSignature(long campaignDocumentId, CampaignDocumentSignerRole role, string signerName, string signerEmail, string? signerDocumentNumber, string? providerSignerId)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(campaignDocumentId);
            ArgumentException.ThrowIfNullOrWhiteSpace(signerName);
            ArgumentException.ThrowIfNullOrWhiteSpace(signerEmail);

            CampaignDocumentId = campaignDocumentId;
            Role = role;
            SignerName = signerName.Trim();
            SignerEmail = signerEmail.Trim();
            SignerDocumentNumber = Normalize(signerDocumentNumber);
            ProviderSignerId = Normalize(providerSignerId);
        }

        public void AssignProviderSignerId(string providerSignerId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(providerSignerId);
            ProviderSignerId = providerSignerId.Trim();
        }

        public void MarkSigned(DateTimeOffset signedAt, string? ipAddress = null, string? userAgent = null)
        {
            if (SignedAt.HasValue)
            {
                return;
            }

            SignedAt = signedAt.ToUniversalTime();
            IpAddress = Normalize(ipAddress);
            UserAgent = Normalize(userAgent);
        }

        public bool IsSigned => SignedAt.HasValue;

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
