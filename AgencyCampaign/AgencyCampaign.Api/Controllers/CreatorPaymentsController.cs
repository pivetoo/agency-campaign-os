using AgencyCampaign.Api.Contracts.CreatorPayments;
using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.CreatorPayments;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
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
    [AccessArea("creatorPayments.area")]
    public sealed class CreatorPaymentsController : ApiControllerBase
    {
        private readonly ICreatorPaymentService creatorPaymentService;
        private readonly WebhookOptions webhookOptions;
        private readonly IMediaAccessTokenService mediaTokens;
        private new readonly IStringLocalizer<AgencyCampaignResource> Localizer;
        private static readonly Func<CreatorPayment, CreatorPaymentContract> MapPaymentRaw = CreatorPaymentContract.Projection.Compile();

        public CreatorPaymentsController(ICreatorPaymentService creatorPaymentService, IOptions<WebhookOptions> webhookOptions, IMediaAccessTokenService mediaTokens, IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.creatorPaymentService = creatorPaymentService;
            this.webhookOptions = webhookOptions.Value;
            this.mediaTokens = mediaTokens;
            Localizer = localizer;
        }

        // Assina a NF (InvoiceUrl) para exibicao quando ela e uma chave de armazenamento privada.
        private CreatorPaymentContract MapPayment(CreatorPayment payment)
        {
            return MapPaymentRaw(payment) with { InvoiceUrl = mediaTokens.ResolveDisplayUrl(payment.InvoiceUrl) };
        }

        [RequireAccess("creatorPayments.get.description")]
        [GetEndpoint]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, CancellationToken cancellationToken)
        {
            PagedResult<CreatorPayment> result = await creatorPaymentService.GetPayments(request, cancellationToken);
            return Http200(new PagedResult<CreatorPaymentContract>
            {
                Items = result.Items.Select(MapPayment).ToArray(),
                Pagination = result.Pagination
            });
        }

        [RequireAccess("creatorPayments.getById.description")]
        [GetEndpoint("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            CreatorPayment? payment = await creatorPaymentService.GetPaymentById(id, cancellationToken);
            return payment is null ? Http404(Localizer["record.notFound"]) : Http200(MapPayment(payment));
        }

        [RequireAccess("creatorPayments.getByCampaign.description")]
        [GetEndpoint("campaign/{campaignId:long}")]
        public async Task<IActionResult> GetByCampaign(long campaignId, CancellationToken cancellationToken)
        {
            List<CreatorPayment> payments = await creatorPaymentService.GetByCampaign(campaignId, cancellationToken);
            return Http200(payments.Select(MapPayment).ToList());
        }

        [RequireAccess("creatorPayments.getByStatus.description")]
        [GetEndpoint("status/{status:int}")]
        public async Task<IActionResult> GetByStatus(int status, CancellationToken cancellationToken)
        {
            List<CreatorPayment> payments = await creatorPaymentService.GetByStatus(status, cancellationToken);
            return Http200(payments.Select(MapPayment).ToList());
        }

        [RequireAccess("creatorPayments.create.description")]
        [PostEndpoint]
        public async Task<IActionResult> Create([FromBody] CreateCreatorPaymentRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            CreatorPayment payment = await creatorPaymentService.CreatePayment(request, cancellationToken);
            return Http201(MapPayment(payment), Localizer["record.created"]);
        }

        [RequireAccess("creatorPayments.update.description")]
        [PutEndpoint("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateCreatorPaymentRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            CreatorPayment payment = await creatorPaymentService.UpdatePayment(id, request, cancellationToken);
            return Http200(MapPayment(payment), Localizer["record.updated"]);
        }

        [RequireAccess("creatorPayments.attachInvoice.description")]
        [PostEndpoint("{id:long}/invoice")]
        public async Task<IActionResult> AttachInvoice(long id, [FromBody] AttachInvoiceRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            CreatorPayment payment = await creatorPaymentService.AttachInvoice(id, request, cancellationToken);
            return Http200(MapPayment(payment), Localizer["record.updated"]);
        }

        [RequireAccess("creatorPayments.markPaid.description")]
        [PostEndpoint("{id:long}/mark-paid")]
        public async Task<IActionResult> MarkPaid(long id, [FromBody] MarkCreatorPaymentPaidRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            CreatorPayment payment = await creatorPaymentService.MarkPaid(id, request, cancellationToken);
            return Http200(MapPayment(payment), Localizer["record.updated"]);
        }

        [RequireAccess("creatorPayments.cancel.description")]
        [PostEndpoint("{id:long}/cancel")]
        public async Task<IActionResult> Cancel(long id, CancellationToken cancellationToken)
        {
            CreatorPayment payment = await creatorPaymentService.Cancel(id, cancellationToken);
            return Http200(MapPayment(payment), Localizer["record.updated"]);
        }

        [RequireAccess("creatorPayments.approve.description")]
        [PostEndpoint("{id:long}/approve")]
        public async Task<IActionResult> Approve(long id, CancellationToken cancellationToken)
        {
            CreatorPayment payment = await creatorPaymentService.ApprovePayment(id, cancellationToken);
            return Http200(MapPayment(payment), Localizer["record.updated"]);
        }

        [RequireAccess("creatorPayments.scheduleBatch.description")]
        [PostEndpoint]
        public async Task<IActionResult> ScheduleBatch([FromBody] SchedulePaymentBatchRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            List<CreatorPayment> payments = await creatorPaymentService.SchedulePaymentBatch(request, cancellationToken);
            return Http200(payments.Select(MapPayment).ToList(), Localizer["record.updated"]);
        }

        // Webhook do provedor de pagamento. O {callbackToken} na URL carrega o prefixo de tenant (PublicLinkToken)
        // e e resolvido pelo PublicTenantResolutionMiddleware ANTES do controller; o pipeline do IntegrationPlatform
        // ecoa nesta URL o callbackToken enviado no payload do agendamento. A verificacao de segredo e por header.
        [AllowAnonymous]
        [PostEndpoint("provider-callback/{callbackToken}")]
        public Task<IActionResult> ProviderCallbackForTenant(string callbackToken, [FromBody] CreatorPaymentProviderCallbackRequest request, CancellationToken cancellationToken)
        {
            _ = callbackToken;
            return HandleProviderCallbackAsync(request, cancellationToken);
        }

        private async Task<IActionResult> HandleProviderCallbackAsync(CreatorPaymentProviderCallbackRequest request, CancellationToken cancellationToken)
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

            CreatorPayment payment = await creatorPaymentService.HandleProviderCallback(request, cancellationToken);
            return Http200(MapPayment(payment), Localizer["record.updated"]);
        }
    }
}
