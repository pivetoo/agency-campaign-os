using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.FinancialAccounts;
using AgencyCampaign.Application.Services;
using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Api.Controllers
{
    public sealed class FinancialAccountsController : ApiControllerBase
    {
        private readonly IFinancialAccountService service;
        private new readonly IStringLocalizer<AgencyCampaignResource> Localizer;

        public FinancialAccountsController(IFinancialAccountService service, IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.service = service;
            Localizer = localizer;
        }

        [RequireAccess("Permite listar as contas financeiras cadastradas.")]
        [GetEndpoint]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, [FromQuery] string? search, [FromQuery] bool includeInactive, CancellationToken cancellationToken)
        {
            return Http200(await service.GetAll(request, search, includeInactive, cancellationToken));
        }

        [RequireAccess("Permite consultar o resumo agregado das contas financeiras.")]
        [GetEndpoint]
        public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
        {
            return Http200(await service.GetSummary(cancellationToken));
        }

        [RequireAccess("Permite consultar uma conta financeira por id.")]
        [GetEndpoint("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            var result = await service.GetById(id, cancellationToken);
            return result is null ? Http404(Localizer["record.notFound"]) : Http200(result);
        }

        [RequireAccess("Permite cadastrar uma nova conta financeira.")]
        [PostEndpoint]
        public async Task<IActionResult> Create([FromBody] CreateFinancialAccountRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            var result = await service.Create(request, cancellationToken);
            return Http201(result, Localizer["record.created"]);
        }

        [RequireAccess("Permite atualizar uma conta financeira.")]
        [PutEndpoint("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateFinancialAccountRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            var result = await service.Update(id, request, cancellationToken);
            return Http200(result, Localizer["record.updated"]);
        }

        [RequireAccess("Permite excluir uma conta financeira.")]
        [DeleteEndpoint("{id:long}")]
        public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
        {
            await service.Delete(id, cancellationToken);
            return Http204();
        }

        [RequireAccess("Permite vincular uma conta financeira a um conector do IntegrationPlatform.")]
        [PutEndpoint("{id:long}/attach-connector")]
        public async Task<IActionResult> AttachConnector(long id, [FromBody] AttachConnectorRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            var result = await service.AttachConnector(id, request.ConnectorId, cancellationToken);
            return Http200(result, Localizer["financialAccount.connector.attached"]);
        }

        [RequireAccess("Permite remover o vínculo da conta financeira com o conector.")]
        [PutEndpoint("{id:long}/detach-connector")]
        public async Task<IActionResult> DetachConnector(long id, CancellationToken cancellationToken)
        {
            var result = await service.DetachConnector(id, cancellationToken);
            return Http200(result, Localizer["financialAccount.connector.detached"]);
        }

        [RequireAccess("Permite disparar uma sincronização manual da conta financeira.")]
        [PostEndpoint("{id:long}/sync")]
        public async Task<IActionResult> Sync(long id, CancellationToken cancellationToken)
        {
            long executionId = await service.TriggerSync(id, cancellationToken);
            return Http200(new { executionId }, Localizer["financialAccount.sync.triggered"]);
        }
    }
}
