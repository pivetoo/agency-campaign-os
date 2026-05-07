using AgencyCampaign.Application.Requests.DeliverableShareLinks;
using AgencyCampaign.Application.Services;
using Archon.Api.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgencyCampaign.Api.Controllers
{
    [AllowAnonymous]
    [Route("api/deliverable-public")]
    public sealed class DeliverablePublicController : ApiControllerBase
    {
        private readonly IDeliverablePublicService publicService;

        public DeliverablePublicController(IDeliverablePublicService publicService)
        {
            this.publicService = publicService;
        }

        [HttpGet("{token}")]
        public async Task<IActionResult> GetByToken(string token, CancellationToken cancellationToken)
        {
            var result = await publicService.GetByToken(token, cancellationToken);
            return result is null ? Http404("Link inválido ou expirado.") : Http200(result);
        }

        [HttpPost("{token}/approve")]
        public async Task<IActionResult> Approve(string token, [FromBody] PublicDeliverableDecisionRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            var result = await publicService.Approve(token, request, cancellationToken);
            return Http200(result);
        }

        [HttpPost("{token}/reject")]
        public async Task<IActionResult> Reject(string token, [FromBody] PublicDeliverableDecisionRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            var result = await publicService.Reject(token, request, cancellationToken);
            return Http200(result);
        }
    }
}
