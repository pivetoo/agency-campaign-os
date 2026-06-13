using AgencyCampaign.Api.Contracts.CampaignDocuments;
using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.CampaignDocuments;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Infrastructure.Options;
using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace AgencyCampaign.Api.Controllers
{
    [AccessArea("campaignDocuments.area")]
    public sealed class CampaignDocumentsController : ApiControllerBase
    {
        private readonly ICampaignDocumentService campaignDocumentService;
        private readonly WebhookOptions webhookOptions;
        private new readonly IStringLocalizer<AgencyCampaignResource> Localizer;
        private static readonly Func<CampaignDocument, CampaignDocumentContract> MapDocument = CampaignDocumentContract.Projection.Compile();

        public CampaignDocumentsController(ICampaignDocumentService campaignDocumentService, IOptions<WebhookOptions> webhookOptions, IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.campaignDocumentService = campaignDocumentService;
            this.webhookOptions = webhookOptions.Value;
            Localizer = localizer;
        }

        [RequireAccess("campaignDocuments.get.description")]
        [GetEndpoint]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, CancellationToken cancellationToken)
        {
            PagedResult<CampaignDocument> result = await campaignDocumentService.GetDocuments(request, cancellationToken);
            return Http200(new PagedResult<CampaignDocumentContract>
            {
                Items = result.Items.Select(MapDocument).ToArray(),
                Pagination = result.Pagination
            });
        }

        [RequireAccess("campaignDocuments.getById.description")]
        [GetEndpoint("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            CampaignDocument? document = await campaignDocumentService.GetDocumentById(id, cancellationToken);
            return document is null ? Http404(Localizer["record.notFound"]) : Http200(MapDocument(document));
        }

        [RequireAccess("campaignDocuments.getById.description")]
        [GetEndpoint("verify-integrity/{id:long}")]
        public async Task<IActionResult> VerifyIntegrity(long id, CancellationToken cancellationToken)
        {
            return Http200(await campaignDocumentService.VerifyContentIntegrity(id, cancellationToken));
        }

        [RequireAccess("campaignDocuments.getByCampaign.description")]
        [GetEndpoint("campaign/{campaignId:long}")]
        public async Task<IActionResult> GetByCampaign(long campaignId, CancellationToken cancellationToken)
        {
            if (campaignId <= 0)
            {
                return Http400(Localizer["request.id.required"]);
            }

            List<CampaignDocument> documents = await campaignDocumentService.GetByCampaign(campaignId, cancellationToken);
            return Http200(documents.Select(MapDocument).ToList());
        }

        [RequireAccess("campaignDocuments.create.description")]
        [PostEndpoint]
        public async Task<IActionResult> Create([FromBody] CreateCampaignDocumentRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            CampaignDocument document = await campaignDocumentService.CreateDocument(request, cancellationToken);
            return Http201(MapDocument(document), Localizer["record.created"]);
        }

        [RequireAccess("campaignDocuments.update.description")]
        [PutEndpoint("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateCampaignDocumentRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            CampaignDocument document = await campaignDocumentService.UpdateDocument(id, request, cancellationToken);
            return Http200(MapDocument(document), Localizer["record.updated"]);
        }

        [RequireAccess("campaignDocuments.sendEmail.description")]
        [HttpPost("{id:long}/send-email")]
        public async Task<IActionResult> SendEmail(long id, [FromBody] SendCampaignDocumentEmailRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            CampaignDocument document = await campaignDocumentService.SendDocumentEmail(id, request, cancellationToken);
            return Http200(MapDocument(document), Localizer["record.updated"]);
        }

        [RequireAccess("campaignDocuments.sendWhatsapp.description")]
        [HttpPost("{id:long}/send-whatsapp")]
        public async Task<IActionResult> SendWhatsapp(long id, [FromBody] SendCampaignDocumentWhatsappRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            CampaignDocument document = await campaignDocumentService.SendDocumentWhatsapp(id, request, cancellationToken);
            return Http200(MapDocument(document), Localizer["record.updated"]);
        }

        [RequireAccess("campaignDocuments.markSigned.description")]
        [HttpPost("{id:long}/mark-signed")]
        public async Task<IActionResult> MarkSigned(long id, [FromBody] MarkCampaignDocumentSignedRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            CampaignDocument document = await campaignDocumentService.MarkAsSigned(id, request, cancellationToken);
            return Http200(MapDocument(document), Localizer["record.updated"]);
        }

        [RequireAccess("campaignDocuments.generateFromTemplate.description")]
        [PostEndpoint]
        public async Task<IActionResult> GenerateFromTemplate([FromBody] GenerateCampaignDocumentFromTemplateRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            CampaignDocument document = await campaignDocumentService.GenerateFromTemplate(request, cancellationToken);
            return Http201(MapDocument(document), Localizer["record.created"]);
        }

        [RequireAccess("campaignDocuments.sendForSignature.description")]
        [HttpPost("{id:long}/send-signature")]
        public async Task<IActionResult> SendForSignature(long id, [FromBody] SendCampaignDocumentForSignatureRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            CampaignDocument document = await campaignDocumentService.SendForSignature(id, request, cancellationToken);
            return Http200(MapDocument(document), Localizer["record.updated"]);
        }

        // Webhook do provedor de assinatura. O {callbackToken} na URL carrega o prefixo de tenant (PublicLinkToken)
        // e e resolvido pelo PublicTenantResolutionMiddleware ANTES do controller; o pipeline de assinatura do
        // IntegrationPlatform ecoa nesta URL o callbackToken enviado no payload do envio. Segredo verificado por header.
        [AllowAnonymous]
        [PostEndpoint("provider-callback/{callbackToken}")]
        public Task<IActionResult> ProviderCallbackForTenant(string callbackToken, [FromBody] CampaignDocumentProviderCallbackRequest request, CancellationToken cancellationToken)
        {
            _ = callbackToken;
            return HandleProviderCallbackAsync(request, cancellationToken);
        }

        private async Task<IActionResult> HandleProviderCallbackAsync(CampaignDocumentProviderCallbackRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(webhookOptions.ProviderCallbackSecret))
            {
                return Http403(Localizer["webhook.secret.notConfigured"]);
            }

            string? incomingSecret = Request.Headers["x-webhook-secret"].FirstOrDefault();
            if (!string.Equals(incomingSecret, webhookOptions.ProviderCallbackSecret, StringComparison.Ordinal))
            {
                return Http403(Localizer["webhook.secret.invalid"]);
            }

            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            CampaignDocument document = await campaignDocumentService.HandleProviderCallback(request, cancellationToken);
            return Http200(MapDocument(document), Localizer["record.updated"]);
        }
    }
}
