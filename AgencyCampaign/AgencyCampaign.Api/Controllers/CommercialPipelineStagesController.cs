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

        [RequireAccess("Permite listar os estágios configurados do pipeline comercial.")]
        [GetEndpoint("[action]")]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, CancellationToken cancellationToken)
        {
            PagedResult<CommercialPipelineStage> result = await stageService.GetStages(request, cancellationToken);
            return Http200(new PagedResult<CommercialPipelineStageContract>
            {
                Items = result.Items.Select(MapStage).ToArray(),
                Pagination = result.Pagination
            });
        }

        [RequireAccess("Permite listar os estágios ativos do pipeline comercial.")]
        [GetEndpoint("active")]
        public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
        {
            List<CommercialPipelineStage> stages = await stageService.GetActiveStages(cancellationToken);
            return Http200(stages.Select(MapStage).ToList());
        }

        [RequireAccess("Permite consultar um estágio do pipeline comercial.")]
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            CommercialPipelineStage? stage = await stageService.GetStageById(id, cancellationToken);
            return stage is null ? Http404(Localizer["record.notFound"]) : Http200(MapStage(stage));
        }

        [RequireAccess("Permite cadastrar um estágio do pipeline comercial.")]
        [PostEndpoint("[action]")]
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

        [RequireAccess("Permite atualizar um estágio do pipeline comercial.")]
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
