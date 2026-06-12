using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.ContentLicenses;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.ValueObjects;
using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Api.Controllers
{
    [AccessArea("campaigns.area")]
    public sealed class ContentLicenseController : ApiControllerBase
    {
        private readonly IContentLicenseService service;
        private new readonly IStringLocalizer<AgencyCampaignResource> Localizer;

        public ContentLicenseController(IContentLicenseService service, IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.service = service;
            Localizer = localizer;
        }

        [RequireAccess("campaigns.get.description")]
        [GetEndpoint("list")]
        public async Task<IActionResult> List([FromQuery] PagedRequest request, [FromQuery] int? status, [FromQuery] int? type, [FromQuery] long? campaignId, [FromQuery] string? search, CancellationToken cancellationToken)
        {
            ContentLicenseStatus? typedStatus = status.HasValue ? (ContentLicenseStatus)status.Value : null;
            ContentLicenseType? typedType = type.HasValue ? (ContentLicenseType)type.Value : null;
            return Http200(await service.GetLicenses(request, typedStatus, typedType, campaignId, search, cancellationToken));
        }

        [RequireAccess("campaigns.getById.description")]
        [GetEndpoint("deliverable/{deliverableId:long}")]
        public async Task<IActionResult> GetByDeliverable(long deliverableId, CancellationToken cancellationToken)
        {
            return Http200(await service.GetByDeliverable(deliverableId, cancellationToken));
        }

        [RequireAccess("campaigns.update.description")]
        [PostEndpoint("deliverable/{deliverableId:long}")]
        public async Task<IActionResult> Add(long deliverableId, [FromBody] AddContentLicenseRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            ContentLicenseModel result = await service.Add(deliverableId, request, cancellationToken);
            return Http201(result, Localizer["record.created"]);
        }

        [RequireAccess("campaigns.update.description")]
        [PutEndpoint("{licenseId:long}")]
        public async Task<IActionResult> Update(long licenseId, [FromBody] UpdateContentLicenseRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            ContentLicenseModel result = await service.Update(licenseId, request, cancellationToken);
            return Http200(result, Localizer["record.updated"]);
        }

        [RequireAccess("campaigns.update.description")]
        [DeleteEndpoint("{licenseId:long}")]
        public async Task<IActionResult> Delete(long licenseId, CancellationToken cancellationToken)
        {
            await service.Delete(licenseId, cancellationToken);
            return Http204();
        }

        [RequireAccess("campaigns.update.description")]
        [PostEndpoint("apply-to-campaign/{licenseId:long}")]
        public async Task<IActionResult> ApplyToCampaign(long licenseId, CancellationToken cancellationToken)
        {
            int applied = await service.ApplyToCampaign(licenseId, cancellationToken);
            return Http200(applied);
        }

        [RequireAccess("campaigns.getById.description")]
        [GetEndpoint("expiring")]
        public async Task<IActionResult> GetExpiring([FromQuery] int days = 30, CancellationToken cancellationToken = default)
        {
            return Http200(await service.GetExpiring(days, cancellationToken));
        }
    }
}
