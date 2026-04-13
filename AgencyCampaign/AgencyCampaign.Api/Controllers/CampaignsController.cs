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
    public sealed class CampaignsController : ApiControllerBase
    {
        private readonly ICampaignService campaignService;
        private new readonly IStringLocalizer<AgencyCampaignResource> Localizer;

        public CampaignsController(ICampaignService campaignService, IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.campaignService = campaignService;
            Localizer = localizer;
        }

        [RequireAccess("Permite listar as campanhas cadastradas.")]
        [GetEndpoint("[action]")]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, CancellationToken cancellationToken)
        {
            PagedResult<Campaign> result = await campaignService.GetCampaigns(request, cancellationToken);
            return Http200(result);
        }

        [RequireAccess("Permite consultar os detalhes de uma campanha.")]
        [GetEndpoint("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            Campaign? campaign = await campaignService.GetCampaignById(id, cancellationToken);
            return campaign is null ? Http404(Localizer["record.notFound"]) : Http200(campaign);
        }

        [RequireAccess("Permite consultar o resumo de uma campanha.")]
        [GetEndpoint("summary/{id:long}")]
        public async Task<IActionResult> GetSummary(long id, CancellationToken cancellationToken)
        {
            var summary = await campaignService.GetSummary(id, cancellationToken);
            return summary is null ? Http404(Localizer["record.notFound"]) : Http200(summary);
        }

        [RequireAccess("Permite cadastrar uma nova campanha.")]
        [PostEndpoint("[action]")]
        public async Task<IActionResult> Create([FromBody] CreateCampaignRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            Campaign campaign = await campaignService.CreateCampaign(request, cancellationToken);
            return Http201(campaign, Localizer["record.created"]);
        }

        [RequireAccess("Permite atualizar os dados de uma campanha.")]
        [PutEndpoint("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateCampaignRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            Campaign campaign = await campaignService.UpdateCampaign(id, request, cancellationToken);
            return Http200(campaign, Localizer["record.updated"]);
        }
    }
}
