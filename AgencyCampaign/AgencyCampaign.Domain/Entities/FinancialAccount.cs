using AgencyCampaign.Domain.ValueObjects;
using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class FinancialAccount : Entity
    {
        public string Name { get; private set; } = string.Empty;

        public FinancialAccountType Type { get; private set; }

        public long? BankId { get; private set; }

        public string? Bank { get; private set; }

        public string? Agency { get; private set; }

        public string? Number { get; private set; }

        public decimal InitialBalance { get; private set; }

        public string Color { get; private set; } = "#6366f1";

        public bool IsActive { get; private set; } = true;

        // Conta padrao da agencia: a auto-geracao (recebivel da marca, repasse) usa esta conta.
        public bool IsDefault { get; private set; }

        public long? IntegrationConnectorId { get; private set; }

        public decimal? LastSyncedBalance { get; private set; }

        public DateTimeOffset? LastSyncedAt { get; private set; }

        public FinancialAccountSyncStatus SyncStatus { get; private set; } = FinancialAccountSyncStatus.NotConfigured;

        private FinancialAccount()
        {
        }

        public FinancialAccount(string name, FinancialAccountType type, decimal initialBalance, string? color = null, long? bankId = null, string? bank = null, string? agency = null, string? number = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            Name = name.Trim();
            Type = type;
            InitialBalance = initialBalance;
            if (!string.IsNullOrWhiteSpace(color))
            {
                Color = color.Trim();
            }
            BankId = bankId;
            Bank = Normalize(bank);
            Agency = Normalize(agency);
            Number = Normalize(number);
        }

        public void Update(string name, FinancialAccountType type, decimal initialBalance, string? color, long? bankId, string? bank, string? agency, string? number, bool isActive)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            Name = name.Trim();
            Type = type;
            InitialBalance = initialBalance;
            if (!string.IsNullOrWhiteSpace(color))
            {
                Color = color.Trim();
            }
            BankId = bankId;
            Bank = Normalize(bank);
            Agency = Normalize(agency);
            Number = Normalize(number);
            IsActive = isActive;
        }

        public void SetAsDefault(bool isDefault)
        {
            IsDefault = isDefault;
        }

        public void AttachConnector(long connectorId)
        {
            if (connectorId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(connectorId));
            }

            IntegrationConnectorId = connectorId;
            SyncStatus = FinancialAccountSyncStatus.Pending;
        }

        public void DetachConnector()
        {
            IntegrationConnectorId = null;
            LastSyncedBalance = null;
            LastSyncedAt = null;
            SyncStatus = FinancialAccountSyncStatus.NotConfigured;
        }

        public void MarkSynced(decimal balance, DateTimeOffset syncedAt)
        {
            LastSyncedBalance = balance;
            LastSyncedAt = syncedAt;
            SyncStatus = FinancialAccountSyncStatus.Synced;
        }

        public void MarkSyncError(DateTimeOffset failedAt)
        {
            LastSyncedAt = failedAt;
            SyncStatus = FinancialAccountSyncStatus.Error;
        }

        public void MarkSyncPending()
        {
            if (IntegrationConnectorId is null)
            {
                throw new InvalidOperationException("financialAccount.sync.cannotPendWithoutConnector");
            }

            SyncStatus = FinancialAccountSyncStatus.Pending;
        }

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
