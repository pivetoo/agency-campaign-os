using AgencyCampaign.Api.Contracts.CampaignDeliverables;
using AgencyCampaign.Api.Contracts.CampaignDocuments;
using AgencyCampaign.Api.Contracts.CreatorPayments;
using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.ContentReview;
using AgencyCampaign.Application.Requests.CreatorPortal;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Archon.Api.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Api.Controllers
{
    [AllowAnonymous]
    [Route("api/creatorportal")]
    public sealed class CreatorPortalController : ApiControllerBase
    {
        private const long MaxUploadBytes = 10 * 1024 * 1024;

        private readonly ICreatorPortalService portalService;
        private readonly IContentFileStorage fileStorage;
        private readonly IMediaAccessTokenService mediaTokens;
        private new IStringLocalizer<AgencyCampaignResource> Localizer { get; }
        private static readonly Func<CampaignDocument, CampaignDocumentContract> MapDocument = CampaignDocumentContract.Projection.Compile();
        private static readonly Func<CreatorPayment, CreatorPaymentContract> MapPaymentRaw = CreatorPaymentContract.Projection.Compile();
        private static readonly Func<CampaignDeliverable, CampaignDeliverableContract> MapDeliverable = CampaignDeliverableContract.Projection.Compile();

        public CreatorPortalController(ICreatorPortalService portalService, IContentFileStorage fileStorage, IMediaAccessTokenService mediaTokens, IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.portalService = portalService;
            this.fileStorage = fileStorage;
            this.mediaTokens = mediaTokens;
            Localizer = localizer;
        }

        // Assina a NF (InvoiceUrl) para exibicao quando ela e uma chave de armazenamento privada.
        private CreatorPaymentContract MapPayment(CreatorPayment payment)
        {
            return MapPaymentRaw(payment) with { InvoiceUrl = mediaTokens.ResolveDisplayUrl(payment.InvoiceUrl) };
        }

        [HttpGet("{token}/me")]
        public async Task<IActionResult> Me(string token, CancellationToken cancellationToken)
        {
            try
            {
                CreatorPortalContext ctx = await portalService.ResolveContext(token, cancellationToken);
                return Http200(new
                {
                    creator = new
                    {
                        ctx.Creator.Id,
                        ctx.Creator.Name,
                        ctx.Creator.StageName,
                        ctx.Creator.Email,
                        ctx.Creator.Phone,
                        ctx.Creator.Document,
                        ctx.Creator.PixKey,
                        ctx.Creator.PixKeyType,
                        ctx.Creator.PrimaryNiche,
                    },
                    token = new
                    {
                        ctx.Token.ExpiresAt,
                        ctx.Token.UsageCount,
                        ctx.Token.LastUsedAt,
                    },
                });
            }
            catch (InvalidOperationException ex)
            {
                return Http401(Localizer[ex.Message]);
            }
        }

        [HttpGet("{token}/campaigns")]
        public async Task<IActionResult> Campaigns(string token, CancellationToken cancellationToken)
        {
            try
            {
                CreatorPortalContext ctx = await portalService.ResolveContext(token, cancellationToken);
                List<CampaignCreator> campaigns = await portalService.GetCampaigns(ctx.Creator.Id, cancellationToken);
                return Http200(campaigns.Select(item => new
                {
                    item.Id,
                    item.CampaignId,
                    CampaignName = item.Campaign?.Name,
                    BrandName = item.Campaign?.Brand?.Name,
                    StatusName = item.CampaignCreatorStatus?.Name,
                    StatusColor = item.CampaignCreatorStatus?.Color,
                    item.AgreedAmount,
                    item.AgencyFeePercent,
                    item.Notes,
                    StartsAt = item.Campaign?.StartsAt,
                    EndsAt = item.Campaign?.EndsAt,
                }).ToList());
            }
            catch (InvalidOperationException ex)
            {
                return Http401(Localizer[ex.Message]);
            }
        }

        [HttpGet("{token}/campaigns/{campaignId:long}/briefing")]
        public async Task<IActionResult> CampaignBriefing(string token, long campaignId, CancellationToken cancellationToken)
        {
            try
            {
                CreatorPortalContext ctx = await portalService.ResolveContext(token, cancellationToken);
                return Http200(await portalService.GetCampaignBriefing(ctx.Creator.Id, campaignId, cancellationToken));
            }
            catch (InvalidOperationException ex)
            {
                return Http401(Localizer[ex.Message]);
            }
        }

        [HttpGet("{token}/documents")]
        public async Task<IActionResult> Documents(string token, CancellationToken cancellationToken)
        {
            try
            {
                CreatorPortalContext ctx = await portalService.ResolveContext(token, cancellationToken);
                List<CampaignDocument> documents = await portalService.GetDocuments(ctx.Creator.Id, cancellationToken);
                return Http200(documents.Select(MapDocument).ToList());
            }
            catch (InvalidOperationException ex)
            {
                return Http401(Localizer[ex.Message]);
            }
        }

        [HttpGet("{token}/deliverables")]
        public async Task<IActionResult> Deliverables(string token, CancellationToken cancellationToken)
        {
            try
            {
                CreatorPortalContext ctx = await portalService.ResolveContext(token, cancellationToken);
                List<CampaignDeliverable> deliverables = await portalService.GetDeliverables(ctx.Creator.Id, cancellationToken);
                return Http200(deliverables.Select(MapDeliverable).ToList());
            }
            catch (InvalidOperationException ex)
            {
                return Http401(Localizer[ex.Message]);
            }
        }

        [HttpPost("{token}/deliverables/{deliverableId:long}/insights")]
        public async Task<IActionResult> SubmitInsights(string token, long deliverableId, [FromBody] SubmitDeliverableInsightsRequest request, CancellationToken cancellationToken)
        {
            try
            {
                CreatorPortalContext ctx = await portalService.ResolveContext(token, cancellationToken);

                IActionResult? validationResult = ValidateBody(request);
                if (validationResult is not null)
                {
                    return validationResult;
                }

                CampaignDeliverable deliverable = await portalService.SubmitInsights(ctx.Creator.Id, deliverableId, request, cancellationToken);
                return Http200(MapDeliverable(deliverable), Localizer["record.updated"]);
            }
            catch (InvalidOperationException ex)
            {
                return Http401(Localizer[ex.Message]);
            }
        }

        [HttpGet("{token}/payments")]
        public async Task<IActionResult> Payments(string token, CancellationToken cancellationToken)
        {
            try
            {
                CreatorPortalContext ctx = await portalService.ResolveContext(token, cancellationToken);
                List<CreatorPayment> payments = await portalService.GetPayments(ctx.Creator.Id, cancellationToken);
                return Http200(payments.Select(MapPayment).ToList());
            }
            catch (InvalidOperationException ex)
            {
                return Http401(Localizer[ex.Message]);
            }
        }

        [HttpPost("{token}/bank-info")]
        public async Task<IActionResult> UpdateBankInfo(string token, [FromBody] UpdateCreatorBankInfoRequest request, CancellationToken cancellationToken)
        {
            try
            {
                CreatorPortalContext ctx = await portalService.ResolveContext(token, cancellationToken);

                IActionResult? validationResult = ValidateBody(request);
                if (validationResult is not null)
                {
                    return validationResult;
                }

                Creator creator = await portalService.UpdateBankInfo(ctx.Creator.Id, request, cancellationToken);
                return Http200(new
                {
                    creator.PixKey,
                    creator.PixKeyType,
                    creator.Document,
                }, Localizer["record.updated"]);
            }
            catch (InvalidOperationException ex)
            {
                return Http401(Localizer[ex.Message]);
            }
        }

        [HttpPost("{token}/invoice")]
        public async Task<IActionResult> UploadInvoice(string token, [FromBody] UploadInvoiceRequest request, CancellationToken cancellationToken)
        {
            try
            {
                CreatorPortalContext ctx = await portalService.ResolveContext(token, cancellationToken);

                IActionResult? validationResult = ValidateBody(request);
                if (validationResult is not null)
                {
                    return validationResult;
                }

                CreatorPayment payment = await portalService.UploadInvoice(ctx.Creator.Id, request, cancellationToken);
                return Http200(MapPayment(payment), Localizer["record.updated"]);
            }
            catch (InvalidOperationException ex)
            {
                return Http401(Localizer[ex.Message]);
            }
        }

        [HttpPost("{token}/invoice/upload")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(MaxUploadBytes)]
        public async Task<IActionResult> UploadInvoiceFile(string token, [FromForm] long creatorPaymentId, [FromForm] string? invoiceNumber, [FromForm] DateTimeOffset? issuedAt, [FromForm] IFormFile file, CancellationToken cancellationToken)
        {
            try
            {
                CreatorPortalContext ctx = await portalService.ResolveContext(token, cancellationToken);

                await using Stream stream = file.OpenReadStream();
                ContentFileResult stored = await fileStorage.SaveAsync(creatorPaymentId, stream, file.FileName, file.ContentType, cancellationToken);

                UploadInvoiceRequest request = new()
                {
                    CreatorPaymentId = creatorPaymentId,
                    InvoiceNumber = invoiceNumber,
                    InvoiceUrl = stored.StorageKey,
                    IssuedAt = issuedAt,
                };

                CreatorPayment payment = await portalService.UploadInvoice(ctx.Creator.Id, request, cancellationToken);
                return Http200(MapPayment(payment), Localizer["record.updated"]);
            }
            catch (InvalidOperationException ex)
            {
                return Http401(Localizer[ex.Message]);
            }
        }

        [HttpGet("{token}/deliverables/{deliverableId:long}/review")]
        public async Task<IActionResult> GetDeliverableReview(string token, long deliverableId, CancellationToken cancellationToken)
        {
            try
            {
                CreatorPortalContext ctx = await portalService.ResolveContext(token, cancellationToken);
                return Http200(await portalService.GetDeliverableReview(ctx.Creator.Id, deliverableId, cancellationToken));
            }
            catch (InvalidOperationException ex)
            {
                return Http401(Localizer[ex.Message]);
            }
        }

        [HttpPost("{token}/deliverables/{deliverableId:long}/version")]
        public async Task<IActionResult> SubmitVersion(string token, long deliverableId, [FromBody] AddContentVersionRequest request, CancellationToken cancellationToken)
        {
            try
            {
                CreatorPortalContext ctx = await portalService.ResolveContext(token, cancellationToken);
                return Http200(await portalService.SubmitContentVersion(ctx.Creator.Id, deliverableId, request, cancellationToken));
            }
            catch (InvalidOperationException ex)
            {
                return Http401(Localizer[ex.Message]);
            }
        }

        [HttpPost("{token}/deliverables/{deliverableId:long}/comment")]
        public async Task<IActionResult> AddComment(string token, long deliverableId, [FromBody] PortalCommentRequest request, CancellationToken cancellationToken)
        {
            try
            {
                CreatorPortalContext ctx = await portalService.ResolveContext(token, cancellationToken);
                return Http200(await portalService.AddReviewComment(ctx.Creator.Id, deliverableId, request.Body, cancellationToken));
            }
            catch (InvalidOperationException ex)
            {
                return Http401(Localizer[ex.Message]);
            }
        }

        [HttpPost("{token}/deliverables/{deliverableId:long}/upload")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(MaxUploadBytes)]
        public async Task<IActionResult> UploadAsset(string token, long deliverableId, [FromForm] IFormFile file, CancellationToken cancellationToken)
        {
            try
            {
                CreatorPortalContext ctx = await portalService.ResolveContext(token, cancellationToken);
                await portalService.EnsureCreatorOwnsDeliverable(ctx.Creator.Id, deliverableId, cancellationToken);
                await using Stream stream = file.OpenReadStream();
                ContentFileResult result = await fileStorage.SaveAsync(deliverableId, stream, file.FileName, file.ContentType, cancellationToken);
                return Http200(new { storageKey = result.StorageKey, previewUrl = mediaTokens.BuildSignedUrl(result.StorageKey), result.FileName, result.ContentType });
            }
            catch (InvalidOperationException ex)
            {
                return Http401(Localizer[ex.Message]);
            }
        }
    }
}
