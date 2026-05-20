export interface AgencyIntegrationBinding {
  id: number
  intentKey: string
  connectorId: number
  pipelineId: number
  isActive: boolean
  createdByUserName?: string
  createdAt: string
  updatedAt?: string
}

export interface IntegrationIntentDescriptor {
  key: string
  label: string
  categoryIdentifier: string
}

export interface SaveAgencyIntegrationBindingRequest {
  intentKey: string
  connectorId: number
  pipelineId: number
  isActive: boolean
}

export const IntegrationIntents = {
  ProposalSendEmail: 'proposal.send-email',
  CampaignDocumentSendSignature: 'campaign-document.send-signature',
  CampaignDocumentSendEmail: 'campaign-document.send-email',
  CreatorPaymentSchedulePix: 'creator-payment.schedule-pix',
  NotificationSendTransactional: 'notification.send-transactional',
  ReceivableIssueInvoice: 'receivable.issue-invoice',
  PayableTransfer: 'payable.transfer',
  FinancialEntryIssueNf: 'financial-entry.issue-nf',
  BankAccountSync: 'bank-account.sync',
  CreatorPortalNotifyWhatsapp: 'creator-portal.notify-whatsapp',
} as const

export type IntegrationIntentKey = (typeof IntegrationIntents)[keyof typeof IntegrationIntents]
