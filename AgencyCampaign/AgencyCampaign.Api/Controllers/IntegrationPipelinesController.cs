using AgencyCampaign.Api.Contracts.IntegrationPipelines;
using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.IntegrationPipelines;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Api.Controllers
{
    public sealed class IntegrationPipelinesController : ApiControllerBase
    {
        private readonly IIntegrationPipelineService pipelineService;
        private new readonly IStringLocalizer<AgencyCampaignResource> Localizer;
        private static readonly Func<IntegrationPipeline, IntegrationPipelineContract> MapPipeline = IntegrationPipelineContract.Projection.Compile();

        public IntegrationPipelinesController(IIntegrationPipelineService pipelineService, IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.pipelineService = pipelineService;
            Localizer = localizer;
        }

        [RequireAccess("Permite listar os pipelines de integracao cadastrados.")]
        [GetEndpoint("[action]")]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, CancellationToken cancellationToken)
        {
            PagedResult<IntegrationPipeline> result = await pipelineService.GetPipelines(request, cancellationToken);
            return Http200(new PagedResult<IntegrationPipelineContract>
            {
                Items = result.Items.Select(MapPipeline).ToArray(),
                Pagination = result.Pagination
            });
        }

        [RequireAccess("Permite consultar os detalhes de um pipeline de integracao.")]
        [GetEndpoint("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            IntegrationPipeline? pipeline = await pipelineService.GetPipelineById(id, cancellationToken);
            return pipeline is null ? Http404(Localizer["record.notFound"]) : Http200(MapPipeline(pipeline));
        }

        [RequireAccess("Permite listar os pipelines de integracao ativos.")]
        [GetEndpoint("active")]
        public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
        {
            List<IntegrationPipeline> pipelines = await pipelineService.GetActivePipelines(cancellationToken);
            return Http200(pipelines.Select(MapPipeline).ToList());
        }

        [RequireAccess("Permite listar os pipelines de integracao por integracao.")]
        [GetEndpoint("by-integration/{integrationId:long}")]
        public async Task<IActionResult> GetByIntegration(long integrationId, CancellationToken cancellationToken)
        {
            List<IntegrationPipeline> pipelines = await pipelineService.GetPipelinesByIntegration(integrationId, cancellationToken);
            return Http200(pipelines.Select(MapPipeline).ToList());
        }

        [RequireAccess("Permite cadastrar um novo pipeline de integracao.")]
        [PostEndpoint("[action]")]
        public async Task<IActionResult> Create([FromBody] CreateIntegrationPipelineRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            IntegrationPipeline pipeline = await pipelineService.CreatePipeline(request, cancellationToken);
            return Http201(MapPipeline(pipeline), Localizer["record.created"]);
        }

        [RequireAccess("Permite atualizar os dados de um pipeline de integracao.")]
        [PutEndpoint("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateIntegrationPipelineRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            IntegrationPipeline pipeline = await pipelineService.UpdatePipeline(id, request, cancellationToken);
            return Http200(MapPipeline(pipeline), Localizer["record.updated"]);
        }
    }
}
