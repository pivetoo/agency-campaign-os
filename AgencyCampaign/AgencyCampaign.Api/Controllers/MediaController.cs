using AgencyCampaign.Application.Services;
using AgencyCampaign.Infrastructure.Options;
using Archon.Api.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;

namespace AgencyCampaign.Api.Controllers
{
    // Serve midia privada por URL assinada (/api/media?t=...). O token HMAC e a autorizacao -
    // permite exibir via <img src>/video/PDF sem header Authorization. Le de um diretorio privado
    // (fora de wwwroot), portanto NAO ha exposicao estatica desses arquivos.
    [AllowAnonymous]
    [Route("api/media")]
    public sealed class MediaController : ApiControllerBase
    {
        private static readonly FileExtensionContentTypeProvider ContentTypeProvider = new();

        private readonly IMediaAccessTokenService tokenService;
        private readonly MediaStorageOptions options;
        private readonly IWebHostEnvironment environment;

        public MediaController(IMediaAccessTokenService tokenService, IOptions<MediaStorageOptions> options, IWebHostEnvironment environment)
        {
            this.tokenService = tokenService;
            this.options = options.Value;
            this.environment = environment;
        }

        [HttpGet]
        public IActionResult Get([FromQuery] string? t)
        {
            if (string.IsNullOrWhiteSpace(t) || !tokenService.TryReadStorageKey(t, out string storageKey))
            {
                return Http403("Acesso de midia invalido ou expirado.");
            }

            string root = Path.GetFullPath(string.IsNullOrWhiteSpace(options.PrivateRootPath)
                ? Path.Combine(environment.ContentRootPath, "private-uploads")
                : options.PrivateRootPath);

            string fullPath = Path.GetFullPath(Path.Combine(root, storageKey.Replace('/', Path.DirectorySeparatorChar)));

            // Defesa contra path traversal: o caminho resolvido tem que ficar dentro da raiz privada.
            if (!fullPath.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal) || !System.IO.File.Exists(fullPath))
            {
                return Http404("Midia nao encontrada.");
            }

            if (!ContentTypeProvider.TryGetContentType(fullPath, out string? contentType))
            {
                contentType = "application/octet-stream";
            }

            FileStream stream = System.IO.File.OpenRead(fullPath);
            return File(stream, contentType, enableRangeProcessing: true);
        }
    }
}
