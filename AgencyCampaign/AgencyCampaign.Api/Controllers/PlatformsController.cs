using AgencyCampaign.Api.Contracts.Platforms;
using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.Platforms;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Api.Controllers
{
    [AccessArea("platforms.area")]
    public sealed class PlatformsController : ApiControllerBase
    {
        private readonly IPlatformService platformService;
        private new readonly IStringLocalizer<AgencyCampaignResource> Localizer;
        private static readonly Func<Platform, PlatformContract> MapPlatform = PlatformContract.Projection.Compile();

        public PlatformsController(IPlatformService platformService, IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.platformService = platformService;
            Localizer = localizer;
        }

        [RequireAccess("platforms.get.description")]
        [GetEndpoint]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, [FromQuery] string? search, [FromQuery] bool includeInactive, CancellationToken cancellationToken)
        {
            PagedResult<Platform> result = await platformService.GetPlatforms(request, search, includeInactive, cancellationToken);
            return Http200(new PagedResult<PlatformContract>
            {
                Items = result.Items.Select(MapPlatform).ToArray(),
                Pagination = result.Pagination
            });
        }

        [RequireAccess("platforms.getById.description")]
        [GetEndpoint("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            Platform? platform = await platformService.GetPlatformById(id, cancellationToken);
            return platform is null ? Http404(Localizer["record.notFound"]) : Http200(MapPlatform(platform));
        }

        [RequireAccess("platforms.getActive.description")]
        [GetEndpoint("active")]
        public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
        {
            List<Platform> platforms = await platformService.GetActivePlatforms(cancellationToken);
            return Http200(platforms.Select(MapPlatform).ToList());
        }

        [RequireAccess("platforms.create.description")]
        [PostEndpoint]
        public async Task<IActionResult> Create([FromBody] CreatePlatformRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            Platform platform = await platformService.CreatePlatform(request, cancellationToken);
            return Http201(MapPlatform(platform), Localizer["record.created"]);
        }

        [RequireAccess("platforms.update.description")]
        [PutEndpoint("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdatePlatformRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            Platform platform = await platformService.UpdatePlatform(id, request, cancellationToken);
            return Http200(MapPlatform(platform), Localizer["record.updated"]);
        }
    }
}
