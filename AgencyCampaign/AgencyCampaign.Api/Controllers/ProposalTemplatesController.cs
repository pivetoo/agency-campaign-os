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
    public sealed class ProposalTemplatesController : ApiControllerBase
    {
        private readonly IProposalTemplateService templateService;
        private new readonly IStringLocalizer<AgencyCampaignResource> Localizer;

        public ProposalTemplatesController(IProposalTemplateService templateService, IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.templateService = templateService;
            Localizer = localizer;
        }

        [RequireAccess("Permite listar os templates de proposta.")]
        [GetEndpoint("[action]")]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, [FromQuery] string? search, [FromQuery] bool includeInactive, CancellationToken cancellationToken)
        {
            return Http200(await templateService.GetAll(request, search, includeInactive, cancellationToken));
        }

        [RequireAccess("Permite consultar um template de proposta.")]
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            var template = await templateService.GetById(id, cancellationToken);
            return template is null ? Http404(Localizer["record.notFound"]) : Http200(template);
        }

        [RequireAccess("Permite cadastrar um template de proposta.")]
        [PostEndpoint("[action]")]
        public async Task<IActionResult> Create([FromBody] CreateProposalTemplateRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            var template = await templateService.Create(request, cancellationToken);
            return Http201(template, Localizer["record.created"]);
        }

        [RequireAccess("Permite atualizar um template de proposta.")]
        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateProposalTemplateRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            var template = await templateService.Update(id, request, cancellationToken);
            return Http200(template, Localizer["record.updated"]);
        }

        [RequireAccess("Permite excluir um template de proposta.")]
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
        {
            await templateService.Delete(id, cancellationToken);
            return Http204();
        }

        [RequireAccess("Permite aplicar um template a uma proposta existente.")]
        [HttpPost("{templateId:long}/ApplyToProposal/{proposalId:long}")]
        public async Task<IActionResult> ApplyToProposal(long templateId, long proposalId, CancellationToken cancellationToken)
        {
            int created = await templateService.ApplyToProposal(proposalId, templateId, cancellationToken);
            return Http200(new { itemsCreated = created }, Localizer["record.updated"]);
        }
    }
}
