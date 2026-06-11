using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.Opportunities
{
    public sealed class CloseOpportunityAsWonRequest
    {
        [StringLength(1000)]
        public string? WonNotes { get; set; }

        public long? WinReasonId { get; set; }

        // Valor fechado informado pelo operador; quando ausente, o servico resolve pela proposta aceita
        [Range(0, 1000000000)]
        public decimal? ClosedValue { get; set; }
    }
}
