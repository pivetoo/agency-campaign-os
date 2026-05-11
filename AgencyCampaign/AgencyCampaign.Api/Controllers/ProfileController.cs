using AgencyCampaign.Application.Services;
using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace AgencyCampaign.Api.Controllers
{
    public sealed class ProfileController : ApiControllerBase
    {
        private const long MaxAvatarBytes = 2 * 1024 * 1024;

        private readonly IImageUploadStorage imageStorage;

        public ProfileController(IImageUploadStorage imageStorage)
        {
            this.imageStorage = imageStorage;
        }

        [RequireAccess("Permite enviar foto de perfil do usuário.")]
        [PostEndpoint("[action]")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(MaxAvatarBytes)]
        public async Task<IActionResult> UploadAvatar([FromForm] IFormFile file, CancellationToken cancellationToken)
        {
            if (file is null || file.Length == 0)
            {
                return Http400("Arquivo não informado.");
            }

            if (file.Length > MaxAvatarBytes)
            {
                return Http400("Arquivo excede o limite de 2MB.");
            }

            if (CurrentUserId is null)
            {
                return Http401();
            }

            await using Stream stream = file.OpenReadStream();
            string url = await imageStorage.SaveAsync("profiles", CurrentUserId.Value, stream, file.ContentType, cancellationToken);

            return Http200(new { url });
        }
    }
}
