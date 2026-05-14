using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.Proposals;
using AgencyCampaign.Application.Services;
using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Api.Controllers
{
    public sealed class ProposalBlocksController : ApiControllerBase
    {
        private readonly IProposalBlockService blockService;
        private new readonly IStringLocalizer<AgencyCampaignResource> Localizer;

        public ProposalBlocksController(IProposalBlockService blockService, IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.blockService = blockService;
            Localizer = localizer;
        }

        [RequireAccess("Permite listar os blocos reutilizáveis de proposta.")]
        [GetEndpoint("[action]")]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, [FromQuery] string? search, [FromQuery] string? category, [FromQuery] bool includeInactive, CancellationToken cancellationToken)
        {
            return Http200(await blockService.GetAll(request, search, category, includeInactive, cancellationToken));
        }

        [RequireAccess("Permite consultar um bloco de proposta.")]
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            var block = await blockService.GetById(id, cancellationToken);
            return block is null ? Http404(Localizer["record.notFound"]) : Http200(block);
        }

        [RequireAccess("Permite cadastrar um bloco reutilizável.")]
        [PostEndpoint("[action]")]
        public async Task<IActionResult> Create([FromBody] CreateProposalBlockRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            var block = await blockService.Create(request, cancellationToken);
            return Http201(block, Localizer["record.created"]);
        }

        [RequireAccess("Permite atualizar um bloco reutilizável.")]
        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateProposalBlockRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            var block = await blockService.Update(id, request, cancellationToken);
            return Http200(block, Localizer["record.updated"]);
        }

        [RequireAccess("Permite excluir um bloco reutilizável.")]
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
        {
            await blockService.Delete(id, cancellationToken);
            return Http204();
        }
    }
}
