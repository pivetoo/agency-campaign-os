using AgencyCampaign.Api.Contracts.Opportunities;
using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.Opportunities;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Api.Controllers
{
    public sealed class OpportunitiesController : ApiControllerBase
    {
        private readonly IOpportunityService opportunityService;
        private readonly IOpportunityNegotiationService negotiationService;
        private readonly IOpportunityFollowUpService followUpService;
        private new readonly IStringLocalizer<AgencyCampaignResource> Localizer;
        private static readonly Func<Opportunity, OpportunityContract> MapOpportunity = OpportunityContract.Projection.Compile();
        private static readonly Func<OpportunityNegotiation, OpportunityNegotiationContract> MapNegotiation = OpportunityNegotiationContract.Projection.Compile();
        private static readonly Func<OpportunityFollowUp, OpportunityFollowUpContract> MapFollowUp = OpportunityFollowUpContract.Projection.Compile();

        public OpportunitiesController(
            IOpportunityService opportunityService,
            IOpportunityNegotiationService negotiationService,
            IOpportunityFollowUpService followUpService,
            IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.opportunityService = opportunityService;
            this.negotiationService = negotiationService;
            this.followUpService = followUpService;
            Localizer = localizer;
        }

        [RequireAccess("Permite listar as oportunidades comerciais cadastradas.")]
        [GetEndpoint("[action]")]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, CancellationToken cancellationToken)
        {
            PagedResult<Opportunity> result = await opportunityService.GetOpportunities(request, cancellationToken);
            return Http200(new PagedResult<OpportunityContract>
            {
                Items = result.Items.Select(MapOpportunity).ToArray(),
                Pagination = result.Pagination
            });
        }

        [RequireAccess("Permite consultar os detalhes de uma oportunidade comercial.")]
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            Opportunity? opportunity = await opportunityService.GetOpportunityById(id, cancellationToken);
            return opportunity is null ? Http404(Localizer["record.notFound"]) : Http200(MapOpportunity(opportunity));
        }

        [RequireAccess("Permite consultar o board comercial por estágio.")]
        [GetEndpoint("[action]")]
        public async Task<IActionResult> Board(CancellationToken cancellationToken)
        {
            return Http200(await opportunityService.GetBoard(cancellationToken));
        }

        [RequireAccess("Permite consultar o resumo do dashboard comercial.")]
        [GetEndpoint("[action]")]
        public async Task<IActionResult> Dashboard(CancellationToken cancellationToken)
        {
            return Http200(await opportunityService.GetDashboardSummary(cancellationToken));
        }

        [RequireAccess("Permite consultar os alertas comerciais.")]
        [GetEndpoint("[action]")]
        public async Task<IActionResult> Alerts(CancellationToken cancellationToken)
        {
            return Http200(await opportunityService.GetAlerts(cancellationToken));
        }

        [RequireAccess("Permite consultar o histórico de estágios de uma oportunidade.")]
        [HttpGet("{id:long}/StageHistory")]
        public async Task<IActionResult> StageHistory(long id, CancellationToken cancellationToken)
        {
            return Http200(await opportunityService.GetStageHistory(id, cancellationToken));
        }


        [RequireAccess("Permite cadastrar uma nova oportunidade comercial.")]
        [PostEndpoint("[action]")]
        public async Task<IActionResult> Create([FromBody] CreateOpportunityRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            Opportunity opportunity = await opportunityService.CreateOpportunity(request, cancellationToken);
            return Http201(MapOpportunity(opportunity), Localizer["record.created"]);
        }

        [RequireAccess("Permite atualizar os dados de uma oportunidade comercial.")]
        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateOpportunityRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            Opportunity opportunity = await opportunityService.UpdateOpportunity(id, request, cancellationToken);
            return Http200(MapOpportunity(opportunity), Localizer["record.updated"]);
        }

        [RequireAccess("Permite alterar o estágio de uma oportunidade comercial.")]
        [HttpPost("{id:long}/ChangeStage")]
        public async Task<IActionResult> ChangeStage(long id, [FromBody] ChangeOpportunityStageRequest request, CancellationToken cancellationToken)
        {
            Opportunity opportunity = await opportunityService.ChangeStage(id, request, cancellationToken);
            return Http200(MapOpportunity(opportunity), Localizer["record.updated"]);
        }

        [RequireAccess("Permite encerrar uma oportunidade como ganha.")]
        [HttpPost("{id:long}/CloseAsWon")]
        public async Task<IActionResult> CloseAsWon(long id, [FromBody] CloseOpportunityAsWonRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            Opportunity opportunity = await opportunityService.CloseAsWon(id, request, cancellationToken);
            return Http200(MapOpportunity(opportunity), Localizer["record.updated"]);
        }

        [RequireAccess("Permite encerrar uma oportunidade como perdida.")]
        [HttpPost("{id:long}/CloseAsLost")]
        public async Task<IActionResult> CloseAsLost(long id, [FromBody] CloseOpportunityAsLostRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            Opportunity opportunity = await opportunityService.CloseAsLost(id, request, cancellationToken);
            return Http200(MapOpportunity(opportunity), Localizer["record.updated"]);
        }

        [RequireAccess("Permite excluir uma oportunidade comercial.")]
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
        {
            Opportunity? opportunity = await opportunityService.Delete(id, cancellationToken);
            return opportunity is null ? Http404(Localizer["record.notFound"]) : Http204();
        }

        [RequireAccess("Permite listar as negociações de uma oportunidade.")]
        [HttpGet("{opportunityId:long}/negotiations/GetNegotiations")]
        public async Task<IActionResult> GetNegotiations(long opportunityId, CancellationToken cancellationToken)
        {
            IReadOnlyCollection<OpportunityNegotiation> negotiations = await negotiationService.GetNegotiationsByOpportunityId(opportunityId, cancellationToken);
            return Http200(negotiations.Select(MapNegotiation).ToList());
        }

        [RequireAccess("Permite adicionar uma negociação a uma oportunidade.")]
        [HttpPost("{opportunityId:long}/negotiations/CreateNegotiation")]
        public async Task<IActionResult> CreateNegotiation(long opportunityId, [FromBody] CreateOpportunityNegotiationRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            if (request.OpportunityId != opportunityId)
            {
                request.OpportunityId = opportunityId;
            }

            OpportunityNegotiation negotiation = await negotiationService.CreateOpportunityNegotiation(request, cancellationToken);
            return Http201(MapNegotiation(negotiation), Localizer["record.created"]);
        }

        [RequireAccess("Permite atualizar uma negociação da oportunidade.")]
        [PutEndpoint("negotiations/{id:long}")]
        public async Task<IActionResult> UpdateNegotiation(long id, [FromBody] UpdateOpportunityNegotiationRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            OpportunityNegotiation negotiation = await negotiationService.UpdateOpportunityNegotiation(id, request, cancellationToken);
            return Http200(MapNegotiation(negotiation), Localizer["record.updated"]);
        }

        [RequireAccess("Permite alterar o status de uma negociação.")]
        [PostEndpoint("negotiations/{id:long}/[action]")]
        public async Task<IActionResult> ChangeStatus(long id, [FromBody] ChangeOpportunityNegotiationStatusRequest request, CancellationToken cancellationToken)
        {
            OpportunityNegotiation negotiation = await negotiationService.ChangeStatus(id, request, cancellationToken);
            return Http200(MapNegotiation(negotiation), Localizer["record.updated"]);
        }

        [RequireAccess("Permite excluir uma negociação da oportunidade.")]
        [DeleteEndpoint("negotiations/{id:long}")]
        public async Task<IActionResult> DeleteNegotiation(long id, CancellationToken cancellationToken)
        {
            await negotiationService.DeleteOpportunityNegotiation(id, cancellationToken);
            return Http204();
        }

        [RequireAccess("Permite listar os follow-ups de uma oportunidade.")]
        [HttpGet("{opportunityId:long}/followups/GetFollowUps")]
        public async Task<IActionResult> GetFollowUps(long opportunityId, CancellationToken cancellationToken)
        {
            IReadOnlyCollection<OpportunityFollowUp> followUps = await followUpService.GetFollowUpsByOpportunityId(opportunityId, cancellationToken);
            return Http200(followUps.Select(MapFollowUp).ToList());
        }

        [RequireAccess("Permite adicionar um follow-up a uma oportunidade.")]
        [HttpPost("{opportunityId:long}/followups/CreateFollowUp")]
        public async Task<IActionResult> CreateFollowUp(long opportunityId, [FromBody] CreateOpportunityFollowUpRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            if (request.OpportunityId != opportunityId)
            {
                request.OpportunityId = opportunityId;
            }

            OpportunityFollowUp followUp = await followUpService.CreateOpportunityFollowUp(request, cancellationToken);
            return Http201(MapFollowUp(followUp), Localizer["record.created"]);
        }

        [RequireAccess("Permite atualizar um follow-up da oportunidade.")]
        [PutEndpoint("followups/{id:long}")]
        public async Task<IActionResult> UpdateFollowUp(long id, [FromBody] UpdateOpportunityFollowUpRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            OpportunityFollowUp followUp = await followUpService.UpdateOpportunityFollowUp(id, request, cancellationToken);
            return Http200(MapFollowUp(followUp), Localizer["record.updated"]);
        }

        [RequireAccess("Permite concluir um follow-up da oportunidade.")]
        [PostEndpoint("followups/{id:long}/[action]")]
        public async Task<IActionResult> Complete(long id, CancellationToken cancellationToken)
        {
            OpportunityFollowUp followUp = await followUpService.CompleteOpportunityFollowUp(id, cancellationToken);
            return Http200(MapFollowUp(followUp), Localizer["record.updated"]);
        }

        [RequireAccess("Permite excluir um follow-up da oportunidade.")]
        [DeleteEndpoint("followups/{id:long}")]
        public async Task<IActionResult> DeleteFollowUp(long id, CancellationToken cancellationToken)
        {
            await followUpService.DeleteOpportunityFollowUp(id, cancellationToken);
            return Http204();
        }
    }
}
