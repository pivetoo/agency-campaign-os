using AgencyCampaign.Api.Contracts.FinancialEntries;
using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.FinancialEntries;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Api.Controllers
{
    public sealed class FinancialEntriesController : ApiControllerBase
    {
        private readonly IFinancialEntryService financialEntryService;
        private new readonly IStringLocalizer<AgencyCampaignResource> Localizer;
        private static readonly Func<FinancialEntry, FinancialEntryContract> MapEntry = FinancialEntryContract.Projection.Compile();

        public FinancialEntriesController(IFinancialEntryService financialEntryService, IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.financialEntryService = financialEntryService;
            Localizer = localizer;
        }

        [RequireAccess("Permite listar os lançamentos financeiros cadastrados.")]
        [GetEndpoint("[action]")]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, [FromQuery] FinancialEntryFilters filters, CancellationToken cancellationToken)
        {
            PagedResult<FinancialEntry> result = await financialEntryService.GetEntries(request, filters, cancellationToken);
            return Http200(new PagedResult<FinancialEntryContract>
            {
                Items = result.Items.Select(MapEntry).ToArray(),
                Pagination = result.Pagination
            });
        }

        [RequireAccess("Permite consultar os detalhes de um lançamento financeiro.")]
        [GetEndpoint("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            FinancialEntry? entry = await financialEntryService.GetEntryById(id, cancellationToken);
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

            List<FinancialEntry> entries = await financialEntryService.GetByCampaign(campaignId, cancellationToken);
            return Http200(entries.Select(MapEntry).ToList());
        }

        [RequireAccess("Permite cadastrar um lançamento financeiro.")]
        [PostEndpoint("[action]")]
        public async Task<IActionResult> Create([FromBody] CreateFinancialEntryRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            FinancialEntry entry = await financialEntryService.CreateEntry(request, cancellationToken);
            return Http201(MapEntry(entry), Localizer["record.created"]);
        }

        [RequireAccess("Permite atualizar um lançamento financeiro.")]
        [PutEndpoint("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateFinancialEntryRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            FinancialEntry entry = await financialEntryService.UpdateEntry(id, request, cancellationToken);
            return Http200(MapEntry(entry), Localizer["record.updated"]);
        }

        [RequireAccess("Permite marcar um lançamento financeiro como pago/recebido.")]
        [PostEndpoint("markaspaid/{id:long}")]
        public async Task<IActionResult> MarkAsPaid(long id, [FromBody] MarkAsPaidRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            FinancialEntry entry = await financialEntryService.MarkAsPaid(id, request, cancellationToken);
            return Http200(MapEntry(entry), Localizer["record.updated"]);
        }

        [RequireAccess("Permite consultar o resumo financeiro por tipo (a receber/a pagar).")]
        [GetEndpoint("summary/{type:int}")]
        public async Task<IActionResult> GetSummary(int type, CancellationToken cancellationToken)
        {
            FinancialEntryType entryType = (FinancialEntryType)type;
            return Http200(await financialEntryService.GetSummary(entryType, cancellationToken));
        }
    }
}
