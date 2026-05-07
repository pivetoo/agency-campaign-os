using AgencyCampaign.Api.Contracts.CampaignCreators;
using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.CampaignCreators;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Api.Controllers
{
    public sealed class CampaignCreatorsController : ApiControllerBase
    {
        private readonly ICampaignCreatorService campaignCreatorService;
        private new readonly IStringLocalizer<AgencyCampaignResource> Localizer;
        private static readonly Func<CampaignCreator, CampaignCreatorContract> MapCampaignCreator = CampaignCreatorContract.Projection.Compile();

        public CampaignCreatorsController(ICampaignCreatorService campaignCreatorService, IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.campaignCreatorService = campaignCreatorService;
            Localizer = localizer;
        }

        [RequireAccess("Permite listar os creators vinculados às campanhas.")]
        [GetEndpoint("[action]")]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, CancellationToken cancellationToken)
        {
            PagedResult<CampaignCreator> result = await campaignCreatorService.GetCampaignCreators(request, cancellationToken);
            return Http200(new PagedResult<CampaignCreatorContract>
            {
                Items = result.Items.Select(MapCampaignCreator).ToArray(),
                Pagination = result.Pagination
            });
        }

        [RequireAccess("Permite consultar os detalhes de um vínculo entre campanha e creator.")]
        [GetEndpoint("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            CampaignCreator? campaignCreator = await campaignCreatorService.GetCampaignCreatorById(id, cancellationToken);
            return campaignCreator is null ? Http404(Localizer["record.notFound"]) : Http200(MapCampaignCreator(campaignCreator));
        }

        [RequireAccess("Permite listar os creators vinculados a uma campanha.")]
        [GetEndpoint("campaign/{campaignId:long}")]
        public async Task<IActionResult> GetByCampaign(long campaignId, CancellationToken cancellationToken)
        {
            if (campaignId <= 0)
            {
                return Http400(Localizer["request.id.required"]);
            }

            List<CampaignCreator> campaignCreators = await campaignCreatorService.GetByCampaign(campaignId, cancellationToken);
            return Http200(campaignCreators.Select(MapCampaignCreator).ToList());
        }

        [RequireAccess("Permite vincular um creator a uma campanha.")]
        [PostEndpoint("[action]")]
        public async Task<IActionResult> Create([FromBody] CreateCampaignCreatorRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            CampaignCreator campaignCreator = await campaignCreatorService.CreateCampaignCreator(request, cancellationToken);
            return Http201(MapCampaignCreator(campaignCreator), Localizer["record.created"]);
        }

        [RequireAccess("Permite atualizar o vínculo entre campanha e creator.")]
        [PutEndpoint("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateCampaignCreatorRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            CampaignCreator campaignCreator = await campaignCreatorService.UpdateCampaignCreator(id, request, cancellationToken);
            return Http200(MapCampaignCreator(campaignCreator), Localizer["record.updated"]);
        }

        [RequireAccess("Permite consultar o histórico de mudanças de status de um creator na campanha.")]
        [GetEndpoint("statushistory/{id:long}")]
        public async Task<IActionResult> GetStatusHistory(long id, CancellationToken cancellationToken)
        {
            var history = await campaignCreatorService.GetStatusHistory(id, cancellationToken);
            return Http200(history.Select(item => new
            {
                id = item.Id,
                fromStatusId = item.FromStatusId,
                fromStatusName = item.FromStatus?.Name,
                fromStatusColor = item.FromStatus?.Color,
                toStatusId = item.ToStatusId,
                toStatusName = item.ToStatus?.Name,
                toStatusColor = item.ToStatus?.Color,
                changedAt = item.ChangedAt,
                changedByUserId = item.ChangedByUserId,
                changedByUserName = item.ChangedByUserName,
                reason = item.Reason
            }));
        }
    }
}
