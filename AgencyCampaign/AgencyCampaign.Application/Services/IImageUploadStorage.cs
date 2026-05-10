namespace AgencyCampaign.Application.Services
{
    public interface IImageUploadStorage
    {
        Task<string> SaveAsync(string section, long entityId, Stream content, string contentType, CancellationToken cancellationToken = default);

        Task RemoveAsync(string section, long entityId, CancellationToken cancellationToken = default);
    }
}
