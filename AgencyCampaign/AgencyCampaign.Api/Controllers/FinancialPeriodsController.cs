using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.FinancialEntries;
using AgencyCampaign.Application.Services;
using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Api.Controllers
{
    [AccessArea("financialPeriods.area")]
    public sealed class FinancialPeriodsController : ApiControllerBase
    {
        private readonly IFinancialPeriodService service;
        private new readonly IStringLocalizer<AgencyCampaignResource> Localizer;

        public FinancialPeriodsController(IFinancialPeriodService service, IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.service = service;
            Localizer = localizer;
        }

        [RequireAccess("financialPeriods.get.description")]
        [GetEndpoint]
        public async Task<IActionResult> Get([FromQuery] int months, CancellationToken cancellationToken)
        {
            int horizon = months <= 0 ? 12 : months;
            var result = await service.GetRecentPeriods(horizon, cancellationToken);
            return Http200(result);
        }

        [RequireAccess("financialPeriods.close.description")]
        [PostEndpoint("close")]
        public async Task<IActionResult> Close([FromBody] FinancialPeriodRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            var result = await service.Close(request.Year, request.Month, cancellationToken);
            return Http200(result, Localizer["record.updated"]);
        }

        [RequireAccess("financialPeriods.reopen.description")]
        [PostEndpoint("reopen")]
        public async Task<IActionResult> Reopen([FromBody] FinancialPeriodRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            var result = await service.Reopen(request.Year, request.Month, cancellationToken);
            return Http200(result, Localizer["record.updated"]);
        }
    }
}
