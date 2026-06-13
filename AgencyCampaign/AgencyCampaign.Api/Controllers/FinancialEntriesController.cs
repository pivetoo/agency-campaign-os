using AgencyCampaign.Api.Contracts.FinancialEntries;
using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.FinancialEntries;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using AgencyCampaign.Infrastructure.Options;
using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace AgencyCampaign.Api.Controllers
{
    [AccessArea("financialEntries.area")]
    public sealed class FinancialEntriesController : ApiControllerBase
    {
        private readonly IFinancialEntryService financialEntryService;
        private readonly WebhookOptions webhookOptions;
        private new readonly IStringLocalizer<AgencyCampaignResource> Localizer;
        private static readonly Func<FinancialEntry, FinancialEntryContract> MapEntry = FinancialEntryContract.Projection.Compile();

        public FinancialEntriesController(IFinancialEntryService financialEntryService, IOptions<WebhookOptions> webhookOptions, IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.financialEntryService = financialEntryService;
            this.webhookOptions = webhookOptions.Value;
            Localizer = localizer;
        }

        [RequireAccess("financialEntries.get.description")]
        [GetEndpoint]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, [FromQuery] FinancialEntryFilters filters, CancellationToken cancellationToken)
        {
            PagedResult<FinancialEntry> result = await financialEntryService.GetEntries(request, filters, cancellationToken);
            return Http200(new PagedResult<FinancialEntryContract>
            {
                Items = result.Items.Select(MapEntry).ToArray(),
                Pagination = result.Pagination
            });
        }

        [RequireAccess("financialEntries.getById.description")]
        [GetEndpoint("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            FinancialEntry? entry = await financialEntryService.GetEntryById(id, cancellationToken);
            return entry is null ? Http404(Localizer["record.notFound"]) : Http200(MapEntry(entry));
        }

        [RequireAccess("financialEntries.getByCampaign.description")]
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

        [RequireAccess("financialEntries.create.description")]
        [PostEndpoint]
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

        [RequireAccess("financialEntries.update.description")]
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

        [RequireAccess("financialEntries.markAsPaid.description")]
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

        [RequireAccess("financialEntries.reverse.description")]
        [PostEndpoint("reverse/{id:long}")]
        public async Task<IActionResult> Reverse(long id, [FromBody] ReverseFinancialEntryRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            ReverseEntryResult result = await financialEntryService.ReverseEntry(id, request, cancellationToken);
            string message = result.CreatorPaymentAlreadyPaid
                ? Localizer["financialEntry.reversedButCreatorPaymentPaid"]
                : Localizer["record.created"];
            return Http200(MapEntry(result.Reversal), message);
        }

        [RequireAccess("financialEntries.getSummary.description")]
        [GetEndpoint("summary/{type:int}")]
        public async Task<IActionResult> GetSummary(int type, CancellationToken cancellationToken)
        {
            FinancialEntryType entryType = (FinancialEntryType)type;
            return Http200(await financialEntryService.GetSummary(entryType, cancellationToken));
        }

        [RequireAccess("financialEntries.createInstallments.description")]
        [PostEndpoint]
        public async Task<IActionResult> CreateInstallments([FromBody] CreateInstallmentSeriesRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            var entries = await financialEntryService.CreateInstallmentSeries(request, cancellationToken);
            return Http201(entries.Select(MapEntry).ToArray(), Localizer["record.created"]);
        }

        // Emite a cobranca (boleto/PIX) do recebivel via IntegrationPlatform.
        [RequireAccess("financialEntries.issueCharge.description")]
        [PostEndpoint("issue-charge/{id:long}")]
        public async Task<IActionResult> IssueCharge(long id, [FromBody] IssueChargeRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            FinancialEntry entry = await financialEntryService.IssueCharge(id, request, cancellationToken);
            return Http200(MapEntry(entry), Localizer["record.updated"]);
        }

        // Webhook do provedor de cobranca (single-tenant fallback, sem token de tenant).
        [AllowAnonymous]
        [PostEndpoint]
        public Task<IActionResult> ProviderCallback([FromBody] FinancialEntryChargeCallbackRequest request, CancellationToken cancellationToken)
        {
            return HandleChargeCallbackAsync(request, cancellationToken);
        }

        // Variante multi-tenant: o {callbackToken} carrega o prefixo de tenant (PublicLinkToken), resolvido pelo
        // PublicTenantResolutionMiddleware ANTES do controller. O pipeline do IntegrationPlatform deve ecoar
        // nesta URL o callbackToken enviado no payload da emissao. A verificacao de segredo e a mesma.
        [AllowAnonymous]
        [PostEndpoint("provider-callback/{callbackToken}")]
        public Task<IActionResult> ProviderCallbackForTenant(string callbackToken, [FromBody] FinancialEntryChargeCallbackRequest request, CancellationToken cancellationToken)
        {
            _ = callbackToken;
            return HandleChargeCallbackAsync(request, cancellationToken);
        }

        private async Task<IActionResult> HandleChargeCallbackAsync(FinancialEntryChargeCallbackRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(webhookOptions.ProviderCallbackSecret))
            {
                return Http403(Localizer["webhook.secret.notConfigured"]);
            }

            string? incomingSecret = Request.Headers["x-webhook-secret"].FirstOrDefault();
            if (!string.Equals(incomingSecret, webhookOptions.ProviderCallbackSecret, StringComparison.Ordinal))
            {
                return Http403(Localizer["webhook.secret.invalid"]);
            }

            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            FinancialEntry entry = await financialEntryService.HandleChargeCallback(request, cancellationToken);
            return Http200(MapEntry(entry), Localizer["record.updated"]);
        }
    }
}
