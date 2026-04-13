using AgencyCampaign.Api.Contracts.CampaignDeliverables;
using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.CampaignDeliverables;
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
        private static readonly Func<CampaignDeliverable, CampaignDeliverableContract> MapDeliverable = CampaignDeliverableContract.Projection.Compile();

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
            return Http200(new PagedResult<CampaignDeliverableContract>
            {
                Items = result.Items.Select(MapDeliverable).ToArray(),
                Pagination = result.Pagination
            });
        }

        [RequireAccess("Permite consultar os detalhes de uma entrega de campanha.")]
        [GetEndpoint("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            CampaignDeliverable? deliverable = await campaignDeliverableService.GetDeliverableById(id, cancellationToken);
            return deliverable is null ? Http404(Localizer["record.notFound"]) : Http200(MapDeliverable(deliverable));
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
            return Http200(deliverables.Select(MapDeliverable).ToList());
        }

        [RequireAccess("Permite cadastrar uma nova entrega de campanha.")]
        [PostEndpoint("[action]")]
        public async Task<IActionResult> Create([FromBody] CreateCampaignDeliverableRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            CampaignDeliverable deliverable = await campaignDeliverableService.CreateDeliverable(request, cancellationToken);
            return Http201(MapDeliverable(deliverable), Localizer["record.created"]);
        }

        [RequireAccess("Permite atualizar os dados de uma entrega de campanha.")]
        [PutEndpoint("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateCampaignDeliverableRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            CampaignDeliverable deliverable = await campaignDeliverableService.UpdateDeliverable(id, request, cancellationToken);
            return Http200(MapDeliverable(deliverable), Localizer["record.updated"]);
        }

        [RequireAccess("Permite excluir uma entrega de campanha.")]
        [DeleteEndpoint("{id:long}")]
        public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
        {
            CampaignDeliverable? deliverable = await campaignDeliverableService.Delete(id, cancellationToken);
            return deliverable is null ? Http404(Localizer["record.notFound"]) : Http200(MapDeliverable(deliverable), Localizer["record.deleted"]);
        }
    }
}
