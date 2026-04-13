using AgencyCampaign.Api.Contracts.Creators;
using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.Creators;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Api.Controllers
{
    public sealed class CreatorsController : ApiControllerBase
    {
        private readonly ICreatorService creatorService;
        private new readonly IStringLocalizer<AgencyCampaignResource> Localizer;
        private static readonly Func<Creator, CreatorContract> MapCreator = CreatorContract.Projection.Compile();

        public CreatorsController(ICreatorService creatorService, IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.creatorService = creatorService;
            Localizer = localizer;
        }

        [RequireAccess("Permite listar os creators cadastrados.")]
        [GetEndpoint("[action]")]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, CancellationToken cancellationToken)
        {
            PagedResult<Creator> result = await creatorService.GetCreators(request, cancellationToken);
            return Http200(new PagedResult<CreatorContract>
            {
                Items = result.Items.Select(MapCreator).ToArray(),
                Pagination = result.Pagination
            });
        }

        [RequireAccess("Permite consultar os detalhes de um creator.")]
        [GetEndpoint("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            Creator? creator = await creatorService.GetCreatorById(id, cancellationToken);
            return creator is null ? Http404(Localizer["record.notFound"]) : Http200(MapCreator(creator));
        }

        [RequireAccess("Permite cadastrar um novo creator.")]
        [PostEndpoint("[action]")]
        public async Task<IActionResult> Create([FromBody] CreateCreatorRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            Creator creator = await creatorService.CreateCreator(request, cancellationToken);
            return Http201(MapCreator(creator), Localizer["record.created"]);
        }

        [RequireAccess("Permite atualizar os dados de um creator.")]
        [PutEndpoint("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateCreatorRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            Creator creator = await creatorService.UpdateCreator(id, request, cancellationToken);
            return Http200(MapCreator(creator), Localizer["record.updated"]);
        }
    }
}
