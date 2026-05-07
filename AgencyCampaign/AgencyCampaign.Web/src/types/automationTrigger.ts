export const AutomationTriggers = {
  ProposalSent: 'proposal_sent',
  ProposalApproved: 'proposal_approved',
  ProposalRejected: 'proposal_rejected',
  ProposalConverted: 'proposal_converted',
  OpportunityStageChanged: 'opportunity_stage_changed',
  FollowUpOverdue: 'follow_up_overdue',
  CampaignCreated: 'campaign_created',
  DeliverablePublished: 'deliverable_published',
  DeliverableBrandApproved: 'deliverable_brand_approved',
  DeliverableBrandRejected: 'deliverable_brand_rejected',
  FinancialReceivableCreated: 'financial_receivable_created',
  FinancialReceivableSettled: 'financial_receivable_settled',
  FinancialPayableCreated: 'financial_payable_created',
  FinancialPayableSettled: 'financial_payable_settled',
  FinancialOverdue: 'financial_overdue',
} as const

export type AutomationTriggerValue = (typeof AutomationTriggers)[keyof typeof AutomationTriggers]

export const automationTriggerLabels: Record<string, string> = {
  proposal_sent: 'Proposta enviada',
  proposal_approved: 'Proposta aprovada',
  proposal_rejected: 'Proposta rejeitada',
  proposal_converted: 'Proposta convertida em campanha',
  opportunity_stage_changed: 'Oportunidade mudou de estágio',
  follow_up_overdue: 'Follow-up atrasado',
  campaign_created: 'Campanha criada',
  deliverable_published: 'Entrega publicada',
  deliverable_brand_approved: 'Entrega aprovada pela marca',
  deliverable_brand_rejected: 'Entrega rejeitada pela marca',
  financial_receivable_created: 'Conta a receber criada',
  financial_receivable_settled: 'Conta recebida',
  financial_payable_created: 'Conta a pagar criada',
  financial_payable_settled: 'Conta paga',
  financial_overdue: 'Lançamento financeiro vencido',
}

export const automationTriggerGroups = [
  {
    label: 'Comercial',
    triggers: [
      AutomationTriggers.ProposalSent,
      AutomationTriggers.ProposalApproved,
      AutomationTriggers.ProposalRejected,
      AutomationTriggers.ProposalConverted,
      AutomationTriggers.OpportunityStageChanged,
      AutomationTriggers.FollowUpOverdue,
    ],
  },
  {
    label: 'Operação',
    triggers: [
      AutomationTriggers.CampaignCreated,
      AutomationTriggers.DeliverablePublished,
      AutomationTriggers.DeliverableBrandApproved,
      AutomationTriggers.DeliverableBrandRejected,
    ],
  },
  {
    label: 'Financeiro',
    triggers: [
      AutomationTriggers.FinancialReceivableCreated,
      AutomationTriggers.FinancialReceivableSettled,
      AutomationTriggers.FinancialPayableCreated,
      AutomationTriggers.FinancialPayableSettled,
      AutomationTriggers.FinancialOverdue,
    ],
  },
] as const
