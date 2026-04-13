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
    public sealed class CampaignDeliverablesController : ApiControllerBase
    {
        private readonly ICampaignDeliverableService campaignDeliverableService;
        private new readonly IStringLocalizer<AgencyCampaignResource> Localizer;

        public CampaignDeliverablesController(ICampaignDeliverableService campaignDeliverableService, IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.campaignDeliverableService = campaignDeliverableService;
            Localizer = localizer;
        }

        [RequireAccess("Permite listar as entregas de campanha cadastradas.")]
        [GetEndpoint("[action]")]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, CancellationToken cancellationToken)
        {
            PagedResult<CampaignDeliverable> result = await campaignDeliverableService.GetDeliverables(request, cancellationToken);
            return Http200(result);
        }

        [RequireAccess("Permite consultar os detalhes de uma entrega de campanha.")]
        [GetEndpoint("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            CampaignDeliverable? deliverable = await campaignDeliverableService.GetDeliverableById(id, cancellationToken);
            return deliverable is null ? Http404(Localizer["record.notFound"]) : Http200(deliverable);
        }

        [RequireAccess("Permite listar as entregas vinculadas a uma campanha.")]
        [GetEndpoint("campaign/{campaignId:long}")]
        public async Task<IActionResult> GetByCampaign(long campaignId, CancellationToken cancellationToken)
        {
            if (campaignId <= 0)
            {
                return Http400(Localizer["request.id.required"]);
            }

            List<CampaignDeliverable> deliverables = await campaignDeliverableService.GetByCampaign(campaignId, cancellationToken);
            return Http200(deliverables);
        }
    }
}
