using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.BrandContacts;
using AgencyCampaign.Application.Services;
using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Api.Controllers
{
    [AccessArea("brands.area")]
    public sealed class BrandContactController : ApiControllerBase
    {
        private readonly IBrandContactService service;
        private new readonly IStringLocalizer<AgencyCampaignResource> Localizer;

        public BrandContactController(IBrandContactService service, IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.service = service;
            Localizer = localizer;
        }

        [RequireAccess("brands.getById.description")]
        [GetEndpoint("brand/{brandId:long}")]
        public async Task<IActionResult> GetByBrand(long brandId, CancellationToken cancellationToken)
        {
            return Http200(await service.GetByBrand(brandId, cancellationToken));
        }

        [RequireAccess("brands.update.description")]
        [PostEndpoint("brand/{brandId:long}")]
        public async Task<IActionResult> Add(long brandId, [FromBody] AddBrandContactRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            BrandContactModel result = await service.Add(brandId, request, cancellationToken);
            return Http201(result, Localizer["record.created"]);
        }

        [RequireAccess("brands.update.description")]
        [PutEndpoint("{contactId:long}")]
        public async Task<IActionResult> Update(long contactId, [FromBody] UpdateBrandContactRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            BrandContactModel result = await service.Update(contactId, request, cancellationToken);
            return Http200(result, Localizer["record.updated"]);
        }

        [RequireAccess("brands.update.description")]
        [DeleteEndpoint("{contactId:long}")]
        public async Task<IActionResult> Delete(long contactId, CancellationToken cancellationToken)
        {
            await service.Delete(contactId, cancellationToken);
            return Http204();
        }

        [RequireAccess("brands.update.description")]
        [PostEndpoint("primary/{contactId:long}")]
        public async Task<IActionResult> SetPrimary(long contactId, CancellationToken cancellationToken)
        {
            BrandContactModel result = await service.SetPrimary(contactId, cancellationToken);
            return Http200(result, Localizer["record.updated"]);
        }
    }
}
