using AgencyCampaign.Application.Models.Commercial;
using AgencyCampaign.Domain.ValueObjects;

namespace AgencyCampaign.Application.Catalogs
{
    public static class EmailTemplateVariableCatalog
    {
        private static readonly EmailTemplateVariableModel[] ProposalVariables =
        [
            new() { Key = "proposalName", Label = "Nome da proposta", Description = "Nome da proposta enviada" },
            new() { Key = "proposalId", Label = "Id da proposta", Description = "Identificador interno da proposta" },
            new() { Key = "totalValue", Label = "Valor total", Description = "Valor total da proposta" },
            new() { Key = "validityUntil", Label = "Validade da proposta", Description = "Data limite de validade da proposta" },
            new() { Key = "opportunityName", Label = "Nome da oportunidade", Description = "Nome da oportunidade vinculada" },
            new() { Key = "brandName", Label = "Nome da marca", Description = "Marca atendida na oportunidade" },
            new() { Key = "contactName", Label = "Nome do contato", Description = "Pessoa responsavel pelo lado da marca" },
            new() { Key = "contactEmail", Label = "E-mail do contato", Description = "E-mail do contato responsavel" },
            new() { Key = "responsibleName", Label = "Responsavel interno", Description = "Responsavel interno pela proposta" },
        ];

        public static IReadOnlyDictionary<EmailEventType, IReadOnlyList<EmailTemplateVariableModel>> All { get; } =
            new Dictionary<EmailEventType, IReadOnlyList<EmailTemplateVariableModel>>
            {
                [EmailEventType.ProposalSent] = ProposalVariables,
                [EmailEventType.ProposalApproved] = ProposalVariables,
                [EmailEventType.ProposalRejected] = ProposalVariables,
                [EmailEventType.ProposalConverted] = ProposalVariables,
                [EmailEventType.FollowUpDueSoon] = ProposalVariables,
                [EmailEventType.FollowUpOverdue] = ProposalVariables,
                [EmailEventType.OpportunityApprovalRequested] = ProposalVariables,
            };
    }
}
