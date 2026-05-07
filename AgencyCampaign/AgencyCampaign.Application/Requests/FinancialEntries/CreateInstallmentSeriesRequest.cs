using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.FinancialEntries
{
    public sealed class CreateInstallmentSeriesRequest : CreateFinancialEntryRequest
    {
        [Required]
        [Range(2, 60)]
        public int InstallmentTotal { get; set; }
    }
}
