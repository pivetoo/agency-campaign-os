using AgencyCampaign.Application.Models.Financial;
using AgencyCampaign.Application.Requests.FinancialAccounts;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using Archon.Core.Pagination;
using Archon.Infrastructure.Persistence.EF;
using Microsoft.EntityFrameworkCore;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class BankTransactionService : IBankTransactionService
    {
        private static readonly TimeSpan MatchingWindow = TimeSpan.FromDays(2);

        private readonly DbContext dbContext;

        public BankTransactionService(DbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<ImportBankTransactionsResult> ImportBatch(ImportBankTransactionsRequest request, CancellationToken cancellationToken = default)
        {
            FinancialAccount? account = await dbContext.Set<FinancialAccount>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == request.AccountId, cancellationToken);

            if (account is null)
            {
                throw new InvalidOperationException("financialAccount.notFound");
            }

            HashSet<string> incomingExternalIds = request.Transactions
                .Select(item => item.ExternalId.Trim())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            HashSet<string> alreadyPersisted = (await dbContext.Set<BankTransaction>()
                .AsNoTracking()
                .Where(item => item.AccountId == account.Id && incomingExternalIds.Contains(item.ExternalId))
                .Select(item => item.ExternalId)
                .ToListAsync(cancellationToken))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            DateTimeOffset windowStart = request.Transactions.Any()
                ? request.Transactions.Min(item => item.OccurredAt) - MatchingWindow
                : DateTimeOffset.UtcNow;
            DateTimeOffset windowEnd = request.Transactions.Any()
                ? request.Transactions.Max(item => item.OccurredAt) + MatchingWindow
                : DateTimeOffset.UtcNow;

            List<FinancialEntry> candidateEntries = await dbContext.Set<FinancialEntry>()
                .AsTracking()
                .Where(item => item.AccountId == account.Id
                    && item.Status == FinancialEntryStatus.Pending
                    && item.DueAt >= windowStart
                    && item.DueAt <= windowEnd)
                .ToListAsync(cancellationToken);

            HashSet<long> usedEntryIds = new();
            int imported = 0;
            int skipped = 0;
            int autoMatched = 0;

            foreach (ImportBankTransactionItem item in request.Transactions)
            {
                string externalId = item.ExternalId.Trim();
                if (alreadyPersisted.Contains(externalId))
                {
                    skipped++;
                    continue;
                }

                BankTransaction bankTransaction = new(
                    account.Id,
                    externalId,
                    item.OccurredAt,
                    item.Amount,
                    item.Direction,
                    item.Description,
                    item.Category,
                    item.RawPayload);

                FinancialEntryType expectedType = item.Direction == BankTransactionDirection.Credit
                    ? FinancialEntryType.Receivable
                    : FinancialEntryType.Payable;

                List<FinancialEntry> matches = candidateEntries
                    .Where(entry => !usedEntryIds.Contains(entry.Id)
                        && entry.Type == expectedType
                        && entry.Amount == item.Amount
                        && (entry.DueAt - item.OccurredAt).Duration() <= MatchingWindow)
                    .ToList();

                if (matches.Count == 1)
                {
                    FinancialEntry single = matches[0];
                    bankTransaction.AttachToEntry(single.Id, BankTransactionMatchKind.Auto);
                    single.SettleFromReconciliation(item.OccurredAt);
                    usedEntryIds.Add(single.Id);
                    autoMatched++;
                }

                dbContext.Set<BankTransaction>().Add(bankTransaction);
                imported++;
            }

            if (request.SyncedBalance.HasValue && request.SyncedAt.HasValue)
            {
                account.MarkSynced(request.SyncedBalance.Value, request.SyncedAt.Value);
            }
            else if (request.SyncedAt.HasValue)
            {
                account.MarkSynced(account.LastSyncedBalance ?? 0m, request.SyncedAt.Value);
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            return new ImportBankTransactionsResult
            {
                Imported = imported,
                Skipped = skipped,
                AutoMatched = autoMatched
            };
        }

        public async Task<PagedResult<BankTransactionModel>> GetByAccount(long accountId, PagedRequest request, CancellationToken cancellationToken = default)
        {
            IQueryable<BankTransaction> query = dbContext.Set<BankTransaction>()
                .AsNoTracking()
                .Where(item => item.AccountId == accountId)
                .OrderByDescending(item => item.OccurredAt);

            PagedResult<BankTransaction> paged = await query.ToPagedResultAsync(request, cancellationToken);

            return new PagedResult<BankTransactionModel>
            {
                Items = paged.Items.Select(ToModel).ToArray(),
                Pagination = paged.Pagination
            };
        }

        public async Task<BankTransactionModel> MatchToEntry(long bankTransactionId, long financialEntryId, CancellationToken cancellationToken = default)
        {
            BankTransaction? bankTransaction = await dbContext.Set<BankTransaction>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == bankTransactionId, cancellationToken);

            if (bankTransaction is null)
            {
                throw new InvalidOperationException("bankTransaction.notFound");
            }

            FinancialEntry? entry = await dbContext.Set<FinancialEntry>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == financialEntryId && item.AccountId == bankTransaction.AccountId, cancellationToken);

            if (entry is null)
            {
                throw new InvalidOperationException("financialEntry.notFoundForAccount");
            }

            if (entry.Amount != bankTransaction.Amount)
            {
                throw new InvalidOperationException("bankTransaction.amountMismatch");
            }

            // Re-vincular para a MESMA entry e idempotente: ja esta baixada por esta transacao, nada muda.
            if (bankTransaction.FinancialEntryId == financialEntryId)
            {
                return ToModel(bankTransaction);
            }

            // Liquida a nova entry primeiro (lanca se ela nao estiver aberta), antes de mexer no vinculo anterior.
            entry.SettleFromReconciliation(bankTransaction.OccurredAt);

            // Se a transacao ja estava conciliada com OUTRA entry, reabre aquela para nao deixar baixa orfa (dupla contagem).
            if (bankTransaction.FinancialEntryId.HasValue)
            {
                FinancialEntry? previous = await dbContext.Set<FinancialEntry>()
                    .AsTracking()
                    .FirstOrDefaultAsync(item => item.Id == bankTransaction.FinancialEntryId.Value, cancellationToken);

                if (previous is not null && previous.Status == FinancialEntryStatus.Paid)
                {
                    previous.ReopenFromReconciliation();
                }
            }

            bankTransaction.AttachToEntry(financialEntryId, BankTransactionMatchKind.Manual);
            await dbContext.SaveChangesAsync(cancellationToken);

            return ToModel(bankTransaction);
        }

        public async Task<BankTransactionModel> UnmatchFromEntry(long bankTransactionId, CancellationToken cancellationToken = default)
        {
            BankTransaction? bankTransaction = await dbContext.Set<BankTransaction>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Id == bankTransactionId, cancellationToken);

            if (bankTransaction is null)
            {
                throw new InvalidOperationException("bankTransaction.notFound");
            }

            if (bankTransaction.FinancialEntryId.HasValue)
            {
                FinancialEntry? entry = await dbContext.Set<FinancialEntry>()
                    .AsTracking()
                    .FirstOrDefaultAsync(item => item.Id == bankTransaction.FinancialEntryId.Value, cancellationToken);

                if (entry is not null && entry.Status == FinancialEntryStatus.Paid)
                {
                    entry.ReopenFromReconciliation();
                }
            }

            bankTransaction.DetachFromEntry();
            await dbContext.SaveChangesAsync(cancellationToken);

            return ToModel(bankTransaction);
        }

        private static BankTransactionModel ToModel(BankTransaction entity)
        {
            return new BankTransactionModel
            {
                Id = entity.Id,
                AccountId = entity.AccountId,
                ExternalId = entity.ExternalId,
                OccurredAt = entity.OccurredAt,
                Amount = entity.Amount,
                Direction = entity.Direction,
                Description = entity.Description,
                Category = entity.Category,
                FinancialEntryId = entity.FinancialEntryId,
                MatchedAt = entity.MatchedAt,
                MatchKind = entity.MatchKind,
                ImportedAt = entity.ImportedAt
            };
        }
    }
}
