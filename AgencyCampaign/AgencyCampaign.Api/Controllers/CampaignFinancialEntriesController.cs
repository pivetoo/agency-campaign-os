using AgencyCampaign.Api.Contracts.CampaignFinancialEntries;
using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.CampaignFinancialEntries;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Api.Controllers
{
    public sealed class CampaignFinancialEntriesController : ApiControllerBase
    {
        private readonly ICampaignFinancialEntryService campaignFinancialEntryService;
        private new readonly IStringLocalizer<AgencyCampaignResource> Localizer;
        private static readonly Func<CampaignFinancialEntry, CampaignFinancialEntryContract> MapEntry = CampaignFinancialEntryContract.Projection.Compile();

        public CampaignFinancialEntriesController(ICampaignFinancialEntryService campaignFinancialEntryService, IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.campaignFinancialEntryService = campaignFinancialEntryService;
            Localizer = localizer;
        }

        [RequireAccess("Permite listar os lançamentos financeiros cadastrados.")]
        [GetEndpoint("[action]")]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, CancellationToken cancellationToken)
        {
            PagedResult<CampaignFinancialEntry> result = await campaignFinancialEntryService.GetEntries(request, cancellationToken);
            return Http200(new PagedResult<CampaignFinancialEntryContract>
            {
                Items = result.Items.Select(MapEntry).ToArray(),
                Pagination = result.Pagination
            });
        }

        [RequireAccess("Permite consultar os detalhes de um lançamento financeiro.")]
        [GetEndpoint("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            CampaignFinancialEntry? entry = await campaignFinancialEntryService.GetEntryById(id, cancellationToken);
            return entry is null ? Http404(Localizer["record.notFound"]) : Http200(MapEntry(entry));
        }

        [RequireAccess("Permite listar os lançamentos financeiros vinculados a uma campanha.")]
        [GetEndpoint("campaign/{campaignId:long}")]
        public async Task<IActionResult> GetByCampaign(long campaignId, CancellationToken cancellationToken)
        {
            if (campaignId <= 0)
            {
                return Http400(Localizer["request.id.required"]);
            }

            List<CampaignFinancialEntry> entries = await campaignFinancialEntryService.GetByCampaign(campaignId, cancellationToken);
            return Http200(entries.Select(MapEntry).ToList());
        }

        [RequireAccess("Permite cadastrar um lançamento financeiro.")]
        [PostEndpoint("[action]")]
        public async Task<IActionResult> Create([FromBody] CreateCampaignFinancialEntryRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            CampaignFinancialEntry entry = await campaignFinancialEntryService.CreateEntry(request, cancellationToken);
            return Http201(MapEntry(entry), Localizer["record.created"]);
        }

        [RequireAccess("Permite atualizar um lançamento financeiro.")]
        [PutEndpoint("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateCampaignFinancialEntryRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            CampaignFinancialEntry entry = await campaignFinancialEntryService.UpdateEntry(id, request, cancellationToken);
            return Http200(MapEntry(entry), Localizer["record.updated"]);
        }
    }
}
