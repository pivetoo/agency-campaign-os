using AgencyCampaign.Api.Contracts.CommercialPipelineStages;
using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.CommercialPipelineStages;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Api.Controllers
{
    [AccessArea("commercialPipelineStages.area")]
    public sealed class CommercialPipelineStagesController : ApiControllerBase
    {
        private readonly ICommercialPipelineStageService stageService;
        private new readonly IStringLocalizer<AgencyCampaignResource> Localizer;
        private static readonly Func<CommercialPipelineStage, CommercialPipelineStageContract> MapStage = CommercialPipelineStageContract.Projection.Compile();

        public CommercialPipelineStagesController(ICommercialPipelineStageService stageService, IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.stageService = stageService;
            Localizer = localizer;
        }

        [RequireAccess("commercialPipelineStages.get.description")]
        [GetEndpoint]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, [FromQuery] string? search, [FromQuery] bool includeInactive, CancellationToken cancellationToken)
        {
            PagedResult<CommercialPipelineStage> result = await stageService.GetStages(request, search, includeInactive, cancellationToken);
            return Http200(new PagedResult<CommercialPipelineStageContract>
            {
                Items = result.Items.Select(MapStage).ToArray(),
                Pagination = result.Pagination
            });
        }

        [RequireAccess("commercialPipelineStages.getActive.description")]
        [GetEndpoint("active")]
        public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
        {
            List<CommercialPipelineStage> stages = await stageService.GetActiveStages(cancellationToken);
            return Http200(stages.Select(MapStage).ToList());
        }

        [RequireAccess("commercialPipelineStages.getById.description")]
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            CommercialPipelineStage? stage = await stageService.GetStageById(id, cancellationToken);
            return stage is null ? Http404(Localizer["record.notFound"]) : Http200(MapStage(stage));
        }

        [RequireAccess("commercialPipelineStages.create.description")]
        [PostEndpoint]
        public async Task<IActionResult> Create([FromBody] CreateCommercialPipelineStageRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            CommercialPipelineStage stage = await stageService.CreateStage(request, cancellationToken);
            return Http201(MapStage(stage), Localizer["record.created"]);
        }

        [RequireAccess("commercialPipelineStages.update.description")]
        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateCommercialPipelineStageRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            CommercialPipelineStage stage = await stageService.UpdateStage(id, request, cancellationToken);
            return Http200(MapStage(stage), Localizer["record.updated"]);
        }
    }
}
