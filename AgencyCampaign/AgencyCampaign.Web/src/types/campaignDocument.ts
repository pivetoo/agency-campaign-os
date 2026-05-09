export const CampaignDocumentType = {
  CreatorAgreement: 1,
  BrandContract: 2,
  AuthorizationTerm: 3,
  BriefingAttachment: 4,
  Other: 5,
} as const
export type CampaignDocumentTypeValue =
  (typeof CampaignDocumentType)[keyof typeof CampaignDocumentType]

export const campaignDocumentTypeLabels: Record<CampaignDocumentTypeValue, string> = {
  1: 'Aceite do creator',
  2: 'Contrato da marca',
  3: 'Termo de autorização',
  4: 'Anexo de briefing',
  5: 'Outro',
}

export const CampaignDocumentStatus = {
  Draft: 1,
  ReadyToSend: 2,
  Sent: 3,
  Viewed: 4,
  Signed: 5,
  Rejected: 6,
  Cancelled: 7,
} as const
export type CampaignDocumentStatusValue =
  (typeof CampaignDocumentStatus)[keyof typeof CampaignDocumentStatus]

export const campaignDocumentStatusLabels: Record<CampaignDocumentStatusValue, string> = {
  1: 'Rascunho',
  2: 'Pronto para enviar',
  3: 'Enviado',
  4: 'Visualizado',
  5: 'Assinado',
  6: 'Recusado',
  7: 'Cancelado',
}

export const CampaignDocumentSignerRole = {
  Creator: 1,
  Agency: 2,
  Brand: 3,
  Other: 4,
} as const
export type CampaignDocumentSignerRoleValue =
  (typeof CampaignDocumentSignerRole)[keyof typeof CampaignDocumentSignerRole]

export const campaignDocumentSignerRoleLabels: Record<CampaignDocumentSignerRoleValue, string> = {
  1: 'Creator',
  2: 'Agência',
  3: 'Marca',
  4: 'Outro',
}

export const CampaignDocumentEventType = {
  Created: 1,
  ReadyToSend: 2,
  Sent: 3,
  Viewed: 4,
  SignerSigned: 5,
  Signed: 6,
  Rejected: 7,
  Cancelled: 8,
  ProviderSyncError: 9,
} as const
export type CampaignDocumentEventTypeValue =
  (typeof CampaignDocumentEventType)[keyof typeof CampaignDocumentEventType]

export const campaignDocumentEventTypeLabels: Record<CampaignDocumentEventTypeValue, string> = {
  1: 'Documento criado',
  2: 'Pronto para enviar',
  3: 'Enviado',
  4: 'Visualizado',
  5: 'Signatário assinou',
  6: 'Documento assinado',
  7: 'Recusado',
  8: 'Cancelado',
  9: 'Erro de sincronização',
}

export interface CampaignDocumentSignature {
  id: number
  role: CampaignDocumentSignerRoleValue
  signerName: string
  signerEmail: string
  signerDocumentNumber?: string
  providerSignerId?: string
  signedAt?: string
  ipAddress?: string
  isSigned: boolean
}

export interface CampaignDocumentEvent {
  id: number
  eventType: CampaignDocumentEventTypeValue
  occurredAt: string
  description?: string
  metadata?: string
}

export interface CampaignDocument {
  id: number
  campaignId: number
  campaignCreatorId?: number
  templateId?: number
  templateName?: string
  documentType: CampaignDocumentTypeValue
  title: string
  documentUrl?: string
  body?: string
  provider?: string
  providerDocumentId?: string
  signedDocumentUrl?: string
  status: CampaignDocumentStatusValue
  recipientEmail?: string
  emailSubject?: string
  emailBody?: string
  sentAt?: string
  signedAt?: string
  notes?: string
  createdAt: string
  updatedAt?: string
  signatures: CampaignDocumentSignature[]
  events: CampaignDocumentEvent[]
}
