using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    // Fechamento de periodo (D3c): trava contabil mensal. Um mes fechado bloqueia criar/editar/marcar-pago
    // lancamentos com data dentro dele (back-dating); o estorno continua liberado porque lanca a contrapartida
    // no mes ABERTO corrente (correcao contabil correta, sem reescrever o mes fechado). Reabertura registra
    // quem reabriu. Identificado por (Year, Month) unico.
    public sealed class FinancialPeriod : Entity
    {
        public int Year { get; private set; }

        public int Month { get; private set; }

        public bool IsClosed { get; private set; }

        public DateTimeOffset? ClosedAt { get; private set; }

        public long? ClosedByUserId { get; private set; }

        public DateTimeOffset? ReopenedAt { get; private set; }

        public long? ReopenedByUserId { get; private set; }

        private FinancialPeriod()
        {
        }

        public FinancialPeriod(int year, int month)
        {
            if (year < 2000 || year > 3000)
            {
                throw new ArgumentOutOfRangeException(nameof(year));
            }

            if (month < 1 || month > 12)
            {
                throw new ArgumentOutOfRangeException(nameof(month));
            }

            Year = year;
            Month = month;
        }

        public void Close(long userId)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(userId);

            if (IsClosed)
            {
                throw new InvalidOperationException("financialPeriod.alreadyClosed");
            }

            IsClosed = true;
            ClosedAt = DateTimeOffset.UtcNow;
            ClosedByUserId = userId;
            ReopenedAt = null;
            ReopenedByUserId = null;
        }

        public void Reopen(long userId)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(userId);

            if (!IsClosed)
            {
                throw new InvalidOperationException("financialPeriod.notClosed");
            }

            IsClosed = false;
            ReopenedAt = DateTimeOffset.UtcNow;
            ReopenedByUserId = userId;
        }
    }
}
