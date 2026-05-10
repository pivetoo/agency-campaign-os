namespace AgencyCampaign.Application.Services
{
    public interface IBrandLogoStorage
    {
        Task<string> SaveAsync(long brandId, Stream content, string contentType, CancellationToken cancellationToken = default);

        Task RemoveAsync(long brandId, string? currentLogoUrl, CancellationToken cancellationToken = default);
    }
}
