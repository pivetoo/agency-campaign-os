using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;

namespace AgencyCampaign.Api.Contracts.CampaignDocuments
{
    public sealed class CampaignDocumentSignatureContract
    {
        public long Id { get; init; }
        public CampaignDocumentSignerRole Role { get; init; }
        public string SignerName { get; init; } = string.Empty;
        public string SignerEmail { get; init; } = string.Empty;
        public string? SignerDocumentNumber { get; init; }
        public string? ProviderSignerId { get; init; }
        public DateTimeOffset? SignedAt { get; init; }
        public string? IpAddress { get; init; }
        public bool IsSigned { get; init; }

        public static CampaignDocumentSignatureContract FromEntity(CampaignDocumentSignature signature) => new()
        {
            Id = signature.Id,
            Role = signature.Role,
            SignerName = signature.SignerName,
            SignerEmail = signature.SignerEmail,
            SignerDocumentNumber = signature.SignerDocumentNumber,
            ProviderSignerId = signature.ProviderSignerId,
            SignedAt = signature.SignedAt,
            IpAddress = signature.IpAddress,
            IsSigned = signature.IsSigned,
        };
    }
}
