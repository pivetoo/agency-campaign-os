namespace AgencyCampaign.Application.Services
{
    public interface IContentFileStorage
    {
        Task<ContentFileResult> SaveAsync(long deliverableId, Stream content, string fileName, string contentType, CancellationToken cancellationToken = default);
        void RemoveByVersion(long deliverableId, IEnumerable<string> urls);
    }

    public sealed record ContentFileResult(string Url, string FileName, string ContentType);
}
