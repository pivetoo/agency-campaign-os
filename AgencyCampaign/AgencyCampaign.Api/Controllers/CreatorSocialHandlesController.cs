using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.CreatorSocialHandles;
using AgencyCampaign.Application.Services;
using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Api.Controllers
{
    public sealed class CreatorSocialHandlesController : ApiControllerBase
    {
        private readonly ICreatorSocialHandleService service;
        private new readonly IStringLocalizer<AgencyCampaignResource> Localizer;

        public CreatorSocialHandlesController(ICreatorSocialHandleService service, IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.service = service;
            Localizer = localizer;
        }

        [RequireAccess("Permite listar os handles sociais de um creator.")]
        [GetEndpoint("creator/{creatorId:long}")]
        public async Task<IActionResult> GetByCreator(long creatorId, CancellationToken cancellationToken)
        {
            return Http200(await service.GetByCreator(creatorId, cancellationToken));
        }

        [RequireAccess("Permite cadastrar um handle social para o creator.")]
        [PostEndpoint("[action]")]
        public async Task<IActionResult> Create([FromBody] CreateCreatorSocialHandleRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            var result = await service.Create(request, cancellationToken);
            return Http201(result, Localizer["record.created"]);
        }

        [RequireAccess("Permite atualizar um handle social do creator.")]
        [PutEndpoint("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateCreatorSocialHandleRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            var result = await service.Update(id, request, cancellationToken);
            return Http200(result, Localizer["record.updated"]);
        }

        [RequireAccess("Permite excluir um handle social do creator.")]
        [DeleteEndpoint("{id:long}")]
        public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
        {
            await service.Delete(id, cancellationToken);
            return Http204();
        }
    }
}
