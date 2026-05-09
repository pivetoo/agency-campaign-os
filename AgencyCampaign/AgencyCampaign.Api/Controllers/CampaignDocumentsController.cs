using AgencyCampaign.Api.Contracts.CampaignDocuments;
using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.CampaignDocuments;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Api.Controllers
{
    public sealed class CampaignDocumentsController : ApiControllerBase
    {
        private readonly ICampaignDocumentService campaignDocumentService;
        private new readonly IStringLocalizer<AgencyCampaignResource> Localizer;
        private static readonly Func<CampaignDocument, CampaignDocumentContract> MapDocument = CampaignDocumentContract.Projection.Compile();

        public CampaignDocumentsController(ICampaignDocumentService campaignDocumentService, IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.campaignDocumentService = campaignDocumentService;
            Localizer = localizer;
        }

        [RequireAccess("Permite listar os documentos cadastrados.")]
        [GetEndpoint("[action]")]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, CancellationToken cancellationToken)
        {
            PagedResult<CampaignDocument> result = await campaignDocumentService.GetDocuments(request, cancellationToken);
            return Http200(new PagedResult<CampaignDocumentContract>
            {
                Items = result.Items.Select(MapDocument).ToArray(),
                Pagination = result.Pagination
            });
        }

        [RequireAccess("Permite consultar os detalhes de um documento.")]
        [GetEndpoint("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            CampaignDocument? document = await campaignDocumentService.GetDocumentById(id, cancellationToken);
            return document is null ? Http404(Localizer["record.notFound"]) : Http200(MapDocument(document));
        }

        [RequireAccess("Permite listar os documentos vinculados a uma campanha.")]
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

        [RequireAccess("Permite cadastrar um documento.")]
        [PostEndpoint("[action]")]
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

        [RequireAccess("Permite atualizar um documento.")]
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

        [RequireAccess("Permite enviar um documento por e-mail.")]
        [PostEndpoint("{id:long}/send-email")]
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

        [RequireAccess("Permite marcar um documento como assinado.")]
        [PostEndpoint("{id:long}/mark-signed")]
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

        [RequireAccess("Permite gerar um documento a partir de um template.")]
        [PostEndpoint("[action]")]
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

        [RequireAccess("Permite enviar um documento para assinatura digital via IntegrationPlatform.")]
        [PostEndpoint("{id:long}/send-signature")]
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

        [RequireAccess("Permite que o IntegrationPlatform notifique eventos do provider de assinatura.")]
        [PostEndpoint("[action]")]
        public async Task<IActionResult> ProviderCallback([FromBody] CampaignDocumentProviderCallbackRequest request, CancellationToken cancellationToken)
        {
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
