using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.Automations;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Api.Controllers
{
    public sealed class AutomationsController : ApiControllerBase
    {
        private readonly IAutomationService automationService;
        private new readonly IStringLocalizer<AgencyCampaignResource> Localizer;

        public AutomationsController(IAutomationService automationService, IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.automationService = automationService;
            Localizer = localizer;
        }

        [RequireAccess("Permite listar as automacoes cadastradas.")]
        [GetEndpoint("[action]")]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, CancellationToken cancellationToken)
        {
            PagedResult<Automation> result = await automationService.GetAutomations(request, cancellationToken);
            return Http200(result);
        }

        [RequireAccess("Permite consultar os detalhes de uma automacao.")]
        [GetEndpoint("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            Automation? automation = await automationService.GetAutomationById(id, cancellationToken);
            return automation is null ? Http404(Localizer["record.notFound"]) : Http200(automation);
        }

        [RequireAccess("Permite cadastrar uma nova automacao.")]
        [PostEndpoint("[action]")]
        public async Task<IActionResult> Create([FromBody] CreateAutomationRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            Automation automation = await automationService.CreateAutomation(request, cancellationToken);
            return Http201(automation, Localizer["record.created"]);
        }

        [RequireAccess("Permite atualizar os dados de uma automacao.")]
        [PutEndpoint("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateAutomationRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            Automation automation = await automationService.UpdateAutomation(id, request, cancellationToken);
            return Http200(automation, Localizer["record.updated"]);
        }

        [RequireAccess("Permite consultar os logs de execucao de uma automacao.")]
        [GetEndpoint("{automationId:long}/[action]")]
        public async Task<IActionResult> Logs(long automationId, [FromQuery] PagedRequest request, CancellationToken cancellationToken)
        {
            PagedResult<AutomationExecutionLog> result = await automationService.GetExecutionLogs(automationId, request, cancellationToken);
            return Http200(result);
        }
    }
}
