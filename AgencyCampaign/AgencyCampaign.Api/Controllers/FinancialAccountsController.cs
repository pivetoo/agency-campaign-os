using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.FinancialAccounts;
using AgencyCampaign.Application.Services;
using Archon.Api.Attributes;
using Archon.Api.Controllers;
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
        [GetEndpoint("[action]")]
        public async Task<IActionResult> Get([FromQuery] bool includeInactive, CancellationToken cancellationToken)
        {
            return Http200(await service.GetAll(includeInactive, cancellationToken));
        }

        [RequireAccess("Permite consultar uma conta financeira por id.")]
        [GetEndpoint("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            var result = await service.GetById(id, cancellationToken);
            return result is null ? Http404(Localizer["record.notFound"]) : Http200(result);
        }

        [RequireAccess("Permite cadastrar uma nova conta financeira.")]
        [PostEndpoint("[action]")]
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
    }
}
