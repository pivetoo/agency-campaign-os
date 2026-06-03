namespace AgencyCampaign.Application.Services
{
    // Baixa (best-effort) o PDF assinado a partir da URL do provedor para guardar uma copia
    // propria (lastro/durabilidade, D1i). Retorna null se a URL nao for elegivel (nao-https) ou
    // se o download falhar/exceder o limite - nunca lanca (a assinatura ja ocorreu; a copia e secundaria).
    public interface ISignedDocumentDownloader
    {
        Task<byte[]?> DownloadAsync(string url, CancellationToken cancellationToken = default);
    }
}
