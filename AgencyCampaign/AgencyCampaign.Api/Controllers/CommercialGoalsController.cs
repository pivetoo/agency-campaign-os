using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.Commercial;
using AgencyCampaign.Application.Services;
using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Api.Controllers
{
    [AccessArea("commercialGoals.area")]
    public sealed class CommercialGoalsController : ApiControllerBase
    {
        private readonly ICommercialGoalService service;
        private new readonly IStringLocalizer<AgencyCampaignResource> Localizer;

        public CommercialGoalsController(ICommercialGoalService service, IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.service = service;
            Localizer = localizer;
        }

        [RequireAccess("commercialGoals.get.description")]
        [GetEndpoint]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, [FromQuery] bool includeInactive, [FromQuery] long? userId, [FromQuery] int? periodType, CancellationToken cancellationToken)
        {
            return Http200(await service.GetAll(request, includeInactive, userId, periodType, cancellationToken));
        }

        [RequireAccess("commercialGoals.progress.description")]
        [GetEndpoint]
        public async Task<IActionResult> Progress([FromQuery] DateTimeOffset? referenceDate, [FromQuery] long? userId, [FromQuery] int? periodType, CancellationToken cancellationToken)
        {
            DateTimeOffset reference = referenceDate ?? DateTimeOffset.UtcNow;
            return Http200(await service.GetProgress(reference, userId, periodType, cancellationToken));
        }

        [RequireAccess("commercialGoals.create.description")]
        [PostEndpoint]
        public async Task<IActionResult> Create([FromBody] CreateCommercialGoalRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            var result = await service.Create(request, cancellationToken);
            return Http201(result, Localizer["record.created"]);
        }

        [RequireAccess("commercialGoals.update.description")]
        [PutEndpoint("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateCommercialGoalRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            var result = await service.Update(id, request, cancellationToken);
            return Http200(result, Localizer["record.updated"]);
        }

        [RequireAccess("commercialGoals.delete.description")]
        [DeleteEndpoint("{id:long}")]
        public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
        {
            await service.Delete(id, cancellationToken);
            return Http204();
        }
    }
}
