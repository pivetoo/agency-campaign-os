using AgencyCampaign.Api.Contracts.CampaignCreatorStatuses;
using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.CampaignCreatorStatuses;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Api.Controllers
{
    public sealed class CampaignCreatorStatusesController : ApiControllerBase
    {
        private readonly ICampaignCreatorStatusService statusService;
        private new readonly IStringLocalizer<AgencyCampaignResource> Localizer;
        private static readonly Func<CampaignCreatorStatus, CampaignCreatorStatusContract> MapStatus = CampaignCreatorStatusContract.Projection.Compile();

        public CampaignCreatorStatusesController(ICampaignCreatorStatusService statusService, IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.statusService = statusService;
            Localizer = localizer;
        }

        [RequireAccess("Permite listar os status configurados de creators em campanhas.")]
        [GetEndpoint("[action]")]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, CancellationToken cancellationToken)
        {
            PagedResult<CampaignCreatorStatus> result = await statusService.GetStatuses(request, cancellationToken);
            return Http200(new PagedResult<CampaignCreatorStatusContract>
            {
                Items = result.Items.Select(MapStatus).ToArray(),
                Pagination = result.Pagination
            });
        }

        [RequireAccess("Permite listar os status ativos de creators em campanhas.")]
        [GetEndpoint("active")]
        public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
        {
            List<CampaignCreatorStatus> statuses = await statusService.GetActiveStatuses(cancellationToken);
            return Http200(statuses.Select(MapStatus).ToList());
        }

        [RequireAccess("Permite consultar um status de creator em campanha.")]
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            CampaignCreatorStatus? status = await statusService.GetStatusById(id, cancellationToken);
            return status is null ? Http404(Localizer["record.notFound"]) : Http200(MapStatus(status));
        }

        [RequireAccess("Permite cadastrar um status de creator em campanha.")]
        [PostEndpoint("[action]")]
        public async Task<IActionResult> Create([FromBody] CreateCampaignCreatorStatusRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            CampaignCreatorStatus status = await statusService.CreateStatus(request, cancellationToken);
            return Http201(MapStatus(status), Localizer["record.created"]);
        }

        [RequireAccess("Permite atualizar um status de creator em campanha.")]
        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateCampaignCreatorStatusRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            CampaignCreatorStatus status = await statusService.UpdateStatus(id, request, cancellationToken);
            return Http200(MapStatus(status), Localizer["record.updated"]);
        }
    }
}
