using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Requests.FinancialAccounts;
using AgencyCampaign.Application.Services;
using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Archon.Core.Pagination;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace AgencyCampaign.Api.Controllers
{
    [AccessArea("bankTransactions.area")]
    public sealed class BankTransactionsController : ApiControllerBase
    {
        private readonly IBankTransactionService service;
        private new readonly IStringLocalizer<AgencyCampaignResource> Localizer;

        public BankTransactionsController(IBankTransactionService service, IStringLocalizer<AgencyCampaignResource> localizer)
        {
            this.service = service;
            Localizer = localizer;
        }

        [RequireAccess("bankTransactions.import.description")]
        [PostEndpoint]
        public async Task<IActionResult> Import([FromBody] ImportBankTransactionsRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            var result = await service.ImportBatch(request, cancellationToken);
            return Http200(result, Localizer["bankTransaction.import.success"]);
        }

        [RequireAccess("bankTransactions.getByAccount.description")]
        [GetEndpoint]
        public async Task<IActionResult> GetByAccount([FromQuery] long accountId, [FromQuery] PagedRequest request, CancellationToken cancellationToken)
        {
            return Http200(await service.GetByAccount(accountId, request, cancellationToken));
        }

        [RequireAccess("bankTransactions.match.description")]
        [PostEndpoint("match/{id:long}")]
        public async Task<IActionResult> Match(long id, [FromBody] MatchBankTransactionRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            var result = await service.MatchToEntry(id, request.FinancialEntryId, cancellationToken);
            return Http200(result, Localizer["bankTransaction.match.success"]);
        }

        [RequireAccess("bankTransactions.unmatch.description")]
        [PostEndpoint("unmatch/{id:long}")]
        public async Task<IActionResult> Unmatch(long id, CancellationToken cancellationToken)
        {
            var result = await service.UnmatchFromEntry(id, cancellationToken);
            return Http200(result, Localizer["bankTransaction.unmatch.success"]);
        }
    }
}
