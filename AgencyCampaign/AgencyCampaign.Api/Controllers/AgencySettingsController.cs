using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.AgencySettings;
using AgencyCampaign.Application.Services;
using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Api.Controllers
{
    public sealed class AgencySettingsController : ApiControllerBase
    {
        private readonly IAgencySettingsService service;
        private new readonly IStringLocalizer<AgencyCampaignResource> Localizer;

        public AgencySettingsController(IAgencySettingsService service, IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.service = service;
            Localizer = localizer;
        }

        [RequireAccess("Permite consultar as configurações da agência.")]
        [GetEndpoint("[action]")]
        public async Task<IActionResult> Get(CancellationToken cancellationToken)
        {
            return Http200(await service.Get(cancellationToken));
        }

        [RequireAccess("Permite atualizar as configurações da agência.")]
        [PutEndpoint("[action]")]
        public async Task<IActionResult> Update([FromBody] UpdateAgencySettingsRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            var result = await service.Update(request, cancellationToken);
            return Http200(result, Localizer["record.updated"]);
        }
    }
}
