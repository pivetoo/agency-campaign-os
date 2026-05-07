using AgencyCampaign.Domain.ValueObjects;
using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class FinancialAccount : Entity
    {
        public string Name { get; private set; } = string.Empty;

        public FinancialAccountType Type { get; private set; }

        public string? Bank { get; private set; }

        public string? Agency { get; private set; }

        public string? Number { get; private set; }

        public decimal InitialBalance { get; private set; }

        public string Color { get; private set; } = "#6366f1";

        public bool IsActive { get; private set; } = true;

        private FinancialAccount()
        {
        }

        public FinancialAccount(string name, FinancialAccountType type, decimal initialBalance, string color, string? bank = null, string? agency = null, string? number = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(color);

            Name = name.Trim();
            Type = type;
            InitialBalance = initialBalance;
            Color = color.Trim();
            Bank = Normalize(bank);
            Agency = Normalize(agency);
            Number = Normalize(number);
        }

        public void Update(string name, FinancialAccountType type, decimal initialBalance, string color, string? bank, string? agency, string? number, bool isActive)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(color);

            Name = name.Trim();
            Type = type;
            InitialBalance = initialBalance;
            Color = color.Trim();
            Bank = Normalize(bank);
            Agency = Normalize(agency);
            Number = Normalize(number);
            IsActive = isActive;
        }

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
