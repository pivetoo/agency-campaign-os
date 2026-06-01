using AgencyCampaign.Application.Services;
using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Infrastructure.IdentityManagement;
using Microsoft.AspNetCore.Mvc;

namespace AgencyCampaign.Api.Controllers
{
    [AccessArea("profile.area")]
    public sealed class ProfileController : ApiControllerBase
    {
        private const long MaxAvatarBytes = 2 * 1024 * 1024;

        private readonly IImageUploadStorage imageStorage;
        private readonly IdentityUsersClient identityUsersClient;

        public ProfileController(IImageUploadStorage imageStorage, IdentityUsersClient identityUsersClient)
        {
            this.imageStorage = imageStorage;
            this.identityUsersClient = identityUsersClient;
        }

        [RequireAccess("profile.uploadAvatar.description")]
        [PostEndpoint]
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

            await identityUsersClient.UpdateUserAvatarAsync(CurrentUserId.Value, url, cancellationToken);

            return Http200(new { url });
        }

        [RequireAccess("profile.removeAvatar.description")]
        [DeleteEndpoint]
        public async Task<IActionResult> RemoveAvatar(CancellationToken cancellationToken)
        {
            if (CurrentUserId is null)
            {
                return Http401();
            }

            await imageStorage.RemoveAsync("profiles", CurrentUserId.Value, cancellationToken);
            await identityUsersClient.DeleteUserAvatarAsync(CurrentUserId.Value, cancellationToken);

            return Http200(new { });
        }
    }
}
