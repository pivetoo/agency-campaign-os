using AgencyCampaign.Api.Contracts.CommercialResponsibles;
using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.CommercialResponsibles;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Api.Controllers
{
    public sealed class CommercialResponsiblesController : ApiControllerBase
    {
        private readonly ICommercialResponsibleService commercialResponsibleService;
        private new readonly IStringLocalizer<AgencyCampaignResource> Localizer;
        private static readonly Func<CommercialResponsible, CommercialResponsibleContract> MapCommercialResponsible = CommercialResponsibleContract.Projection.Compile();

        public CommercialResponsiblesController(ICommercialResponsibleService commercialResponsibleService, IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.commercialResponsibleService = commercialResponsibleService;
            Localizer = localizer;
        }

        [RequireAccess("Permite listar os responsáveis comerciais cadastrados.")]
        [GetEndpoint("[action]")]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, CancellationToken cancellationToken)
        {
            PagedResult<CommercialResponsible> result = await commercialResponsibleService.GetCommercialResponsibles(request, cancellationToken);
            return Http200(new PagedResult<CommercialResponsibleContract>
            {
                Items = result.Items.Select(MapCommercialResponsible).ToArray(),
                Pagination = result.Pagination
            });
        }

        [RequireAccess("Permite consultar os detalhes de um responsável comercial.")]
        [GetEndpoint("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            CommercialResponsible? responsible = await commercialResponsibleService.GetCommercialResponsibleById(id, cancellationToken);
            return responsible is null ? Http404(Localizer["record.notFound"]) : Http200(MapCommercialResponsible(responsible));
        }

        [RequireAccess("Permite cadastrar um novo responsável comercial.")]
        [PostEndpoint("[action]")]
        public async Task<IActionResult> Create([FromBody] CreateCommercialResponsibleRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            CommercialResponsible responsible = await commercialResponsibleService.CreateCommercialResponsible(request, cancellationToken);
            return Http201(MapCommercialResponsible(responsible), Localizer["record.created"]);
        }

        [RequireAccess("Permite atualizar os dados de um responsável comercial.")]
        [PutEndpoint("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateCommercialResponsibleRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            CommercialResponsible responsible = await commercialResponsibleService.UpdateCommercialResponsible(id, request, cancellationToken);
            return Http200(MapCommercialResponsible(responsible), Localizer["record.updated"]);
        }
    }
}
