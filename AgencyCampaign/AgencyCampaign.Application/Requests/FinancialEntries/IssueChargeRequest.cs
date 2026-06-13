using System.ComponentModel.DataAnnotations;

namespace AgencyCampaign.Application.Requests.FinancialEntries
{
    // Disparo da emissao de cobranca (boleto/PIX) de um recebivel. Os dados do lancamento (valor, vencimento,
    // descricao) ja vem do FinancialEntry; aqui sao apenas complementos do pagador/metodo repassados ao conector.
    public sealed class IssueChargeRequest
    {
        [StringLength(150)]
        public string? PayerName { get; set; }

        [StringLength(20)]
        public string? PayerDocument { get; set; }

        // boleto | pix | boleto_pix — interpretado pelo conector do IntegrationPlatform.
        [StringLength(20)]
        public string? Method { get; set; }
    }
}
