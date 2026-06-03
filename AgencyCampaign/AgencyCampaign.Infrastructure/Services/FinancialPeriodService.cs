using AgencyCampaign.Application.Models.Financial;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using Archon.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace AgencyCampaign.Infrastructure.Services
{
    // Fechamento de periodo mensal (D3c). Lista os meses recentes com seu status, fecha e reabre - registrando
    // o usuario que fechou/reabriu. A trava de escrita em si vive no FinancialEntryService (EnsurePeriodOpen).
    public sealed class FinancialPeriodService : IFinancialPeriodService
    {
        private readonly DbContext dbContext;
        private readonly ICurrentUser? currentUser;

        public FinancialPeriodService(DbContext dbContext, ICurrentUser? currentUser = null)
        {
            this.dbContext = dbContext;
            this.currentUser = currentUser;
        }

        public async Task<IReadOnlyList<FinancialPeriodModel>> GetRecentPeriods(int months, CancellationToken cancellationToken = default)
        {
            int horizon = Math.Clamp(months, 1, 36);
            DateTimeOffset now = DateTimeOffset.UtcNow;
            DateTimeOffset firstOfCurrentMonth = new(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);

            List<FinancialPeriod> rows = await dbContext.Set<FinancialPeriod>()
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            List<FinancialPeriodModel> result = [];
            for (int index = 0; index < horizon; index++)
            {
                DateTimeOffset month = firstOfCurrentMonth.AddMonths(-index);
                FinancialPeriod? row = rows.FirstOrDefault(item => item.Year == month.Year && item.Month == month.Month);
                result.Add(Map(month.Year, month.Month, row));
            }

            return result;
        }

        public async Task<FinancialPeriodModel> Close(int year, int month, CancellationToken cancellationToken = default)
        {
            long userId = RequireUser();
            FinancialPeriod period = await ResolveOrCreate(year, month, cancellationToken);
            period.Close(userId);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Map(period.Year, period.Month, period);
        }

        public async Task<FinancialPeriodModel> Reopen(int year, int month, CancellationToken cancellationToken = default)
        {
            long userId = RequireUser();
            FinancialPeriod period = await ResolveOrCreate(year, month, cancellationToken);
            period.Reopen(userId);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Map(period.Year, period.Month, period);
        }

        private async Task<FinancialPeriod> ResolveOrCreate(int year, int month, CancellationToken cancellationToken)
        {
            FinancialPeriod? existing = await dbContext.Set<FinancialPeriod>()
                .AsTracking()
                .FirstOrDefaultAsync(item => item.Year == year && item.Month == month, cancellationToken);

            if (existing is not null)
            {
                return existing;
            }

            FinancialPeriod created = new(year, month);
            dbContext.Set<FinancialPeriod>().Add(created);
            return created;
        }

        private long RequireUser()
        {
            long? userId = currentUser?.UserId;
            if (!userId.HasValue)
            {
                throw new InvalidOperationException("financialPeriod.userUnknown");
            }

            return userId.Value;
        }

        private static FinancialPeriodModel Map(int year, int month, FinancialPeriod? period) => new()
        {
            Year = year,
            Month = month,
            IsClosed = period?.IsClosed ?? false,
            ClosedAt = period?.ClosedAt,
            ClosedByUserId = period?.ClosedByUserId
        };
    }
}
