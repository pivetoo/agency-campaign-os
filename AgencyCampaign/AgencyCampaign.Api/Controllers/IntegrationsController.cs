using AgencyCampaign.Api.Contracts.Integrations;
using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.Integrations;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Api.Controllers
{
    public sealed class IntegrationsController : ApiControllerBase
    {
        private readonly IIntegrationService integrationService;
        private new readonly IStringLocalizer<AgencyCampaignResource> Localizer;
        private static readonly Func<Integration, IntegrationContract> MapIntegration = IntegrationContract.Projection.Compile();

        public IntegrationsController(IIntegrationService integrationService, IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.integrationService = integrationService;
            Localizer = localizer;
        }

        [RequireAccess("Permite listar as integracoes cadastradas.")]
        [GetEndpoint("[action]")]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, CancellationToken cancellationToken)
        {
            PagedResult<Integration> result = await integrationService.GetIntegrations(request, cancellationToken);
            return Http200(new PagedResult<IntegrationContract>
            {
                Items = result.Items.Select(MapIntegration).ToArray(),
                Pagination = result.Pagination
            });
        }

        [RequireAccess("Permite consultar os detalhes de uma integracao.")]
        [GetEndpoint("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            Integration? integration = await integrationService.GetIntegrationById(id, cancellationToken);
            return integration is null ? Http404(Localizer["record.notFound"]) : Http200(MapIntegration(integration));
        }

        [RequireAccess("Permite listar as integracoes ativas.")]
        [GetEndpoint("active")]
        public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
        {
            List<Integration> integrations = await integrationService.GetActiveIntegrations(cancellationToken);
            return Http200(integrations.Select(MapIntegration).ToList());
        }

        [RequireAccess("Permite cadastrar uma nova integracao.")]
        [PostEndpoint("[action]")]
        public async Task<IActionResult> Create([FromBody] CreateIntegrationRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            Integration integration = await integrationService.CreateIntegration(request, cancellationToken);
            return Http201(MapIntegration(integration), Localizer["record.created"]);
        }

        [RequireAccess("Permite atualizar os dados de uma integracao.")]
        [PutEndpoint("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateIntegrationRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            Integration integration = await integrationService.UpdateIntegration(id, request, cancellationToken);
            return Http200(MapIntegration(integration), Localizer["record.updated"]);
        }
    }
}
