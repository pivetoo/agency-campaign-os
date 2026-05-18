using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.Banks;
using AgencyCampaign.Application.Services;
using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Api.Controllers
{
    public sealed class BanksController : ApiControllerBase
    {
        private const long MaxLogoBytes = 2 * 1024 * 1024;

        private readonly IBankService service;
        private readonly IImageUploadStorage imageStorage;
        private new readonly IStringLocalizer<AgencyCampaignResource> Localizer;

        public BanksController(IBankService service, IImageUploadStorage imageStorage, IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.service = service;
            this.imageStorage = imageStorage;
            Localizer = localizer;
        }

        [RequireAccess("Permite listar os bancos cadastrados.")]
        [GetEndpoint("[action]")]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, [FromQuery] string? search, [FromQuery] bool includeInactive, CancellationToken cancellationToken)
        {
            return Http200(await service.GetAll(request, search, includeInactive, cancellationToken));
        }

        [RequireAccess("Permite listar bancos ativos para seleção.")]
        [GetEndpoint("active")]
        public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
        {
            return Http200(await service.GetActive(cancellationToken));
        }

        [RequireAccess("Permite consultar um banco por id.")]
        [GetEndpoint("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            var result = await service.GetById(id, cancellationToken);
            return result is null ? Http404(Localizer["record.notFound"]) : Http200(result);
        }

        [RequireAccess("Permite cadastrar um novo banco no catálogo.")]
        [PostEndpoint("[action]")]
        public async Task<IActionResult> Create([FromBody] CreateBankRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            var result = await service.Create(request, cancellationToken);
            return Http201(result, Localizer["record.created"]);
        }

        [RequireAccess("Permite atualizar os dados de um banco.")]
        [PutEndpoint("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateBankRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            var result = await service.Update(id, request, cancellationToken);
            return Http200(result, Localizer["record.updated"]);
        }

        [RequireAccess("Permite excluir um banco custom (não permitido em bancos do sistema).")]
        [DeleteEndpoint("{id:long}")]
        public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
        {
            await service.Delete(id, cancellationToken);
            return Http204();
        }

        [RequireAccess("Permite enviar o logo do banco.")]
        [PostEndpoint("[action]/{id:long}")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(MaxLogoBytes)]
        public async Task<IActionResult> UploadLogo(long id, [FromForm] IFormFile file, CancellationToken cancellationToken)
        {
            if (file is null || file.Length == 0)
            {
                return Http400(Localizer["imageUpload.file.required"]);
            }

            if (file.Length > MaxLogoBytes)
            {
                return Http400(Localizer["imageUpload.file.exceedsLimit"]);
            }

            var existing = await service.GetById(id, cancellationToken);
            if (existing is null)
            {
                return Http404(Localizer["bank.notFound"]);
            }

            await using Stream stream = file.OpenReadStream();
            string logoUrl = await imageStorage.SaveAsync("banks", id, stream, file.ContentType, cancellationToken);

            var result = await service.SetLogo(id, logoUrl, cancellationToken);
            return Http200(result, Localizer["record.updated"]);
        }

        [RequireAccess("Permite remover o logo customizado do banco (bancos system voltam ao logo padrão).")]
        [DeleteEndpoint("[action]/{id:long}")]
        public async Task<IActionResult> RemoveLogo(long id, CancellationToken cancellationToken)
        {
            var existing = await service.GetById(id, cancellationToken);
            if (existing is null)
            {
                return Http404(Localizer["bank.notFound"]);
            }

            await imageStorage.RemoveAsync("banks", id, cancellationToken);
            var result = await service.RemoveLogo(id, cancellationToken);
            return Http200(result, Localizer["record.updated"]);
        }
    }
}
