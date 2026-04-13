using AgencyCampaign.Application.Requests.Brands;
using AgencyCampaign.Application.Services;
using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using AgencyCampaign.Application.Localization;

namespace AgencyCampaign.Api.Controllers
{
    public sealed class BrandsController : ApiControllerBase
    {
        private readonly IBrandService brandService;
        private new readonly IStringLocalizer<AgencyCampaignResource> Localizer;

        public BrandsController(IBrandService brandService, IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.brandService = brandService;
            Localizer = localizer;
        }

        [RequireAccess("Permite listar as marcas cadastradas.")]
        [GetEndpoint("[action]")]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, CancellationToken cancellationToken)
        {
            PagedResult<Domain.Entities.Brand> result = await brandService.GetBrands(request, cancellationToken);
            return Http200(result);
        }

        [RequireAccess("Permite consultar os detalhes de uma marca.")]
        [GetEndpoint("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            Domain.Entities.Brand? brand = await brandService.GetBrandById(id, cancellationToken);
            return brand is null ? Http404(Localizer["record.notFound"]) : Http200(brand);
        }

        [RequireAccess("Permite cadastrar uma nova marca.")]
        [PostEndpoint("[action]")]
        public async Task<IActionResult> Create([FromBody] CreateBrandRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            Domain.Entities.Brand brand = await brandService.CreateBrand(request, cancellationToken);
            return Http201(brand, Localizer["record.created"]);
        }

        [RequireAccess("Permite atualizar os dados de uma marca.")]
        [PutEndpoint("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateBrandRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            Domain.Entities.Brand brand = await brandService.UpdateBrand(id, request, cancellationToken);
            return Http200(brand, Localizer["record.updated"]);
        }
    }
}
