namespace AgencyCampaign.Domain.ValueObjects
{
    public static class AutomationTriggers
    {
        public const string ProposalSent = "proposal_sent";
        public const string ProposalApproved = "proposal_approved";
        public const string ProposalRejected = "proposal_rejected";
        public const string ProposalConverted = "proposal_converted";

        public const string OpportunityStageChanged = "opportunity_stage_changed";
        public const string FollowUpOverdue = "follow_up_overdue";

        public const string CampaignCreated = "campaign_created";
        public const string DeliverablePublished = "deliverable_published";
        public const string DeliverableBrandApproved = "deliverable_brand_approved";
        public const string DeliverableBrandRejected = "deliverable_brand_rejected";

        public const string FinancialReceivableCreated = "financial_receivable_created";
        public const string FinancialReceivableSettled = "financial_receivable_settled";
        public const string FinancialPayableCreated = "financial_payable_created";
        public const string FinancialPayableSettled = "financial_payable_settled";
        public const string FinancialOverdue = "financial_overdue";

        public static readonly IReadOnlyDictionary<string, string> Labels = new Dictionary<string, string>
        {
            [ProposalSent] = "Proposta enviada",
            [ProposalApproved] = "Proposta aprovada",
            [ProposalRejected] = "Proposta rejeitada",
            [ProposalConverted] = "Proposta convertida em campanha",
            [OpportunityStageChanged] = "Oportunidade mudou de estágio",
            [FollowUpOverdue] = "Follow-up atrasado",
            [CampaignCreated] = "Campanha criada",
            [DeliverablePublished] = "Entrega publicada",
            [DeliverableBrandApproved] = "Entrega aprovada pela marca",
            [DeliverableBrandRejected] = "Entrega rejeitada pela marca",
            [FinancialReceivableCreated] = "Conta a receber criada",
            [FinancialReceivableSettled] = "Conta recebida",
            [FinancialPayableCreated] = "Conta a pagar criada",
            [FinancialPayableSettled] = "Conta paga",
            [FinancialOverdue] = "Lançamento financeiro vencido"
        };
    }
}
