using AgencyCampaign.Api.Contracts.Brands;
using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.Brands;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Api.Controllers
{
    public sealed class BrandsController : ApiControllerBase
    {
        private const long MaxLogoBytes = 2 * 1024 * 1024;

        private readonly IBrandService brandService;
        private readonly IBrandLogoStorage brandLogoStorage;
        private new readonly IStringLocalizer<AgencyCampaignResource> Localizer;
        private static readonly Func<Brand, BrandContract> MapBrand = BrandContract.Projection.Compile();

        public BrandsController(IBrandService brandService, IBrandLogoStorage brandLogoStorage, IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.brandService = brandService;
            this.brandLogoStorage = brandLogoStorage;
            Localizer = localizer;
        }

        [RequireAccess("Permite listar as marcas cadastradas.")]
        [GetEndpoint("[action]")]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, CancellationToken cancellationToken)
        {
            PagedResult<Brand> result = await brandService.GetBrands(request, cancellationToken);
            return Http200(new PagedResult<BrandContract>
            {
                Items = result.Items.Select(MapBrand).ToArray(),
                Pagination = result.Pagination
            });
        }

        [RequireAccess("Permite consultar os detalhes de uma marca.")]
        [GetEndpoint("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            Brand? brand = await brandService.GetBrandById(id, cancellationToken);
            return brand is null ? Http404(Localizer["record.notFound"]) : Http200(MapBrand(brand));
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

            Brand brand = await brandService.CreateBrand(request, cancellationToken);
            return Http201(MapBrand(brand), Localizer["record.created"]);
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

            Brand brand = await brandService.UpdateBrand(id, request, cancellationToken);
            return Http200(MapBrand(brand), Localizer["record.updated"]);
        }
    }
}
