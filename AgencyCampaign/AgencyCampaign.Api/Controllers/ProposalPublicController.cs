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
    }
}
