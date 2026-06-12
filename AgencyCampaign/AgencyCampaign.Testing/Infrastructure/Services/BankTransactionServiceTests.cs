using AgencyCampaign.Application.Models.Financial;
using AgencyCampaign.Application.Requests.FinancialAccounts;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using AgencyCampaign.Infrastructure.Services;
using AgencyCampaign.Testing.TestSupport;
using Archon.Core.Pagination;
using Microsoft.EntityFrameworkCore;

namespace AgencyCampaign.Testing.Infrastructure.Services
{
    [TestFixture]
    public sealed class BankTransactionServiceTests
    {
        private TestDbContext db = null!;
        private BankTransactionService service = null!;

        [SetUp]
        public void SetUp()
        {
            db = TestDbContext.CreateInMemory();
            service = new BankTransactionService(db);
        }

        [TearDown]
        public void TearDown() => db.Dispose();

        private async Task<FinancialAccount> SeedAccountAsync()
        {
            FinancialAccount account = new("Conta", FinancialAccountType.Bank, 0m, "#fff");
            db.Add(account);
            await db.SaveChangesAsync();
            return account;
        }

        private async Task<FinancialEntry> SeedEntryAsync(long accountId, decimal amount, DateTimeOffset dueAt, FinancialEntryType type = FinancialEntryType.Receivable, FinancialEntryStatus? status = null)
        {
            FinancialEntry entry = new(
                accountId,
                type,
                type == FinancialEntryType.Receivable ? FinancialEntryCategory.BrandReceivable : FinancialEntryCategory.OperationalCost,
                "Entrada teste",
                amount,
                dueAt,
                DateTimeOffset.UtcNow);
            db.Add(entry);
            await db.SaveChangesAsync();
            return entry;
        }

        private static ImportBankTransactionItem MakeItem(string externalId, decimal amount, BankTransactionDirection direction, DateTimeOffset occurredAt)
        {
            return new ImportBankTransactionItem
            {
                ExternalId = externalId,
                Amount = amount,
                Direction = direction,
                OccurredAt = occurredAt,
                Description = "tx " + externalId
            };
        }

        [Test]
        public async Task ImportBatch_should_throw_when_account_not_found()
        {
            ImportBankTransactionsRequest request = new() { AccountId = 99 };

            Func<Task> act = () => service.ImportBatch(request);

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("financialAccount.notFound");
        }

        [Test]
        public async Task ImportBatch_should_skip_duplicates_by_external_id()
        {
            FinancialAccount account = await SeedAccountAsync();
            DateTimeOffset now = DateTimeOffset.UtcNow;
            db.Add(new BankTransaction(account.Id, "ext-1", now, 100m, BankTransactionDirection.Credit, "preexisting"));
            await db.SaveChangesAsync();

            ImportBankTransactionsRequest request = new()
            {
                AccountId = account.Id,
                Transactions = new() { MakeItem("ext-1", 100m, BankTransactionDirection.Credit, now) }
            };

            ImportBankTransactionsResult result = await service.ImportBatch(request);

            result.Imported.Should().Be(0);
            result.Skipped.Should().Be(1);
            result.AutoMatched.Should().Be(0);
        }

        [Test]
        public async Task ImportBatch_should_persist_new_transactions()
        {
            FinancialAccount account = await SeedAccountAsync();
            DateTimeOffset now = DateTimeOffset.UtcNow;

            ImportBankTransactionsRequest request = new()
            {
                AccountId = account.Id,
                Transactions = new()
                {
                    MakeItem("ext-1", 100m, BankTransactionDirection.Credit, now),
                    MakeItem("ext-2", 50m, BankTransactionDirection.Debit, now),
                }
            };

            ImportBankTransactionsResult result = await service.ImportBatch(request);

            result.Imported.Should().Be(2);
            result.Skipped.Should().Be(0);
            (await db.Set<BankTransaction>().CountAsync()).Should().Be(2);
        }

