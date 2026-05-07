using AgencyCampaign.Api.Contracts.Proposals;
using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.Proposals;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Api.Controllers
{
    public sealed class ProposalsController : ApiControllerBase
    {
        private readonly IProposalService proposalService;
        private readonly IProposalItemService proposalItemService;
        private readonly IProposalShareLinkService shareLinkService;
        private readonly IProposalVersionService versionService;
        private readonly IProposalPdfService pdfService;
        private new readonly IStringLocalizer<AgencyCampaignResource> Localizer;
        private static readonly Func<Proposal, ProposalContract> MapProposal = ProposalContract.Projection.Compile();
        private static readonly Func<ProposalItem, ProposalItemContract> MapProposalItem = ProposalItemContract.Projection.Compile();

        public ProposalsController(
            IProposalService proposalService,
            IProposalItemService proposalItemService,
            IProposalShareLinkService shareLinkService,
            IProposalVersionService versionService,
            IProposalPdfService pdfService,
            IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.proposalService = proposalService;
            this.proposalItemService = proposalItemService;
            this.shareLinkService = shareLinkService;
            this.versionService = versionService;
            this.pdfService = pdfService;
            Localizer = localizer;
        }

        [RequireAccess("Permite baixar a proposta em PDF.")]
        [GetEndpoint("pdf/{id:long}")]
        public async Task<IActionResult> GetPdf(long id, CancellationToken cancellationToken)
        {
            byte[] bytes = await pdfService.GenerateForProposalAsync(id, cancellationToken);
            return File(bytes, "application/pdf", $"proposta-{id}.pdf");
        }

        [RequireAccess("Permite listar as propostas comerciais.")]
        [GetEndpoint("[action]")]
        public async Task<IActionResult> Get([FromQuery] PagedRequest request, [FromQuery] ProposalListFilters filters, CancellationToken cancellationToken)
        {
            PagedResult<Proposal> result = await proposalService.GetProposals(request, filters, cancellationToken);
            return Http200(new PagedResult<ProposalContract>
            {
                Items = result.Items.Select(MapProposal).ToArray(),
                Pagination = result.Pagination
            });
        }

        [RequireAccess("Permite consultar os detalhes de uma proposta.")]
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            Proposal? proposal = await proposalService.GetProposalById(id, cancellationToken);
            return proposal is null ? Http404(Localizer["record.notFound"]) : Http200(MapProposal(proposal));
        }

        [RequireAccess("Permite cadastrar uma nova proposta comercial.")]
        [PostEndpoint("[action]")]
        public async Task<IActionResult> Create([FromBody] CreateProposalRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            Proposal proposal = await proposalService.CreateProposal(request, cancellationToken);
            return Http201(MapProposal(proposal), Localizer["record.created"]);
        }

        [RequireAccess("Permite atualizar os dados de uma proposta.")]
        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateProposalRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            Proposal proposal = await proposalService.UpdateProposal(id, request, cancellationToken);
            return Http200(MapProposal(proposal), Localizer["record.updated"]);
        }

        [RequireAccess("Permite enviar uma proposta para a marca.")]
        [HttpPost("{id:long}/Send")]
        public async Task<IActionResult> Send(long id, CancellationToken cancellationToken)
        {
            Proposal proposal = await proposalService.MarkAsSent(id, cancellationToken);
            return Http200(MapProposal(proposal), Localizer["record.updated"]);
        }

        [RequireAccess("Permite marcar uma proposta como visualizada.")]
        [HttpPost("{id:long}/MarkAsViewed")]
        public async Task<IActionResult> MarkAsViewed(long id, CancellationToken cancellationToken)
        {
            Proposal proposal = await proposalService.MarkAsViewed(id, cancellationToken);
            return Http200(MapProposal(proposal));
        }

        [RequireAccess("Permite aprovar uma proposta.")]
        [HttpPost("{id:long}/Approve")]
        public async Task<IActionResult> Approve(long id, CancellationToken cancellationToken)
        {
            Proposal proposal = await proposalService.ApproveProposal(id, cancellationToken);
            return Http200(MapProposal(proposal), Localizer["record.updated"]);
        }

        [RequireAccess("Permite rejeitar uma proposta.")]
        [HttpPost("{id:long}/Reject")]
        public async Task<IActionResult> Reject(long id, CancellationToken cancellationToken)
        {
            Proposal proposal = await proposalService.RejectProposal(id, cancellationToken);
            return Http200(MapProposal(proposal), Localizer["record.updated"]);
        }

        [RequireAccess("Permite converter uma proposta em campanha.")]
        [HttpPost("{id:long}/ConvertToCampaign")]
        public async Task<IActionResult> ConvertToCampaign(long id, [FromBody] ConvertToCampaignRequest request, CancellationToken cancellationToken)
        {
            Proposal proposal = await proposalService.ConvertToCampaign(id, request.CampaignId, cancellationToken);
            return Http200(MapProposal(proposal), Localizer["record.updated"]);
        }

        [RequireAccess("Permite cancelar uma proposta.")]
        [HttpPost("{id:long}/Cancel")]
        public async Task<IActionResult> Cancel(long id, CancellationToken cancellationToken)
        {
            Proposal proposal = await proposalService.CancelProposal(id, cancellationToken);
            return Http200(MapProposal(proposal), Localizer["record.updated"]);
        }

        [RequireAccess("Permite consultar o histórico de status de uma proposta.")]
        [HttpGet("{id:long}/StatusHistory")]
        public async Task<IActionResult> StatusHistory(long id, CancellationToken cancellationToken)
        {
            return Http200(await proposalService.GetStatusHistory(id, cancellationToken));
        }

        [RequireAccess("Permite listar as versões de uma proposta.")]
        [HttpGet("{id:long}/versions/Get")]
        public async Task<IActionResult> GetVersions(long id, CancellationToken cancellationToken)
        {
            return Http200(await versionService.GetByProposalId(id, cancellationToken));
        }

        [RequireAccess("Permite consultar uma versão específica de proposta.")]
        [HttpGet("versions/{versionId:long}")]
        public async Task<IActionResult> GetVersionById(long versionId, CancellationToken cancellationToken)
        {
            var version = await versionService.GetById(versionId, cancellationToken);
            return version is null ? Http404(Localizer["record.notFound"]) : Http200(version);
        }

        [RequireAccess("Permite listar os links de compartilhamento de uma proposta.")]
        [HttpGet("{id:long}/share-links/Get")]
        public async Task<IActionResult> GetShareLinks(long id, CancellationToken cancellationToken)
        {
            return Http200(await shareLinkService.GetByProposalId(id, cancellationToken));
        }

        [RequireAccess("Permite gerar um novo link público para uma proposta.")]
        [HttpPost("{id:long}/share-links/Create")]
        public async Task<IActionResult> CreateShareLink(long id, [FromBody] CreateProposalShareLinkRequest request, CancellationToken cancellationToken)
        {
            var link = await shareLinkService.CreateShareLink(id, request, cancellationToken);
            return Http201(link, Localizer["record.created"]);
        }

        [RequireAccess("Permite revogar um link público de proposta.")]
        [HttpPost("share-links/{shareLinkId:long}/Revoke")]
        public async Task<IActionResult> RevokeShareLink(long shareLinkId, CancellationToken cancellationToken)
        {
            var link = await shareLinkService.RevokeShareLink(shareLinkId, cancellationToken);
            return Http200(link, Localizer["record.updated"]);
        }

        [RequireAccess("Permite listar os itens de uma proposta.")]
        [HttpGet("{proposalId:long}/items/Get")]
        public async Task<IActionResult> GetItems(long proposalId, CancellationToken cancellationToken)
        {
            IReadOnlyCollection<ProposalItem> items = await proposalItemService.GetItemsByProposalId(proposalId, cancellationToken);
            return Http200(items.Select(MapProposalItem).ToList());
        }

        [RequireAccess("Permite adicionar um item a uma proposta.")]
        [HttpPost("{proposalId:long}/items/Create")]
        public async Task<IActionResult> CreateItem(long proposalId, [FromBody] CreateProposalItemRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            if (request.ProposalId != proposalId)
            {
                request.ProposalId = proposalId;
            }

            ProposalItem item = await proposalItemService.CreateProposalItem(request, cancellationToken);
            return Http201(MapProposalItem(item), Localizer["record.created"]);
        }

        [RequireAccess("Permite atualizar um item da proposta.")]
        [PutEndpoint("items/{id:long}")]
        public async Task<IActionResult> UpdateItem(long id, [FromBody] UpdateProposalItemRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            ProposalItem item = await proposalItemService.UpdateProposalItem(id, request, cancellationToken);
            return Http200(MapProposalItem(item), Localizer["record.updated"]);
        }

        [RequireAccess("Permite excluir um item da proposta.")]
        [DeleteEndpoint("items/{id:long}")]
        public async Task<IActionResult> DeleteItem(long id, CancellationToken cancellationToken)
        {
            await proposalItemService.DeleteProposalItem(id, cancellationToken);
            return Http204();
        }
    }

    public sealed class ConvertToCampaignRequest
    {
        public long CampaignId { get; set; }
    }
}
