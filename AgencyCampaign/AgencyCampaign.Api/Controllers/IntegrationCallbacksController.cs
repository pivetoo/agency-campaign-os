using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.IntegrationCallbacks;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Infrastructure.Options;
using Archon.Api.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace AgencyCampaign.Api.Controllers
{
    [AllowAnonymous]
    [Route("api/integration-callbacks")]
    public sealed class IntegrationCallbacksController : ApiControllerBase
    {
        private readonly IIntegrationCallbackRouter router;
        private readonly WebhookOptions webhookOptions;
        private new readonly IStringLocalizer<AgencyCampaignResource> Localizer;

        public IntegrationCallbacksController(IIntegrationCallbackRouter router, IOptions<WebhookOptions> webhookOptions, IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.router = router;
            this.webhookOptions = webhookOptions.Value;
            Localizer = localizer;
        }

        [HttpPost]
        public async Task<IActionResult> Receive([FromBody] IntegrationCallbackEnvelope envelope, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(webhookOptions.ProviderCallbackSecret))
            {
                return Http403(Localizer["webhook.secret.notConfigured"]);
            }

            string? incomingToken = Request.Headers["X-Callback-Token"].FirstOrDefault();
            if (!string.Equals(incomingToken, webhookOptions.ProviderCallbackSecret, StringComparison.Ordinal))
            {
                return Http403(Localizer["webhook.secret.invalid"]);
            }

            IActionResult? validationResult = ValidateBody(envelope);
            if (validationResult is not null)
            {
                return validationResult;
            }

            IntegrationCallbackRouterResult result = await router.RouteAsync(envelope, cancellationToken);
            if (!result.Handled)
            {
                return Http200(new { handled = false, detail = result.Detail }, Localizer["integrationCallback.received.unhandled"]);
            }

            return Http200(new { handled = true, detail = result.Detail }, Localizer["integrationCallback.handled"]);
        }
    }
}