        [Test]
        public async Task ImportBatch_should_auto_match_credit_to_pending_receivable()
        {
            FinancialAccount account = await SeedAccountAsync();
            DateTimeOffset now = DateTimeOffset.UtcNow;
            FinancialEntry entry = await SeedEntryAsync(account.Id, 250m, now, FinancialEntryType.Receivable);

            ImportBankTransactionsRequest request = new()
            {
                AccountId = account.Id,
                Transactions = new() { MakeItem("ext-credit", 250m, BankTransactionDirection.Credit, now) }
            };

            ImportBankTransactionsResult result = await service.ImportBatch(request);

            result.AutoMatched.Should().Be(1);
            BankTransaction tx = await db.Set<BankTransaction>().FirstAsync();
            tx.FinancialEntryId.Should().Be(entry.Id);
            tx.MatchKind.Should().Be(BankTransactionMatchKind.Auto);
        }

        [Test]
        public async Task ImportBatch_should_not_match_when_multiple_candidates_exist()
        {
            FinancialAccount account = await SeedAccountAsync();
            DateTimeOffset now = DateTimeOffset.UtcNow;
            await SeedEntryAsync(account.Id, 100m, now);
            await SeedEntryAsync(account.Id, 100m, now.AddHours(1));

            ImportBankTransactionsRequest request = new()
            {
                AccountId = account.Id,
                Transactions = new() { MakeItem("ext-1", 100m, BankTransactionDirection.Credit, now) }
            };

            ImportBankTransactionsResult result = await service.ImportBatch(request);

            result.Imported.Should().Be(1);
            result.AutoMatched.Should().Be(0);
        }

        [Test]
        public async Task ImportBatch_should_match_debit_to_payable_only()
        {
            FinancialAccount account = await SeedAccountAsync();
            DateTimeOffset now = DateTimeOffset.UtcNow;
            await SeedEntryAsync(account.Id, 70m, now, FinancialEntryType.Receivable);
            FinancialEntry payable = await SeedEntryAsync(account.Id, 70m, now, FinancialEntryType.Payable);

            ImportBankTransactionsRequest request = new()
            {
                AccountId = account.Id,
                Transactions = new() { MakeItem("ext-debit", 70m, BankTransactionDirection.Debit, now) }
            };

            ImportBankTransactionsResult result = await service.ImportBatch(request);

            result.AutoMatched.Should().Be(1);
            BankTransaction tx = await db.Set<BankTransaction>().FirstAsync();
            tx.FinancialEntryId.Should().Be(payable.Id);
        }

        [Test]
        public async Task ImportBatch_should_update_account_balance_when_provided()
        {
            FinancialAccount account = await SeedAccountAsync();
            DateTimeOffset syncedAt = DateTimeOffset.UtcNow;

            ImportBankTransactionsRequest request = new()
            {
                AccountId = account.Id,
                SyncedBalance = 1234.56m,
                SyncedAt = syncedAt,
                Transactions = new()
            };

            await service.ImportBatch(request);

            FinancialAccount refreshed = await db.Set<FinancialAccount>().AsNoTracking().FirstAsync(item => item.Id == account.Id);
            refreshed.LastSyncedBalance.Should().Be(1234.56m);
            refreshed.LastSyncedAt.Should().Be(syncedAt);
        }

        [Test]
        public async Task GetByAccount_should_return_transactions_ordered_desc()
        {
            FinancialAccount account = await SeedAccountAsync();
            DateTimeOffset baseDate = DateTimeOffset.UtcNow;
            db.Add(new BankTransaction(account.Id, "a", baseDate.AddDays(-1), 10m, BankTransactionDirection.Credit, "x"));
            db.Add(new BankTransaction(account.Id, "b", baseDate, 20m, BankTransactionDirection.Credit, "x"));
            await db.SaveChangesAsync();

            PagedResult<BankTransactionModel> result = await service.GetByAccount(account.Id, new PagedRequest { Page = 1, PageSize = 10 });

            result.Items.Should().HaveCount(2);
            result.Items.First().ExternalId.Should().Be("b");
        }

