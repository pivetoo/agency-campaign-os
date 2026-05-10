using AgencyCampaign.Application.Models.Commercial;
using AgencyCampaign.Domain.ValueObjects;

namespace AgencyCampaign.Application.Catalogs
{
    public static class CampaignDocumentTemplateVariableCatalog
    {
        private const string GeneralGroup = "Geral";
        private const string CampaignGroup = "Campanha";
        private const string BrandGroup = "Marca";
        private const string CreatorGroup = "Creator";

        private static readonly CampaignDocumentTemplateVariableModel[] Variables =
        [
            new() { Key = "today", Label = "Data atual", Description = "Data de geracao do documento", Group = GeneralGroup },
            new() { Key = "campaignId", Label = "Id da campanha", Description = "Identificador interno da campanha", Group = GeneralGroup },

            new() { Key = "campaignName", Label = "Nome da campanha", Description = "Nome da campanha vinculada", Group = CampaignGroup },
            new() { Key = "campaignDescription", Label = "Descricao da campanha", Description = "Descricao livre da campanha", Group = CampaignGroup },
            new() { Key = "campaignObjective", Label = "Objetivo", Description = "Objetivo principal da campanha", Group = CampaignGroup },
            new() { Key = "campaignBriefing", Label = "Briefing", Description = "Briefing da campanha", Group = CampaignGroup },
            new() { Key = "campaignStartDate", Label = "Data de inicio", Description = "Inicio previsto da campanha", Group = CampaignGroup },
            new() { Key = "campaignEndDate", Label = "Data de termino", Description = "Termino previsto da campanha", Group = CampaignGroup },
            new() { Key = "campaignBudget", Label = "Orcamento", Description = "Orcamento total da campanha", Group = CampaignGroup },

            new() { Key = "brandName", Label = "Nome da marca", Description = "Razao social da marca", Group = BrandGroup },
            new() { Key = "brandTradeName", Label = "Nome fantasia", Description = "Nome fantasia da marca", Group = BrandGroup },
            new() { Key = "brandDocument", Label = "Documento da marca", Description = "CNPJ da marca", Group = BrandGroup },
            new() { Key = "brandContactName", Label = "Contato da marca", Description = "Pessoa responsavel do lado da marca", Group = BrandGroup },
            new() { Key = "brandContactEmail", Label = "E-mail da marca", Description = "E-mail do contato da marca", Group = BrandGroup },

            new() { Key = "creatorName", Label = "Nome do creator", Description = "Nome legal do creator", Group = CreatorGroup },
            new() { Key = "creatorStageName", Label = "Nome artistico", Description = "Nome publico do creator", Group = CreatorGroup },
            new() { Key = "creatorEmail", Label = "E-mail do creator", Description = "E-mail principal do creator", Group = CreatorGroup },
            new() { Key = "creatorDocument", Label = "Documento do creator", Description = "CPF do creator", Group = CreatorGroup },
            new() { Key = "creatorAgreedAmount", Label = "Valor combinado", Description = "Valor combinado com o creator", Group = CreatorGroup },
            new() { Key = "creatorAgencyFeePercent", Label = "Comissao da agencia (%)", Description = "Percentual de comissao retido pela agencia", Group = CreatorGroup },
            new() { Key = "creatorAgencyFeeAmount", Label = "Comissao da agencia (valor)", Description = "Valor absoluto da comissao da agencia", Group = CreatorGroup },
            new() { Key = "scopeNotes", Label = "Observacoes do escopo", Description = "Notas adicionais do escopo do creator", Group = CreatorGroup },
        ];

        public static IReadOnlyDictionary<CampaignDocumentType, IReadOnlyList<CampaignDocumentTemplateVariableModel>> All { get; } =
            Enum.GetValues<CampaignDocumentType>()
                .ToDictionary(type => type, _ => (IReadOnlyList<CampaignDocumentTemplateVariableModel>)Variables);
    }
}
