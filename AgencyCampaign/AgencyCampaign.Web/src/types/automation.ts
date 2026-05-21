export const AutomationTriggerType = {
  Event: 1,
  UserAction: 2,
} as const

export type AutomationTriggerTypeValue = (typeof AutomationTriggerType)[keyof typeof AutomationTriggerType]

export interface Automation {
  id: number
  name: string
  trigger: string
  triggerType: AutomationTriggerTypeValue
  triggerCondition?: string
  connectorId: number
  pipelineId: number
  variableMappingJson: string
  isActive: boolean
  createdAt: string
  updatedAt?: string
}

export interface CreateAutomationPayload {
  name: string
  trigger: string
  triggerType?: AutomationTriggerTypeValue
  triggerCondition?: string
  connectorId: number
  pipelineId: number
  variableMapping: Record<string, string>
  isActive: boolean
}

export interface UpdateAutomationPayload {
  id: number
  name: string
  trigger: string
  triggerType?: AutomationTriggerTypeValue
  triggerCondition?: string
  connectorId: number
  pipelineId: number
  variableMapping: Record<string, string>
  isActive: boolean
}

export interface AutomationExecutionLog {
  id: number
  automationId: number
  automationName: string
  trigger: string
  succeeded: boolean
  renderedPayload?: string
  errorMessage?: string
  createdAt: string
}

export interface IntegrationIntentDescriptor {
  key: string
  label: string
  categoryIdentifier: string
  serviceContractIdentifier: string
}

export const IntegrationIntents = {
  ProposalSendEmail: 'proposal.send-email',
  ProposalSendWhatsapp: 'proposal.send-whatsapp',
  CampaignDocumentSendSignature: 'campaign-document.send-signature',
  CampaignDocumentSendEmail: 'campaign-document.send-email',
  CampaignDocumentSendWhatsapp: 'campaign-document.send-whatsapp',
  CreatorPaymentSchedulePix: 'creator-payment.schedule-pix',
  NotificationSendTransactional: 'notification.send-transactional',
  ReceivableIssueInvoice: 'receivable.issue-invoice',
  PayableTransfer: 'payable.transfer',
  CreatorPortalNotifyWhatsapp: 'creator-portal.notify-whatsapp',
} as const

export type IntegrationIntentKey = (typeof IntegrationIntents)[keyof typeof IntegrationIntents]
