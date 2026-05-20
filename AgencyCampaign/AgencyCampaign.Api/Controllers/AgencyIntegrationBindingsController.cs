using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.IntegrationBindings;
using AgencyCampaign.Application.Services;
using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Api.Controllers
{
    [AccessArea("integrationBindings.area")]
    public sealed class AgencyIntegrationBindingsController : ApiControllerBase
    {
        private readonly IAgencyIntegrationBindingService service;
        private new readonly IStringLocalizer<AgencyCampaignResource> Localizer;

        public AgencyIntegrationBindingsController(IAgencyIntegrationBindingService service, IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.service = service;
            Localizer = localizer;
        }

        [RequireAccess("integrationBindings.list.description")]
        [GetEndpoint]
        public async Task<IActionResult> List(CancellationToken cancellationToken)
        {
            return Http200(await service.GetAll(cancellationToken));
        }

        [RequireAccess("integrationBindings.getByIntentKey.description")]
        [GetEndpoint("{intentKey}")]
        public async Task<IActionResult> GetByIntentKey(string intentKey, CancellationToken cancellationToken)
        {
            var result = await service.GetByIntentKey(intentKey, cancellationToken);
            return result is null ? Http404(Localizer["record.notFound"]) : Http200(result);
        }

        [RequireAccess("integrationBindings.save.description")]
        [PutEndpoint]
        public async Task<IActionResult> Save([FromBody] SaveAgencyIntegrationBindingRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            var result = await service.Save(request, cancellationToken);
            return Http200(result, Localizer["record.updated"]);
        }

        [RequireAccess("integrationBindings.delete.description")]
        [DeleteEndpoint("{intentKey}")]
        public async Task<IActionResult> Delete(string intentKey, CancellationToken cancellationToken)
        {
            bool deleted = await service.DeleteByIntentKey(intentKey, cancellationToken);
            return deleted ? Http200(Localizer["record.deleted"]) : Http404(Localizer["record.notFound"]);
        }
    }
}
