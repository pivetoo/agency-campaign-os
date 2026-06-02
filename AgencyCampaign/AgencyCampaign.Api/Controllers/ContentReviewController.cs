using AgencyCampaign.Application.Requests.ContentReview;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.ValueObjects;
using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace AgencyCampaign.Api.Controllers
{
    [AccessArea("campaigns.area")]
    public sealed class ContentReviewController : ApiControllerBase
    {
        private const long MaxAssetBytes = 10 * 1024 * 1024;

        private readonly IContentReviewService service;
        private readonly IContentFileStorage fileStorage;
        private readonly IMediaAccessTokenService mediaTokens;

        public ContentReviewController(IContentReviewService service, IContentFileStorage fileStorage, IMediaAccessTokenService mediaTokens)
        {
            this.service = service;
            this.fileStorage = fileStorage;
            this.mediaTokens = mediaTokens;
        }

        [RequireAccess("campaigns.getById.description")]
        [GetEndpoint("deliverable/{deliverableId:long}")]
        public async Task<IActionResult> Get(long deliverableId, CancellationToken cancellationToken)
        {
            return Http200(await service.GetByDeliverable(deliverableId, cancellationToken));
        }

        [RequireAccess("campaigns.update.description")]
        [PostEndpoint("upload/{deliverableId:long}")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(MaxAssetBytes)]
        public async Task<IActionResult> Upload(long deliverableId, [FromForm] IFormFile file, CancellationToken cancellationToken)
        {
            await using Stream stream = file.OpenReadStream();
            ContentFileResult result = await fileStorage.SaveAsync(deliverableId, stream, file.FileName, file.ContentType, cancellationToken);
            return Http200(new { storageKey = result.StorageKey, previewUrl = mediaTokens.BuildSignedUrl(result.StorageKey), result.FileName, result.ContentType });
        }

        [RequireAccess("campaigns.update.description")]
        [PostEndpoint("deliverable/{deliverableId:long}/version")]
        public async Task<IActionResult> AddVersion(long deliverableId, [FromBody] AddContentVersionRequest request, CancellationToken cancellationToken)
        {
            return Http200(await service.AddVersion(deliverableId, ReviewParticipant.Agency, CurrentUserName ?? "Agencia", request, cancellationToken));
        }

        [RequireAccess("campaigns.update.description")]
        [PostEndpoint("version/{versionId:long}/request-changes")]
        public async Task<IActionResult> RequestChanges(long versionId, [FromBody] AddReviewCommentRequest request, CancellationToken cancellationToken)
        {
            return Http200(await service.RequestChanges(versionId, CurrentUserName ?? "Agencia", request.Body, cancellationToken));
        }

        [RequireAccess("campaigns.update.description")]
        [PostEndpoint("version/{versionId:long}/send-to-brand")]
        public async Task<IActionResult> SendToBrand(long versionId, CancellationToken cancellationToken)
        {
            return Http200(await service.SendToBrand(versionId, cancellationToken));
        }

        [RequireAccess("campaigns.update.description")]
        [PostEndpoint("version/{versionId:long}/agency-approve")]
        public async Task<IActionResult> AgencyApprove(long versionId, CancellationToken cancellationToken)
        {
            return Http200(await service.AgencyApprove(versionId, cancellationToken));
        }

        [RequireAccess("campaigns.update.description")]
        [PostEndpoint("deliverable/{deliverableId:long}/comment")]
        public async Task<IActionResult> AddComment(long deliverableId, [FromBody] AddReviewCommentRequest request, CancellationToken cancellationToken)
        {
            return Http200(await service.AddComment(deliverableId, ReviewParticipant.Agency, CurrentUserName ?? "Agencia", request, cancellationToken));
        }
    }
}
