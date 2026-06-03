using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.FinancialEntries
{
    public sealed class FinancialPeriodRequest
    {
        [Range(2000, 3000)]
        public int Year { get; set; }

        [Range(1, 12)]
        public int Month { get; set; }
    }
}
