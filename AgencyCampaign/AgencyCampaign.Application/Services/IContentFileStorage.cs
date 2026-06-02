namespace AgencyCampaign.Application.Services
{
    public interface IContentFileStorage
    {
        Task<ContentFileResult> SaveAsync(long deliverableId, Stream content, string fileName, string contentType, CancellationToken cancellationToken = default);
        void RemoveByVersion(long deliverableId, IEnumerable<string> urls);
    }

    // StorageKey: chave duravel de armazenamento privado (ex.: "content/tenant-1/10/abc.png"),
    // sem barra inicial. NAO e uma URL publica - e exibida via URL assinada (/api/media?t=...).
    public sealed record ContentFileResult(string StorageKey, string FileName, string ContentType);
}
