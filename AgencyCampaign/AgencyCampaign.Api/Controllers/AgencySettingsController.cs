using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.AgencySettings;
using AgencyCampaign.Application.Services;
using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Api.Controllers
{
    public sealed class AgencySettingsController : ApiControllerBase
    {
        private const long MaxLogoBytes = 2 * 1024 * 1024;

        private readonly IAgencySettingsService service;
        private readonly IImageUploadStorage imageStorage;
        private new readonly IStringLocalizer<AgencyCampaignResource> Localizer;

        public AgencySettingsController(IAgencySettingsService service, IImageUploadStorage imageStorage, IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.service = service;
            this.imageStorage = imageStorage;
            Localizer = localizer;
        }

        [RequireAccess("Permite consultar as configurações da agência.")]
        [GetEndpoint("[action]")]
        public async Task<IActionResult> Get(CancellationToken cancellationToken)
        {
            return Http200(await service.Get(cancellationToken));
        }

        [RequireAccess("Permite atualizar as configurações da agência.")]
        [PutEndpoint("[action]")]
        public async Task<IActionResult> Update([FromBody] UpdateAgencySettingsRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            var result = await service.Update(request, cancellationToken);
            return Http200(result, Localizer["record.updated"]);
        }

        [RequireAccess("Permite enviar a logo da agência.")]
        [PostEndpoint("[action]")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(MaxLogoBytes)]
        public async Task<IActionResult> UploadLogo([FromForm] IFormFile file, CancellationToken cancellationToken)
        {
            if (file is null || file.Length == 0)
            {
                return Http400("Arquivo nao informado.");
            }

            if (file.Length > MaxLogoBytes)
            {
                return Http400("Arquivo excede o limite de 2MB.");
            }

            var current = await service.Get(cancellationToken);

            await using Stream stream = file.OpenReadStream();
            string logoUrl = await imageStorage.SaveAsync("agency", current.Id, stream, file.ContentType, cancellationToken);

            var result = await service.SetLogo(logoUrl, cancellationToken);
            return Http200(result, Localizer["record.updated"]);
        }

        [RequireAccess("Permite remover a logo da agência.")]
        [DeleteEndpoint("[action]")]
        public async Task<IActionResult> RemoveLogo(CancellationToken cancellationToken)
        {
            var current = await service.Get(cancellationToken);
            await imageStorage.RemoveAsync("agency", current.Id, cancellationToken);
            var result = await service.RemoveLogo(cancellationToken);
            return Http200(result, Localizer["record.updated"]);
        }

        [RequireAccess("Permite consultar os layouts de proposta disponíveis.")]
        [GetEndpoint("[action]")]
        public async Task<IActionResult> GetProposalLayouts(CancellationToken cancellationToken)
        {
            var layouts = await service.GetProposalLayouts(cancellationToken);
            return Http200(layouts);
        }

        [RequireAccess("Permite salvar o template HTML da proposta.")]
        [PutEndpoint("[action]")]
        public async Task<IActionResult> SaveProposalTemplate([FromBody] SetProposalTemplateRequest request, CancellationToken cancellationToken)
        {
            var result = await service.SaveProposalTemplate(request.Template, cancellationToken);
            return Http200(result, Localizer["record.updated"]);
        }

        [RequireAccess("Permite visualizar um preview do template HTML da proposta.")]
        [PostEndpoint("[action]")]
        public async Task<IActionResult> PreviewProposalTemplate([FromBody] PreviewProposalTemplateRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            string html = await service.PreviewProposalTemplate(request.Template, cancellationToken);
            return Http200(new { html });
        }

        [RequireAccess("Permite listar as versões salvas do template de proposta.")]
        [GetEndpoint("[action]")]
        public async Task<IActionResult> GetProposalTemplateVersions(CancellationToken cancellationToken)
        {
            var versions = await service.GetProposalTemplateVersions(cancellationToken);
            return Http200(versions);
        }

        [RequireAccess("Permite salvar uma nova versão do template de proposta.")]
        [PostEndpoint("[action]")]
        public async Task<IActionResult> SaveProposalTemplateVersion([FromBody] SaveProposalTemplateVersionRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            var result = await service.SaveProposalTemplateVersion(request.Name, request.Template, request.Activate, cancellationToken);
            return Http200(result, Localizer["record.created"]);
        }

        [RequireAccess("Permite ativar uma versão do template de proposta.")]
        [PutEndpoint("[action]")]
        public async Task<IActionResult> ActivateProposalTemplateVersion([FromQuery] long id, CancellationToken cancellationToken)
        {
            var result = await service.ActivateProposalTemplateVersion(id, cancellationToken);
            return Http200(result, Localizer["record.updated"]);
        }

        [RequireAccess("Permite excluir uma versão do template de proposta.")]
        [DeleteEndpoint("[action]")]
        public async Task<IActionResult> DeleteProposalTemplateVersion([FromQuery] long id, CancellationToken cancellationToken)
        {
            await service.DeleteProposalTemplateVersion(id, cancellationToken);
            return Http200(Localizer["record.deleted"]);
        }
    }
}
