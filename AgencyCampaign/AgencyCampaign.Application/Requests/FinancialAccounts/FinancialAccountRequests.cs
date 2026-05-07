using AgencyCampaign.Domain.ValueObjects;
using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.FinancialAccounts
{
    public class CreateFinancialAccountRequest
    {
        [Required]
        [StringLength(120, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public FinancialAccountType Type { get; set; }

        [StringLength(120)]
        public string? Bank { get; set; }

        [StringLength(50)]
        public string? Agency { get; set; }

        [StringLength(50)]
        public string? Number { get; set; }

        public decimal InitialBalance { get; set; }

        [Required]
        [StringLength(32)]
        public string Color { get; set; } = "#6366f1";
    }

    public sealed class UpdateFinancialAccountRequest : CreateFinancialAccountRequest
    {
        [Required]
        public long Id { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
