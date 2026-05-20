using AgencyCampaign.Application.Catalogs;
using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.IntegrationCapabilities;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Api.Controllers
{
    [AccessArea("integrationCapabilities.area")]
    public sealed class IntegrationCapabilitiesController : ApiControllerBase
    {
        private readonly IIntegrationCapabilityService capabilityService;
        private new readonly IStringLocalizer<AgencyCampaignResource> Localizer;

        public IntegrationCapabilitiesController(IIntegrationCapabilityService capabilityService, IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.capabilityService = capabilityService;
            Localizer = localizer;
        }

        [RequireAccess("integrationCapabilities.get.description")]
        [GetEndpoint]
        public async Task<IActionResult> Get(CancellationToken cancellationToken)
        {
            IReadOnlyList<IntegrationCapability> result = await capabilityService.GetAll(cancellationToken);
            return Http200(result);
        }

        [RequireAccess("integrationCapabilities.getByIntent.description")]
        [GetEndpoint("by-intent/{intentKey}")]
        public async Task<IActionResult> GetByIntent(string intentKey, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(intentKey))
            {
                return Http400(Localizer["integrationCapability.intent.required"]);
            }

            IntegrationCapability? entity = await capabilityService.GetByIntent(intentKey, cancellationToken);
            return entity is null ? Http404(Localizer["record.notFound"]) : Http200(entity);
        }

        [RequireAccess("integrationCapabilities.getCatalog.description")]
        [GetEndpoint("catalog")]
        public IActionResult GetCatalog()
        {
            IReadOnlyList<IntegrationIntentDescriptor> catalog = capabilityService.GetIntentCatalog();
            return Http200(catalog);
        }

        [RequireAccess("integrationCapabilities.set.description")]
        [PostEndpoint]
        public async Task<IActionResult> Set([FromBody] SetIntegrationCapabilityRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            IntegrationCapability entity = await capabilityService.SetCapability(request, cancellationToken);
            return Http200(entity, Localizer["integrationCapability.saved"]);
        }

        [RequireAccess("integrationCapabilities.remove.description")]
        [DeleteEndpoint("{intentKey}")]
        public async Task<IActionResult> Remove(string intentKey, CancellationToken cancellationToken)
        {
            bool removed = await capabilityService.RemoveCapability(intentKey, cancellationToken);
            if (!removed)
            {
                return Http404(Localizer["record.notFound"]);
            }

            return Http200(true, Localizer["integrationCapability.removed"]);
        }
    }
}
