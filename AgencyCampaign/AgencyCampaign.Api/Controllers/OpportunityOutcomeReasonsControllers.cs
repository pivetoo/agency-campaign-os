using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.Opportunities;
using AgencyCampaign.Application.Services;
using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Api.Controllers
{
    [AccessArea("opportunityWinReasons.area")]
    public sealed class OpportunityWinReasonsController : ApiControllerBase
    {
        private readonly IOpportunityWinReasonService service;
        private new readonly IStringLocalizer<AgencyCampaignResource> Localizer;

        public OpportunityWinReasonsController(IOpportunityWinReasonService service, IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.service = service;
            Localizer = localizer;
        }

        [RequireAccess("opportunityWinReasons.get.description")]
        [GetEndpoint]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, [FromQuery] string? search, [FromQuery] bool includeInactive, CancellationToken cancellationToken)
        {
            return Http200(await service.GetAll(request, search, includeInactive, cancellationToken));
        }

        [RequireAccess("opportunityWinReasons.create.description")]
        [PostEndpoint]
        public async Task<IActionResult> Create([FromBody] CreateOpportunityWinReasonRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            var result = await service.Create(request, cancellationToken);
            return Http201(result, Localizer["record.created"]);
        }

        [RequireAccess("opportunityWinReasons.update.description")]
        [PutEndpoint("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateOpportunityWinReasonRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            var result = await service.Update(id, request, cancellationToken);
            return Http200(result, Localizer["record.updated"]);
        }

        [RequireAccess("opportunityWinReasons.delete.description")]
        [DeleteEndpoint("{id:long}")]
        public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
        {
            await service.Delete(id, cancellationToken);
            return Http204();
        }
    }

    [AccessArea("opportunityLossReasons.area")]
    public sealed class OpportunityLossReasonsController : ApiControllerBase
    {
        private readonly IOpportunityLossReasonService service;
        private new readonly IStringLocalizer<AgencyCampaignResource> Localizer;

        public OpportunityLossReasonsController(IOpportunityLossReasonService service, IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.service = service;
            Localizer = localizer;
        }

        [RequireAccess("opportunityLossReasons.get.description")]
        [GetEndpoint]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, [FromQuery] string? search, [FromQuery] bool includeInactive, CancellationToken cancellationToken)
        {
            return Http200(await service.GetAll(request, search, includeInactive, cancellationToken));
        }

        [RequireAccess("opportunityLossReasons.create.description")]
        [PostEndpoint]
        public async Task<IActionResult> Create([FromBody] CreateOpportunityLossReasonRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            var result = await service.Create(request, cancellationToken);
            return Http201(result, Localizer["record.created"]);
        }

        [RequireAccess("opportunityLossReasons.update.description")]
        [PutEndpoint("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateOpportunityLossReasonRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            var result = await service.Update(id, request, cancellationToken);
            return Http200(result, Localizer["record.updated"]);
        }

        [RequireAccess("opportunityLossReasons.delete.description")]
        [DeleteEndpoint("{id:long}")]
        public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
        {
            await service.Delete(id, cancellationToken);
            return Http204();
        }
    }
}
