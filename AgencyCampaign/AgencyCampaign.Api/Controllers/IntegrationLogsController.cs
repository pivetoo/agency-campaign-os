using AgencyCampaign.Api.Contracts.IntegrationLogs;
using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Api.Controllers
{
    public sealed class IntegrationLogsController : ApiControllerBase
    {
        private readonly IIntegrationLogService logService;
        private new readonly IStringLocalizer<AgencyCampaignResource> Localizer;
        private static readonly Func<IntegrationLog, IntegrationLogContract> MapLog = IntegrationLogContract.Projection.Compile();

        public IntegrationLogsController(IIntegrationLogService logService, IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.logService = logService;
            Localizer = localizer;
        }

        [RequireAccess("Permite listar os logs de execucao de integracao.")]
        [GetEndpoint("[action]")]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, CancellationToken cancellationToken)
        {
            PagedResult<IntegrationLog> result = await logService.GetLogs(request, cancellationToken);
            return Http200(new PagedResult<IntegrationLogContract>
            {
                Items = result.Items.Select(MapLog).ToArray(),
                Pagination = result.Pagination
            });
        }

        [RequireAccess("Permite consultar os detalhes de um log de execucao.")]
        [GetEndpoint("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            IntegrationLog? log = await logService.GetLogById(id, cancellationToken);
            return log is null ? Http404(Localizer["record.notFound"]) : Http200(MapLog(log));
        }

        [RequireAccess("Permite listar os logs de execucao por pipeline.")]
        [GetEndpoint("by-pipeline/{integrationPipelineId:long}")]
        public async Task<IActionResult> GetByPipeline(long integrationPipelineId, CancellationToken cancellationToken)
        {
            List<IntegrationLog> logs = await logService.GetLogsByPipeline(integrationPipelineId, cancellationToken);
            return Http200(logs.Select(MapLog).ToList());
        }
    }
}
