using AgencyCampaign.Api.Contracts.CampaignDocuments;
using AgencyCampaign.Api.Contracts.CreatorPayments;
using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.CreatorPortal;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Archon.Api.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Api.Controllers
{
    [AllowAnonymous]
    [Route("api/creatorportal")]
    public sealed class CreatorPortalController : ApiControllerBase
    {
        private readonly ICreatorPortalService portalService;
        private new IStringLocalizer<AgencyCampaignResource> Localizer { get; }
        private static readonly Func<CampaignDocument, CampaignDocumentContract> MapDocument = CampaignDocumentContract.Projection.Compile();
        private static readonly Func<CreatorPayment, CreatorPaymentContract> MapPayment = CreatorPaymentContract.Projection.Compile();

        public CreatorPortalController(ICreatorPortalService portalService, IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.portalService = portalService;
            Localizer = localizer;
        }

        [HttpGet("{token}/me")]
        public async Task<IActionResult> Me(string token, CancellationToken cancellationToken)
        {
            try
            {
                CreatorPortalContext ctx = await portalService.ResolveContext(token, cancellationToken);
                return Http200(new
                {
                    creator = new
                    {
                        ctx.Creator.Id,
                        ctx.Creator.Name,
                        ctx.Creator.StageName,
                        ctx.Creator.Email,
                        ctx.Creator.Phone,
                        ctx.Creator.Document,
                        ctx.Creator.PixKey,
                        ctx.Creator.PixKeyType,
                        ctx.Creator.PrimaryNiche,
                    },
                    token = new
                    {
                        ctx.Token.ExpiresAt,
                        ctx.Token.UsageCount,
                        ctx.Token.LastUsedAt,
                    },
                });
            }
            catch (InvalidOperationException ex)
            {
                return Http401(ex.Message);
            }
        }

        [HttpGet("{token}/campaigns")]
        public async Task<IActionResult> Campaigns(string token, CancellationToken cancellationToken)
        {
            try
            {
                CreatorPortalContext ctx = await portalService.ResolveContext(token, cancellationToken);
                List<CampaignCreator> campaigns = await portalService.GetCampaigns(ctx.Creator.Id, cancellationToken);
                return Http200(campaigns.Select(item => new
                {
                    item.Id,
                    item.CampaignId,
                    CampaignName = item.Campaign?.Name,
                    BrandName = item.Campaign?.Brand?.Name,
                    StatusName = item.CampaignCreatorStatus?.Name,
                    StatusColor = item.CampaignCreatorStatus?.Color,
                    item.AgreedAmount,
                    item.AgencyFeePercent,
                    item.Notes,
                    StartsAt = item.Campaign?.StartsAt,
                    EndsAt = item.Campaign?.EndsAt,
                }).ToList());
            }
            catch (InvalidOperationException ex)
            {
                return Http401(ex.Message);
            }
        }

        [HttpGet("{token}/documents")]
        public async Task<IActionResult> Documents(string token, CancellationToken cancellationToken)
        {
            try
            {
                CreatorPortalContext ctx = await portalService.ResolveContext(token, cancellationToken);
                List<CampaignDocument> documents = await portalService.GetDocuments(ctx.Creator.Id, cancellationToken);
                return Http200(documents.Select(MapDocument).ToList());
            }
            catch (InvalidOperationException ex)
            {
                return Http401(ex.Message);
            }
        }

        [HttpGet("{token}/payments")]
        public async Task<IActionResult> Payments(string token, CancellationToken cancellationToken)
        {
            try
            {
                CreatorPortalContext ctx = await portalService.ResolveContext(token, cancellationToken);
                List<CreatorPayment> payments = await portalService.GetPayments(ctx.Creator.Id, cancellationToken);
                return Http200(payments.Select(MapPayment).ToList());
            }
            catch (InvalidOperationException ex)
            {
                return Http401(ex.Message);
            }
        }

        [HttpPost("{token}/bank-info")]
        public async Task<IActionResult> UpdateBankInfo(string token, [FromBody] UpdateCreatorBankInfoRequest request, CancellationToken cancellationToken)
        {
            try
            {
                CreatorPortalContext ctx = await portalService.ResolveContext(token, cancellationToken);

                IActionResult? validationResult = ValidateBody(request);
                if (validationResult is not null)
                {
                    return validationResult;
                }

                Creator creator = await portalService.UpdateBankInfo(ctx.Creator.Id, request, cancellationToken);
                return Http200(new
                {
                    creator.PixKey,
                    creator.PixKeyType,
                    creator.Document,
                }, Localizer["record.updated"]);
            }
            catch (InvalidOperationException ex)
            {
                return Http401(ex.Message);
            }
        }

        [HttpPost("{token}/invoice")]
        public async Task<IActionResult> UploadInvoice(string token, [FromBody] UploadInvoiceRequest request, CancellationToken cancellationToken)
        {
            try
            {
                CreatorPortalContext ctx = await portalService.ResolveContext(token, cancellationToken);

                IActionResult? validationResult = ValidateBody(request);
                if (validationResult is not null)
                {
                    return validationResult;
                }

                CreatorPayment payment = await portalService.UploadInvoice(ctx.Creator.Id, request, cancellationToken);
                return Http200(MapPayment(payment), Localizer["record.updated"]);
            }
            catch (InvalidOperationException ex)
            {
                return Http401(ex.Message);
            }
        }
    }
}
