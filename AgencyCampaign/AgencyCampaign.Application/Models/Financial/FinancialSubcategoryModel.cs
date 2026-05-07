using AgencyCampaign.Domain.ValueObjects;

namespace AgencyCampaign.Application.Models.Financial
{
    public sealed class FinancialSubcategoryModel
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public FinancialEntryCategory MacroCategory { get; set; }
        public string Color { get; set; } = "#6366f1";
        public bool IsActive { get; set; }
    }
}
