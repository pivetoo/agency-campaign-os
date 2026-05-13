using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.WhatsApp;
using AgencyCampaign.Application.Services;
using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Api.Controllers
{
    [Route("api/whatsapp")]
    public sealed class WhatsAppWebhookController : ApiControllerBase
    {
        private readonly IWhatsAppService service;
        private readonly IAgencySettingsService settingsService;
        private new readonly IStringLocalizer<AgencyCampaignResource> Localizer;

        public WhatsAppWebhookController(IWhatsAppService service, IAgencySettingsService settingsService, IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.service = service;
            this.settingsService = settingsService;
            Localizer = localizer;
        }

        [RequireAccess("Permite receber mensagens do webhook WhatsApp via IntegrationPlatform.")]
        [HttpPost("webhook")]
        public async Task<IActionResult> Receive([FromBody] ReceiveWhatsAppWebhookRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            var settings = await settingsService.Get(cancellationToken);
            var conversation = await service.ReceiveInboundMessage(request, settings.WhatsAppConnectorId, cancellationToken);
            return Http200(conversation);
        }
    }
}
