using AgencyCampaign.Domain.ValueObjects;
using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.FinancialSubcategories
{
    public class CreateFinancialSubcategoryRequest
    {
        [Required]
        [StringLength(120, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public FinancialEntryCategory MacroCategory { get; set; }

        [Required]
        [StringLength(32)]
        public string Color { get; set; } = "#6366f1";
    }

    public sealed class UpdateFinancialSubcategoryRequest : CreateFinancialSubcategoryRequest
    {
        [Required]
        public long Id { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
