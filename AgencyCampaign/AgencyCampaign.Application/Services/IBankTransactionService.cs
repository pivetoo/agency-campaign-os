using AgencyCampaign.Application.Models.Financial;
using AgencyCampaign.Application.Requests.FinancialAccounts;
using Archon.Core.Pagination;

namespace AgencyCampaign.Application.Services
{
    public interface IBankTransactionService
    {
        Task<ImportBankTransactionsResult> ImportBatch(ImportBankTransactionsRequest request, CancellationToken cancellationToken = default);

        Task<PagedResult<BankTransactionModel>> GetByAccount(long accountId, PagedRequest request, CancellationToken cancellationToken = default);

        Task<BankTransactionModel> MatchToEntry(long bankTransactionId, long financialEntryId, CancellationToken cancellationToken = default);

        Task<BankTransactionModel> UnmatchFromEntry(long bankTransactionId, CancellationToken cancellationToken = default);
    }
}
