using AgencyCampaign.Domain.ValueObjects;
using Archon.Core.Entities;

namespace AgencyCampaign.Domain.Entities
{
    public sealed class FinancialSubcategory : Entity
    {
        public string Name { get; private set; } = string.Empty;

        public FinancialEntryCategory MacroCategory { get; private set; }

        public string Color { get; private set; } = "#6366f1";

        public bool IsActive { get; private set; } = true;

        private FinancialSubcategory()
        {
        }

        public FinancialSubcategory(string name, FinancialEntryCategory macroCategory, string color)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(color);

            Name = name.Trim();
            MacroCategory = macroCategory;
            Color = color.Trim();
        }

        public void Update(string name, FinancialEntryCategory macroCategory, string color, bool isActive)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(color);

            Name = name.Trim();
            MacroCategory = macroCategory;
            Color = color.Trim();
            IsActive = isActive;
        }
    }
}
