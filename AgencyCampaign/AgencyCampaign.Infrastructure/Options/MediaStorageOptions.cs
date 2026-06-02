namespace AgencyCampaign.Infrastructure.Options
{
    public sealed class MediaStorageOptions
    {
        // Segredo HMAC para assinar as URLs de midia privada. Em producao DEVE ser definido
        // (appsettings.Production.json no VPS); vazio = assinatura recusada (fail-closed).
        public string SigningKey { get; set; } = string.Empty;

        // Raiz de armazenamento da midia privada. Vazio = ContentRootPath/private-uploads
        // (fora de wwwroot, portanto NAO servida estaticamente).
        public string PrivateRootPath { get; set; } = string.Empty;

        // Tempo de vida padrao da URL assinada de midia.
        public int SignedUrlMinutes { get; set; } = 120;

        // Limite de tamanho de upload de midia (bytes). Default 25 MB.
        public long MaxUploadBytes { get; set; } = 26_214_400;
    }
}
