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
        private readonly IImageUploadStorage imageStorage;
        private new readonly IStringLocalizer<AgencyCampaignResource> Localizer;
        private static readonly Func<Brand, BrandContract> MapBrand = BrandContract.Projection.Compile();

        public BrandsController(IBrandService brandService, IImageUploadStorage imageStorage, IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.brandService = brandService;
            this.imageStorage = imageStorage;
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

        [RequireAccess("Permite enviar a logo da marca.")]
        [PostEndpoint("[action]/{id:long}")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(MaxLogoBytes)]
        public async Task<IActionResult> UploadLogo(long id, [FromForm] IFormFile file, CancellationToken cancellationToken)
        {
            if (file is null || file.Length == 0)
            {
                return Http400("Arquivo nao informado.");
            }

            if (file.Length > MaxLogoBytes)
            {
                return Http400("Arquivo excede o limite de 2MB.");
            }

            Brand? existing = await brandService.GetBrandById(id, cancellationToken);
            if (existing is null)
            {
                return Http404(Localizer["record.notFound"]);
            }

            await using Stream stream = file.OpenReadStream();
            string logoUrl = await imageStorage.SaveAsync("brands", id, stream, file.ContentType, cancellationToken);

            Brand brand = await brandService.SetBrandLogo(id, logoUrl, cancellationToken);
            return Http200(MapBrand(brand), Localizer["record.updated"]);
        }

        [RequireAccess("Permite remover a logo da marca.")]
        [DeleteEndpoint("[action]/{id:long}")]
        public async Task<IActionResult> RemoveLogo(long id, CancellationToken cancellationToken)
        {
            Brand? existing = await brandService.GetBrandById(id, cancellationToken);
            if (existing is null)
            {
                return Http404(Localizer["record.notFound"]);
            }

            await imageStorage.RemoveAsync("brands", id, cancellationToken);
            Brand brand = await brandService.RemoveBrandLogo(id, cancellationToken);
            return Http200(MapBrand(brand), Localizer["record.updated"]);
        }
    }
}
