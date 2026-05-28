using AgencyCampaign.Domain.ValueObjects;

namespace AgencyCampaign.Application.Requests.ContentLicenses
{
    public sealed record AddContentLicenseRequest(ContentLicenseType Type, string? Channels, DateTimeOffset? StartsAt, DateTimeOffset? ExpiresAt, decimal? Value, string? Notes, long? CampaignDocumentId);

    public sealed record UpdateContentLicenseRequest(long Id, ContentLicenseType Type, string? Channels, DateTimeOffset? StartsAt, DateTimeOffset? ExpiresAt, decimal? Value, string? Notes, long? CampaignDocumentId);
}
