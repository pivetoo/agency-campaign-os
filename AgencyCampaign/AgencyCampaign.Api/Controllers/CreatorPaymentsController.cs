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
    public sealed class CreatorPaymentsController : ApiControllerBase
    {
        private readonly ICreatorPaymentService creatorPaymentService;
        private readonly WebhookOptions webhookOptions;
        private new readonly IStringLocalizer<AgencyCampaignResource> Localizer;
        private static readonly Func<CreatorPayment, CreatorPaymentContract> MapPayment = CreatorPaymentContract.Projection.Compile();

        public CreatorPaymentsController(ICreatorPaymentService creatorPaymentService, IOptions<WebhookOptions> webhookOptions, IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.creatorPaymentService = creatorPaymentService;
            this.webhookOptions = webhookOptions.Value;
            Localizer = localizer;
        }

        [RequireAccess("Permite listar os pagamentos de creators.")]
        [GetEndpoint("[action]")]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, CancellationToken cancellationToken)
        {
            PagedResult<CreatorPayment> result = await creatorPaymentService.GetPayments(request, cancellationToken);
            return Http200(new PagedResult<CreatorPaymentContract>
            {
                Items = result.Items.Select(MapPayment).ToArray(),
                Pagination = result.Pagination
            });
        }

        [RequireAccess("Permite consultar os detalhes de um pagamento.")]
        [GetEndpoint("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            CreatorPayment? payment = await creatorPaymentService.GetPaymentById(id, cancellationToken);
            return payment is null ? Http404(Localizer["record.notFound"]) : Http200(MapPayment(payment));
        }

        [RequireAccess("Permite listar pagamentos por campanha.")]
        [GetEndpoint("campaign/{campaignId:long}")]
        public async Task<IActionResult> GetByCampaign(long campaignId, CancellationToken cancellationToken)
        {
            List<CreatorPayment> payments = await creatorPaymentService.GetByCampaign(campaignId, cancellationToken);
            return Http200(payments.Select(MapPayment).ToList());
        }

        [RequireAccess("Permite listar pagamentos por status.")]
        [GetEndpoint("status/{status:int}")]
        public async Task<IActionResult> GetByStatus(int status, CancellationToken cancellationToken)
        {
            List<CreatorPayment> payments = await creatorPaymentService.GetByStatus(status, cancellationToken);
            return Http200(payments.Select(MapPayment).ToList());
        }

        [RequireAccess("Permite registrar um pagamento a creator.")]
        [PostEndpoint("[action]")]
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

        [RequireAccess("Permite atualizar dados de um pagamento.")]
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

        [RequireAccess("Permite anexar nota fiscal a um pagamento.")]
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

        [RequireAccess("Permite marcar um pagamento como pago.")]
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

        [RequireAccess("Permite cancelar um pagamento.")]
        [PostEndpoint("{id:long}/cancel")]
        public async Task<IActionResult> Cancel(long id, CancellationToken cancellationToken)
        {
            CreatorPayment payment = await creatorPaymentService.Cancel(id, cancellationToken);
            return Http200(MapPayment(payment), Localizer["record.updated"]);
        }

        [RequireAccess("Permite agendar um lote de pagamentos via gateway.")]
        [PostEndpoint("[action]")]
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

        [AllowAnonymous]
        [PostEndpoint("[action]")]
        public async Task<IActionResult> ProviderCallback([FromBody] CreatorPaymentProviderCallbackRequest request, CancellationToken cancellationToken)
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