        [Test]
        public async Task MatchToEntry_should_throw_when_transaction_not_found()
        {
            Func<Task> act = () => service.MatchToEntry(99, 1);

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("bankTransaction.notFound");
        }

        [Test]
        public async Task MatchToEntry_should_throw_when_entry_belongs_to_other_account()
        {
            FinancialAccount account = await SeedAccountAsync();
            FinancialAccount other = new("Outra", FinancialAccountType.Bank, 0m, "#000");
            db.Add(other);
            await db.SaveChangesAsync();

            BankTransaction tx = new(account.Id, "ext", DateTimeOffset.UtcNow, 50m, BankTransactionDirection.Credit, "x");
            db.Add(tx);
            FinancialEntry foreignEntry = await SeedEntryAsync(other.Id, 50m, DateTimeOffset.UtcNow);
            await db.SaveChangesAsync();

            Func<Task> act = () => service.MatchToEntry(tx.Id, foreignEntry.Id);

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("financialEntry.notFoundForAccount");
        }

        [Test]
        public async Task MatchToEntry_should_set_match_kind_manual()
        {
            FinancialAccount account = await SeedAccountAsync();
            BankTransaction tx = new(account.Id, "ext", DateTimeOffset.UtcNow, 50m, BankTransactionDirection.Credit, "x");
            db.Add(tx);
            FinancialEntry entry = await SeedEntryAsync(account.Id, 50m, DateTimeOffset.UtcNow);
            await db.SaveChangesAsync();

            BankTransactionModel result = await service.MatchToEntry(tx.Id, entry.Id);

            result.FinancialEntryId.Should().Be(entry.Id);
            result.MatchKind.Should().Be(BankTransactionMatchKind.Manual);
        }

        [Test]
        public async Task UnmatchFromEntry_should_throw_when_transaction_not_found()
        {
            Func<Task> act = () => service.UnmatchFromEntry(99);

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("bankTransaction.notFound");
        }

        [Test]
        public async Task UnmatchFromEntry_should_clear_attachment()
        {
            FinancialAccount account = await SeedAccountAsync();
            BankTransaction tx = new(account.Id, "ext", DateTimeOffset.UtcNow, 50m, BankTransactionDirection.Credit, "x");
            tx.AttachToEntry(123, BankTransactionMatchKind.Manual);
            db.Add(tx);
            await db.SaveChangesAsync();

            BankTransactionModel result = await service.UnmatchFromEntry(tx.Id);

            result.FinancialEntryId.Should().BeNull();
        }

        [Test]
        public async Task MatchToEntry_should_settle_the_matched_entry_as_paid()
        {
            FinancialAccount account = await SeedAccountAsync();
            DateTimeOffset txDate = DateTimeOffset.UtcNow.AddDays(-1);
            BankTransaction tx = new(account.Id, "ext", txDate, 50m, BankTransactionDirection.Credit, "x");
            db.Add(tx);
            FinancialEntry entry = await SeedEntryAsync(account.Id, 50m, DateTimeOffset.UtcNow.AddDays(3));
            await db.SaveChangesAsync();

            await service.MatchToEntry(tx.Id, entry.Id);

            FinancialEntry reloaded = await db.Set<FinancialEntry>().AsNoTracking().FirstAsync(item => item.Id == entry.Id);
            reloaded.Status.Should().Be(FinancialEntryStatus.Paid);
            reloaded.PaidAt.Should().BeCloseTo(txDate, TimeSpan.FromSeconds(1));
        }

