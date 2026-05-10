export const EmailEventType = {
  ProposalSent: 1,
  ProposalApproved: 2,
  ProposalRejected: 3,
  ProposalConverted: 4,
  FollowUpDueSoon: 5,
  FollowUpOverdue: 6,
  OpportunityApprovalRequested: 7,
} as const

export type EmailEventTypeValue = (typeof EmailEventType)[keyof typeof EmailEventType]

export const emailEventTypeLabels: Record<EmailEventTypeValue, string> = {
  [EmailEventType.ProposalSent]: 'Proposta enviada',
  [EmailEventType.ProposalApproved]: 'Proposta aprovada',
  [EmailEventType.ProposalRejected]: 'Proposta rejeitada',
  [EmailEventType.ProposalConverted]: 'Proposta convertida em campanha',
  [EmailEventType.FollowUpDueSoon]: 'Follow-up próximo do vencimento',
  [EmailEventType.FollowUpOverdue]: 'Follow-up atrasado',
  [EmailEventType.OpportunityApprovalRequested]: 'Aprovação de oportunidade solicitada',
}

export interface EmailTemplateVariable {
  key: string
  label: string
  description: string
}

export type EmailTemplateVariableMap = Record<string, EmailTemplateVariable[]>

export interface EmailTemplate {
  id: number
  name: string
  eventType: EmailEventTypeValue
  subject: string
  htmlBody: string
  isActive: boolean
  createdByUserName?: string | null
  createdAt: string
  updatedAt?: string | null
}
