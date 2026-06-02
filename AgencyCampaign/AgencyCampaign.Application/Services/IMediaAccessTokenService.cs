namespace AgencyCampaign.Application.Services
{
    // Assina/valida o acesso a um arquivo de midia privada. O token (HMAC + expiracao) e a
    // autorizacao: vai na query da URL (/api/media?t=...), permitindo exibir via <img src> sem
    // header Authorization. Curta duracao = revogacao implicita.
    public interface IMediaAccessTokenService
    {
        // Cria a URL relativa assinada (/api/media?t=...) para uma chave de armazenamento.
        string BuildSignedUrl(string storageKey, TimeSpan? lifetime = null);

        // Valida o token e devolve a chave de armazenamento; false se invalido/expirado/adulterado.
        bool TryReadStorageKey(string token, out string storageKey);
    }
}
