using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.WhatsApp;
using AgencyCampaign.Application.Services;
using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Api.Controllers
{
    public sealed class WhatsAppConversationsController : ApiControllerBase
    {
        private readonly IWhatsAppService service;
        private new readonly IStringLocalizer<AgencyCampaignResource> Localizer;

        public WhatsAppConversationsController(IWhatsAppService service, IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.service = service;
            Localizer = localizer;
        }

        [RequireAccess("Permite listar conversas do inbox WhatsApp.")]
        [GetEndpoint("[action]")]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, CancellationToken cancellationToken)
        {
            var result = await service.GetConversations(request, cancellationToken);
            return Http200(result);
        }

        [RequireAccess("Permite consultar mensagens de uma conversa WhatsApp.")]
        [GetEndpoint("{id:long}/[action]")]
        public async Task<IActionResult> Messages(long id, CancellationToken cancellationToken)
        {
            var messages = await service.GetMessages(id, cancellationToken);
            return Http200(messages);
        }

        [RequireAccess("Permite enviar mensagem WhatsApp em uma conversa.")]
        [PostEndpoint("{id:long}/[action]")]
        public async Task<IActionResult> Send(long id, [FromBody] SendWhatsAppMessageRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            var message = await service.SendMessage(id, request, cancellationToken);
            return Http201(message, Localizer["record.created"]);
        }

        [RequireAccess("Permite marcar conversa WhatsApp como lida.")]
        [PutEndpoint("{id:long}/[action]")]
        public async Task<IActionResult> Read(long id, CancellationToken cancellationToken)
        {
            await service.MarkAsRead(id, cancellationToken);
            return Http200(Localizer["record.updated"]);
        }
    }
}
