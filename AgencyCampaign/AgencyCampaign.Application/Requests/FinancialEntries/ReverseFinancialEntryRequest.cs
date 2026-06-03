using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.FinancialEntries
{
    public sealed class ReverseFinancialEntryRequest
    {
        [StringLength(500)]
        public string? Reason { get; set; }
    }
}
