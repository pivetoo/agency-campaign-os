using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.Commercial;
using AgencyCampaign.Application.Services;
using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Api.Controllers
{
    [AccessArea("commercialPolicy.area")]
    public sealed class CommercialPolicyController : ApiControllerBase
    {
        private readonly ICommercialPolicyService service;
        private new readonly IStringLocalizer<AgencyCampaignResource> Localizer;

        public CommercialPolicyController(ICommercialPolicyService service, IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.service = service;
            Localizer = localizer;
        }

        [RequireAccess("commercialPolicy.get.description")]
        [GetEndpoint]
        public async Task<IActionResult> Get(CancellationToken cancellationToken)
        {
            return Http200(await service.GetCurrent(cancellationToken));
        }

        [RequireAccess("commercialPolicy.upsert.description")]
        [PutEndpoint]
        public async Task<IActionResult> Upsert([FromBody] UpsertCommercialPolicyRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            var result = await service.Upsert(request, cancellationToken);
            return Http200(result, Localizer["record.updated"]);
        }
    }
}
