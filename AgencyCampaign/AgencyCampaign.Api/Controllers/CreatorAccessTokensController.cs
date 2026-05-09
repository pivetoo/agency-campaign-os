using AgencyCampaign.Api.Contracts.CreatorAccessTokens;
using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.CreatorAccessTokens;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Api.Controllers
{
    public sealed class CreatorAccessTokensController : ApiControllerBase
    {
        private readonly ICreatorAccessTokenService accessTokenService;
        private new readonly IStringLocalizer<AgencyCampaignResource> Localizer;
        private static readonly Func<CreatorAccessToken, CreatorAccessTokenContract> MapToken = CreatorAccessTokenContract.Projection.Compile();

        public CreatorAccessTokensController(ICreatorAccessTokenService accessTokenService, IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.accessTokenService = accessTokenService;
            Localizer = localizer;
        }

        [RequireAccess("Permite listar os links de acesso ao portal de um creator.")]
        [GetEndpoint("creator/{creatorId:long}")]
        public async Task<IActionResult> GetByCreator(long creatorId, CancellationToken cancellationToken)
        {
            List<CreatorAccessToken> tokens = await accessTokenService.GetByCreator(creatorId, cancellationToken);
            return Http200(tokens.Select(MapToken).ToList());
        }

        [RequireAccess("Permite emitir um novo link de acesso ao portal do creator.")]
        [PostEndpoint("[action]")]
        public async Task<IActionResult> Issue([FromBody] IssueCreatorAccessTokenRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            CreatorAccessToken token = await accessTokenService.Issue(request, cancellationToken);
            return Http201(MapToken(token), Localizer["record.created"]);
        }

        [RequireAccess("Permite revogar um link de acesso ao portal do creator.")]
        [PostEndpoint("{id:long}/revoke")]
        public async Task<IActionResult> Revoke(long id, CancellationToken cancellationToken)
        {
            bool revoked = await accessTokenService.Revoke(id, cancellationToken);
            return revoked ? Http200(new { revoked = true }, Localizer["record.updated"]) : Http404(Localizer["record.notFound"]);
        }
    }
}
