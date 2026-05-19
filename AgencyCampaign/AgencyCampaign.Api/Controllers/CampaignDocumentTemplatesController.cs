using AgencyCampaign.Api.Contracts.CampaignDocumentTemplates;
using AgencyCampaign.Application.Catalogs;
using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.CampaignDocumentTemplates;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Api.Controllers
{
    [AccessArea("campaignDocumentTemplates.area")]
    public sealed class CampaignDocumentTemplatesController : ApiControllerBase
    {
        private readonly ICampaignDocumentTemplateService templateService;
        private new readonly IStringLocalizer<AgencyCampaignResource> Localizer;
        private static readonly Func<CampaignDocumentTemplate, CampaignDocumentTemplateContract> MapTemplate = CampaignDocumentTemplateContract.Projection.Compile();

        public CampaignDocumentTemplatesController(ICampaignDocumentTemplateService templateService, IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.templateService = templateService;
            Localizer = localizer;
        }

        [RequireAccess("campaignDocumentTemplates.get.description")]
        [GetEndpoint]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, CancellationToken cancellationToken)
        {
            PagedResult<CampaignDocumentTemplate> result = await templateService.GetTemplates(request, cancellationToken);
            return Http200(new PagedResult<CampaignDocumentTemplateContract>
            {
                Items = result.Items.Select(MapTemplate).ToArray(),
                Pagination = result.Pagination
            });
        }

        [RequireAccess("campaignDocumentTemplates.getById.description")]
        [GetEndpoint("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            CampaignDocumentTemplate? template = await templateService.GetTemplateById(id, cancellationToken);
            return template is null ? Http404(Localizer["record.notFound"]) : Http200(MapTemplate(template));
        }

        [RequireAccess("campaignDocumentTemplates.getActiveByDocumentType.description")]
        [GetEndpoint("active/{documentType:int}")]
        public async Task<IActionResult> GetActiveByDocumentType(int documentType, CancellationToken cancellationToken)
        {
            CampaignDocumentType type = (CampaignDocumentType)documentType;
            List<CampaignDocumentTemplate> templates = await templateService.GetActiveByDocumentType(type, cancellationToken);
            return Http200(templates.Select(MapTemplate).ToList());
        }

        [RequireAccess("campaignDocumentTemplates.variables.description")]
        [GetEndpoint]
        public IActionResult Variables()
        {
            var result = CampaignDocumentTemplateVariableCatalog.All
                .ToDictionary(item => (int)item.Key, item => item.Value);
            return Http200(result);
        }

        [RequireAccess("campaignDocumentTemplates.create.description")]
        [PostEndpoint]
        public async Task<IActionResult> Create([FromBody] CreateCampaignDocumentTemplateRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            CampaignDocumentTemplate template = await templateService.CreateTemplate(request, cancellationToken);
            return Http201(MapTemplate(template), Localizer["record.created"]);
        }

        [RequireAccess("campaignDocumentTemplates.update.description")]
        [PutEndpoint("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateCampaignDocumentTemplateRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            CampaignDocumentTemplate template = await templateService.UpdateTemplate(id, request, cancellationToken);
            return Http200(MapTemplate(template), Localizer["record.updated"]);
        }

        [RequireAccess("campaignDocumentTemplates.delete.description")]
        [DeleteEndpoint("{id:long}")]
        public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
        {
            bool deleted = await templateService.DeleteTemplate(id, cancellationToken);
            return Http200(new { deleted, deactivatedInsteadOfDeleted = !deleted }, Localizer["record.deleted"]);
        }

        [RequireAccess("campaignDocumentTemplates.preview.description")]
        [PostEndpoint]
        public async Task<IActionResult> Preview([FromBody] PreviewCampaignDocumentTemplateRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            string html = await templateService.PreviewTemplate(request.Body, request.DocumentType, cancellationToken);
            return Http200(new { html });
        }
    }
}