        [Test]
        public async Task MatchToEntry_should_throw_when_amount_differs()
        {
            FinancialAccount account = await SeedAccountAsync();
            BankTransaction tx = new(account.Id, "ext", DateTimeOffset.UtcNow, 985m, BankTransactionDirection.Credit, "x");
            db.Add(tx);
            FinancialEntry entry = await SeedEntryAsync(account.Id, 1000m, DateTimeOffset.UtcNow);
            await db.SaveChangesAsync();

            Func<Task> act = () => service.MatchToEntry(tx.Id, entry.Id);

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("bankTransaction.amountMismatch");
        }

        [Test]
        public async Task MatchToEntry_should_throw_when_entry_is_already_paid()
        {
            FinancialAccount account = await SeedAccountAsync();
            BankTransaction tx = new(account.Id, "ext", DateTimeOffset.UtcNow, 50m, BankTransactionDirection.Credit, "x");
            db.Add(tx);
            FinancialEntry entry = await SeedEntryAsync(account.Id, 50m, DateTimeOffset.UtcNow);
            entry.ChangeStatus(FinancialEntryStatus.Paid, DateTimeOffset.UtcNow);
            await db.SaveChangesAsync();

            Func<Task> act = () => service.MatchToEntry(tx.Id, entry.Id);

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task MatchToEntry_then_unmatch_should_reopen_the_entry()
        {
            FinancialAccount account = await SeedAccountAsync();
            BankTransaction tx = new(account.Id, "ext", DateTimeOffset.UtcNow, 50m, BankTransactionDirection.Credit, "x");
            db.Add(tx);
            FinancialEntry entry = await SeedEntryAsync(account.Id, 50m, DateTimeOffset.UtcNow.AddDays(3));
            await db.SaveChangesAsync();

            await service.MatchToEntry(tx.Id, entry.Id);
            FinancialEntry afterMatch = await db.Set<FinancialEntry>().AsNoTracking().FirstAsync(item => item.Id == entry.Id);
            afterMatch.Status.Should().Be(FinancialEntryStatus.Paid);

            await service.UnmatchFromEntry(tx.Id);

            FinancialEntry afterUnmatch = await db.Set<FinancialEntry>().AsNoTracking().FirstAsync(item => item.Id == entry.Id);
            afterUnmatch.Status.Should().Be(FinancialEntryStatus.Pending);
            afterUnmatch.PaidAt.Should().BeNull();
        }

        [Test]
        public async Task ImportBatch_auto_match_should_settle_the_entry()
        {
            FinancialAccount account = await SeedAccountAsync();
            DateTimeOffset now = DateTimeOffset.UtcNow;
            FinancialEntry entry = await SeedEntryAsync(account.Id, 250m, now, FinancialEntryType.Receivable);

            ImportBankTransactionsRequest request = new()
            {
                AccountId = account.Id,
                Transactions = new() { MakeItem("ext-credit", 250m, BankTransactionDirection.Credit, now) }
            };

            await service.ImportBatch(request);

            FinancialEntry reloaded = await db.Set<FinancialEntry>().AsNoTracking().FirstAsync(item => item.Id == entry.Id);
            reloaded.Status.Should().Be(FinancialEntryStatus.Paid);
        }

        [Test]
        public async Task MatchToEntry_rematch_to_different_entry_should_reopen_previous_and_settle_only_new()
        {
            FinancialAccount account = await SeedAccountAsync();
            DateTimeOffset txDate = DateTimeOffset.UtcNow.AddDays(-1);
            BankTransaction tx = new(account.Id, "ext", txDate, 50m, BankTransactionDirection.Credit, "x");
            db.Add(tx);
            FinancialEntry entryA = await SeedEntryAsync(account.Id, 50m, DateTimeOffset.UtcNow.AddDays(3));
            FinancialEntry entryB = await SeedEntryAsync(account.Id, 50m, DateTimeOffset.UtcNow.AddDays(3));
            await db.SaveChangesAsync();

            await service.MatchToEntry(tx.Id, entryA.Id);
            await service.MatchToEntry(tx.Id, entryB.Id);

            FinancialEntry reloadedA = await db.Set<FinancialEntry>().AsNoTracking().FirstAsync(item => item.Id == entryA.Id);
            FinancialEntry reloadedB = await db.Set<FinancialEntry>().AsNoTracking().FirstAsync(item => item.Id == entryB.Id);
            BankTransaction reloadedTx = await db.Set<BankTransaction>().AsNoTracking().FirstAsync(item => item.Id == tx.Id);

            reloadedA.Status.Should().Be(FinancialEntryStatus.Pending);
            reloadedA.PaidAt.Should().BeNull();
            reloadedB.Status.Should().Be(FinancialEntryStatus.Paid);
            reloadedTx.FinancialEntryId.Should().Be(entryB.Id);
        }

        [Test]
        public async Task MatchToEntry_to_same_entry_twice_should_be_idempotent()
        {
            FinancialAccount account = await SeedAccountAsync();
            BankTransaction tx = new(account.Id, "ext", DateTimeOffset.UtcNow.AddDays(-1), 50m, BankTransactionDirection.Credit, "x");
            db.Add(tx);
            FinancialEntry entry = await SeedEntryAsync(account.Id, 50m, DateTimeOffset.UtcNow.AddDays(3));
            await db.SaveChangesAsync();

            await service.MatchToEntry(tx.Id, entry.Id);
            Func<Task> secondMatch = () => service.MatchToEntry(tx.Id, entry.Id);

            await secondMatch.Should().NotThrowAsync();

            FinancialEntry reloaded = await db.Set<FinancialEntry>().AsNoTracking().FirstAsync(item => item.Id == entry.Id);
            reloaded.Status.Should().Be(FinancialEntryStatus.Paid);
            BankTransaction reloadedTx = await db.Set<BankTransaction>().AsNoTracking().FirstAsync(item => item.Id == tx.Id);
            reloadedTx.FinancialEntryId.Should().Be(entry.Id);
        }

        [Test]
        public async Task ImportBatch_should_auto_match_overdue_receivable()
        {
            FinancialAccount account = await SeedAccountAsync();
            FinancialEntry entry = new(account.Id, FinancialEntryType.Receivable, FinancialEntryCategory.BrandReceivable, "vencido", 250m, DateTimeOffset.UtcNow.AddDays(-5), DateTimeOffset.UtcNow);
            entry.RecalculateOverdue(DateTimeOffset.UtcNow);
            db.Add(entry);
            await db.SaveChangesAsync();

            ImportBankTransactionsRequest request = new()
            {
                AccountId = account.Id,
                Transactions = new() { MakeItem("ext-late", 250m, BankTransactionDirection.Credit, DateTimeOffset.UtcNow.AddDays(-4)) }
            };

            ImportBankTransactionsResult result = await service.ImportBatch(request);

            result.AutoMatched.Should().Be(1);
            FinancialEntry reloaded = await db.Set<FinancialEntry>().AsNoTracking().FirstAsync(item => item.Id == entry.Id);
            reloaded.Status.Should().Be(FinancialEntryStatus.Paid);
        }

        [Test]
        public async Task ImportBatch_should_auto_match_within_one_cent_tolerance()
        {
            FinancialAccount account = await SeedAccountAsync();
            DateTimeOffset now = DateTimeOffset.UtcNow;
            FinancialEntry entry = await SeedEntryAsync(account.Id, 100.01m, now, FinancialEntryType.Receivable);

            ImportBankTransactionsRequest request = new()
            {
                AccountId = account.Id,
                Transactions = new() { MakeItem("ext-cent", 100.00m, BankTransactionDirection.Credit, now) }
            };

            ImportBankTransactionsResult result = await service.ImportBatch(request);

            result.AutoMatched.Should().Be(1);
            FinancialEntry reloaded = await db.Set<FinancialEntry>().AsNoTracking().FirstAsync(item => item.Id == entry.Id);
            reloaded.Status.Should().Be(FinancialEntryStatus.Paid);
        }
    }
}
