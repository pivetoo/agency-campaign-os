using AgencyCampaign.Api.Contracts.Creators;
using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.Creators;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Api.Controllers
{
    [AccessArea("creators.area")]
    public sealed class CreatorsController : ApiControllerBase
    {
        private const long MaxPhotoBytes = 2 * 1024 * 1024;

        private readonly ICreatorService creatorService;
        private readonly IImageUploadStorage imageStorage;
        private new readonly IStringLocalizer<AgencyCampaignResource> Localizer;
        private static readonly Func<Creator, CreatorContract> MapCreator = CreatorContract.Projection.Compile();

        public CreatorsController(ICreatorService creatorService, IImageUploadStorage imageStorage, IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.creatorService = creatorService;
            this.imageStorage = imageStorage;
            Localizer = localizer;
        }

        [RequireAccess("creators.get.description")]
        [GetEndpoint]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, [FromQuery] string? search, [FromQuery] bool includeInactive, CancellationToken cancellationToken)
        {
            PagedResult<Creator> result = await creatorService.GetCreators(request, search, includeInactive, cancellationToken);
            return Http200(new PagedResult<CreatorContract>
            {
                Items = result.Items.Select(MapCreator).ToArray(),
                Pagination = result.Pagination
            });
        }

        [RequireAccess("creators.getById.description")]
        [GetEndpoint("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            Creator? creator = await creatorService.GetCreatorById(id, cancellationToken);
            return creator is null ? Http404(Localizer["record.notFound"]) : Http200(MapCreator(creator));
        }

        [RequireAccess("creators.create.description")]
        [PostEndpoint]
        public async Task<IActionResult> Create([FromBody] CreateCreatorRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            Creator creator = await creatorService.CreateCreator(request, cancellationToken);
            return Http201(MapCreator(creator), Localizer["record.created"]);
        }

        [RequireAccess("creators.update.description")]
        [PutEndpoint("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateCreatorRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            Creator creator = await creatorService.UpdateCreator(id, request, cancellationToken);
            return Http200(MapCreator(creator), Localizer["record.updated"]);
        }

        [RequireAccess("creators.getSummary.description")]
        [GetEndpoint("summary/{id:long}")]
        public async Task<IActionResult> GetSummary(long id, CancellationToken cancellationToken)
        {
            var summary = await creatorService.GetSummary(id, cancellationToken);
            return summary is null ? Http404(Localizer["record.notFound"]) : Http200(summary);
        }

        [RequireAccess("creators.uploadPhoto.description")]
        [PostEndpoint("[action]/{id:long}")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(MaxPhotoBytes)]
        public async Task<IActionResult> UploadPhoto(long id, [FromForm] IFormFile file, CancellationToken cancellationToken)
        {
            if (file is null || file.Length == 0)
            {
                return Http400("Arquivo nao informado.");
            }

            if (file.Length > MaxPhotoBytes)
            {
                return Http400("Arquivo excede o limite de 2MB.");
            }

            Creator? existing = await creatorService.GetCreatorById(id, cancellationToken);
            if (existing is null)
            {
                return Http404(Localizer["record.notFound"]);
            }

            await using Stream stream = file.OpenReadStream();
            string photoUrl = await imageStorage.SaveAsync("creators", id, stream, file.ContentType, cancellationToken);

            Creator creator = await creatorService.SetCreatorPhoto(id, photoUrl, cancellationToken);
            return Http200(MapCreator(creator), Localizer["record.updated"]);
        }

        [RequireAccess("creators.removePhoto.description")]
        [DeleteEndpoint("[action]/{id:long}")]
        public async Task<IActionResult> RemovePhoto(long id, CancellationToken cancellationToken)
        {
            Creator? existing = await creatorService.GetCreatorById(id, cancellationToken);
            if (existing is null)
            {
                return Http404(Localizer["record.notFound"]);
            }

            await imageStorage.RemoveAsync("creators", id, cancellationToken);
            Creator creator = await creatorService.RemoveCreatorPhoto(id, cancellationToken);
            return Http200(MapCreator(creator), Localizer["record.updated"]);
        }

        [RequireAccess("creators.export.description")]
        [GetEndpoint]
        public async Task Export(CancellationToken cancellationToken)
        {
            Response.ContentType = "text/csv; charset=utf-8";
            Response.Headers.Append("Content-Disposition", "attachment; filename=influenciadores.csv");
            await using StreamWriter writer = new(Response.Body, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: true), leaveOpen: true);
            await writer.WriteLineAsync("nome,nome_artistico,nicho,cidade,estado,email,telefone,documento,fee_padrao,ativo");
            await foreach (string row in creatorService.ExportAsync(cancellationToken))
            {
                await writer.WriteLineAsync(row);
                await writer.FlushAsync(cancellationToken);
            }
        }

        [RequireAccess("creators.getCampaigns.description")]
        [GetEndpoint("campaigns/{id:long}")]
        public async Task<IActionResult> GetCampaigns(long id, CancellationToken cancellationToken)
        {
            var campaigns = await creatorService.GetCampaignsByCreator(id, cancellationToken);
            return Http200(campaigns.Select(item => new
            {
                campaignCreatorId = item.Id,
                campaignId = item.CampaignId,
                campaignName = item.Campaign?.Name,
                brandId = item.Campaign?.BrandId,
                brandName = item.Campaign?.Brand?.Name,
                statusId = item.CampaignCreatorStatusId,
                statusName = item.CampaignCreatorStatus?.Name,
                statusColor = item.CampaignCreatorStatus?.Color,
                agreedAmount = item.AgreedAmount,
                agencyFeeAmount = item.AgencyFeeAmount,
                confirmedAt = item.ConfirmedAt,
                cancelledAt = item.CancelledAt
            }));
        }
    }
}
