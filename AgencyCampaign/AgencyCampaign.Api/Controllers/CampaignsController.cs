using AgencyCampaign.Api.Contracts.Campaigns;
using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.Campaigns;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Api.Controllers
{
    [AccessArea("campaigns.area")]
    public sealed class CampaignsController : ApiControllerBase
    {
        private readonly ICampaignService campaignService;
        private new readonly IStringLocalizer<AgencyCampaignResource> Localizer;
        private static readonly Func<Campaign, CampaignContract> MapCampaign = CampaignContract.Projection.Compile();

        public CampaignsController(ICampaignService campaignService, IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.campaignService = campaignService;
            Localizer = localizer;
        }

        [RequireAccess("campaigns.get.description")]
        [GetEndpoint]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, [FromQuery] string? search, [FromQuery] bool includeInactive, CancellationToken cancellationToken)
        {
            PagedResult<Campaign> result = await campaignService.GetCampaigns(request, search, includeInactive, cancellationToken);
            return Http200(new PagedResult<CampaignContract>
            {
                Items = result.Items.Select(MapCampaign).ToArray(),
                Pagination = result.Pagination
            });
        }

        [RequireAccess("campaigns.getById.description")]
        [GetEndpoint("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            Campaign? campaign = await campaignService.GetCampaignById(id, cancellationToken);
            return campaign is null ? Http404(Localizer["record.notFound"]) : Http200(MapCampaign(campaign));
        }

        [RequireAccess("campaigns.getSummary.description")]
        [GetEndpoint("summary/{id:long}")]
        public async Task<IActionResult> GetSummary(long id, CancellationToken cancellationToken)
        {
            var summary = await campaignService.GetSummary(id, cancellationToken);
            return summary is null ? Http404(Localizer["record.notFound"]) : Http200(summary);
        }

        [RequireAccess("campaigns.create.description")]
        [PostEndpoint]
        public async Task<IActionResult> Create([FromBody] CreateCampaignRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            Campaign campaign = await campaignService.CreateCampaign(request, cancellationToken);
            return Http201(MapCampaign(campaign), Localizer["record.created"]);
        }

        [RequireAccess("campaigns.update.description")]
        [PutEndpoint("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateCampaignRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            Campaign campaign = await campaignService.UpdateCampaign(id, request, cancellationToken);
            return Http200(MapCampaign(campaign), Localizer["record.updated"]);
        }

        [RequireAccess("campaigns.getStatusHistory.description")]
        [GetEndpoint("statushistory/{id:long}")]
        public async Task<IActionResult> GetStatusHistory(long id, CancellationToken cancellationToken)
        {
            var history = await campaignService.GetStatusHistory(id, cancellationToken);
            return Http200(history.Select(item => new
            {
                id = item.Id,
                fromStatus = item.FromStatus.HasValue ? (int)item.FromStatus.Value : (int?)null,
                toStatus = (int)item.ToStatus,
                changedAt = item.ChangedAt,
                changedByUserId = item.ChangedByUserId,
                changedByUserName = item.ChangedByUserName,
                reason = item.Reason
            }));
        }
    }
}
