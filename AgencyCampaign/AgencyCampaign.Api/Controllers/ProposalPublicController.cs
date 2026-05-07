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

        public ProposalPublicController(IProposalPublicService publicService)
        {
            this.publicService = publicService;
        }

        [HttpGet("{token}")]
        public async Task<IActionResult> GetByToken(string token, CancellationToken cancellationToken)
        {
            string? ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            string? userAgent = HttpContext.Request.Headers.UserAgent.ToString();

            var result = await publicService.GetByToken(token, ipAddress, userAgent, cancellationToken);
            return result is null ? Http404("Link inválido ou expirado.") : Http200(result);
        }
    }
}
