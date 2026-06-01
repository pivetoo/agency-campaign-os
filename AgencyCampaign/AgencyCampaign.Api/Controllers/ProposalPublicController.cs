using AgencyCampaign.Application.Models.Commercial;
using AgencyCampaign.Application.Requests.Proposals;
using AgencyCampaign.Application.Services;
using Archon.Api.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgencyCampaign.Api.Controllers
{
    [AllowAnonymous]
    [Route("api/proposal-public")]
    public sealed class ProposalPublicController : ApiControllerBase
    {
        private readonly IProposalPublicService publicService;
        private readonly IProposalPdfService pdfService;

        public ProposalPublicController(IProposalPublicService publicService, IProposalPdfService pdfService)
        {
            this.publicService = publicService;
            this.pdfService = pdfService;
        }

        [HttpGet("{token}")]
        public async Task<IActionResult> GetByToken(string token, CancellationToken cancellationToken)
        {
            string? ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            string? userAgent = HttpContext.Request.Headers.UserAgent.ToString();

            var result = await publicService.GetByToken(token, ipAddress, userAgent, cancellationToken);
            return result is null ? Http404("Link inválido ou expirado.") : Http200(result);
        }

        [HttpGet("{token}/pdf")]
        public async Task<IActionResult> GetPdfByToken(string token, CancellationToken cancellationToken)
        {
            byte[]? bytes = await pdfService.GenerateForShareTokenAsync(token, cancellationToken);
            return bytes is null ? Http404("Link inválido ou expirado.") : File(bytes, "application/pdf", $"proposta-{token}.pdf");
        }

        [HttpPost("{token}/accept")]
        public Task<IActionResult> Accept(string token, [FromBody] ProposalClientDecisionRequest request, CancellationToken cancellationToken)
        {
            return Decide(token, accept: true, request, cancellationToken);
        }

        [HttpPost("{token}/reject")]
        public Task<IActionResult> Reject(string token, [FromBody] ProposalClientDecisionRequest request, CancellationToken cancellationToken)
        {
            return Decide(token, accept: false, request, cancellationToken);
        }

        private async Task<IActionResult> Decide(string token, bool accept, ProposalClientDecisionRequest request, CancellationToken cancellationToken)
        {
            ProposalClientDecisionResult result = await publicService.RegisterClientDecision(
                token, accept, request?.Name ?? string.Empty, request?.Email, request?.Notes, cancellationToken);

            return result switch
            {
                ProposalClientDecisionResult.Success => Http200(new { ok = true }),
                ProposalClientDecisionResult.NotFound => Http404("Link inválido ou expirado."),
                ProposalClientDecisionResult.AlreadyDecided => Http409("Esta proposta já recebeu uma decisão."),
                _ => Http400("Informe seu nome para confirmar a decisão."),
            };
        }
    }
}
