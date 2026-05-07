using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.FinancialSubcategories;
using AgencyCampaign.Application.Services;
using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Api.Controllers
{
    public sealed class FinancialSubcategoriesController : ApiControllerBase
    {
        private readonly IFinancialSubcategoryService service;
        private new readonly IStringLocalizer<AgencyCampaignResource> Localizer;

        public FinancialSubcategoriesController(IFinancialSubcategoryService service, IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.service = service;
            Localizer = localizer;
        }

        [RequireAccess("Permite listar as subcategorias financeiras cadastradas.")]
        [GetEndpoint("[action]")]
        public async Task<IActionResult> Get([FromQuery] bool includeInactive, CancellationToken cancellationToken)
        {
            return Http200(await service.GetAll(includeInactive, cancellationToken));
        }

        [RequireAccess("Permite cadastrar uma subcategoria financeira.")]
        [PostEndpoint("[action]")]
        public async Task<IActionResult> Create([FromBody] CreateFinancialSubcategoryRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            var result = await service.Create(request, cancellationToken);
            return Http201(result, Localizer["record.created"]);
        }

        [RequireAccess("Permite atualizar uma subcategoria financeira.")]
        [PutEndpoint("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateFinancialSubcategoryRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            var result = await service.Update(id, request, cancellationToken);
            return Http200(result, Localizer["record.updated"]);
        }

        [RequireAccess("Permite excluir uma subcategoria financeira.")]
        [DeleteEndpoint("{id:long}")]
        public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
        {
            await service.Delete(id, cancellationToken);
            return Http204();
        }
    }
}
