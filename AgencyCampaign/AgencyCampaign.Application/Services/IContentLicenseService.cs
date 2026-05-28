using AgencyCampaign.Application.Requests.ContentLicenses;
using AgencyCampaign.Domain.ValueObjects;

namespace AgencyCampaign.Application.Services
{
    public interface IContentLicenseService
    {
        Task<IReadOnlyList<ContentLicenseModel>> GetByDeliverable(long deliverableId, CancellationToken cancellationToken = default);
        Task<ContentLicenseModel> Add(long deliverableId, AddContentLicenseRequest request, CancellationToken cancellationToken = default);
        Task<ContentLicenseModel> Update(long licenseId, UpdateContentLicenseRequest request, CancellationToken cancellationToken = default);
        Task Delete(long licenseId, CancellationToken cancellationToken = default);
        Task<int> ApplyToCampaign(long licenseId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ContentLicenseModel>> GetExpiring(int withinDays, CancellationToken cancellationToken = default);
        Task<int> AlertExpiring(IReadOnlyList<int> thresholdsDays, CancellationToken cancellationToken = default);
    }

    public sealed class ContentLicenseModel
    {
        public long Id { get; init; }
        public long DeliverableId { get; init; }
        public ContentLicenseType Type { get; init; }
        public string? Channels { get; init; }
        public DateTimeOffset? StartsAt { get; init; }
        public DateTimeOffset? ExpiresAt { get; init; }
        public decimal? Value { get; init; }
        public string? Notes { get; init; }
        public long? CampaignDocumentId { get; init; }
        public ContentLicenseStatus Status { get; init; }
        public int? DaysUntilExpiry { get; init; }
    }
}
