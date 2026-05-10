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

        [RequireAccess("Permite listar os templates de documento.")]
        [GetEndpoint("[action]")]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, CancellationToken cancellationToken)
        {
            PagedResult<CampaignDocumentTemplate> result = await templateService.GetTemplates(request, cancellationToken);
            return Http200(new PagedResult<CampaignDocumentTemplateContract>
            {
                Items = result.Items.Select(MapTemplate).ToArray(),
                Pagination = result.Pagination
            });
        }

        [RequireAccess("Permite consultar um template de documento.")]
        [GetEndpoint("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            CampaignDocumentTemplate? template = await templateService.GetTemplateById(id, cancellationToken);
            return template is null ? Http404(Localizer["record.notFound"]) : Http200(MapTemplate(template));
        }

        [RequireAccess("Permite listar os templates ativos por tipo de documento.")]
        [GetEndpoint("active/{documentType:int}")]
        public async Task<IActionResult> GetActiveByDocumentType(int documentType, CancellationToken cancellationToken)
        {
            CampaignDocumentType type = (CampaignDocumentType)documentType;
            List<CampaignDocumentTemplate> templates = await templateService.GetActiveByDocumentType(type, cancellationToken);
            return Http200(templates.Select(MapTemplate).ToList());
        }

        [RequireAccess("Permite listar as variaveis disponiveis por tipo de documento.")]
        [GetEndpoint("[action]")]
        public IActionResult Variables()
        {
            var result = CampaignDocumentTemplateVariableCatalog.All
                .ToDictionary(item => (int)item.Key, item => item.Value);
            return Http200(result);
        }

        [RequireAccess("Permite cadastrar um template de documento.")]
        [PostEndpoint("[action]")]
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

        [RequireAccess("Permite atualizar um template de documento.")]
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

        [RequireAccess("Permite remover um template de documento.")]
        [DeleteEndpoint("{id:long}")]
        public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
        {
            bool deleted = await templateService.DeleteTemplate(id, cancellationToken);
            return Http200(new { deleted, deactivatedInsteadOfDeleted = !deleted }, Localizer["record.deleted"]);
        }
    }
}
