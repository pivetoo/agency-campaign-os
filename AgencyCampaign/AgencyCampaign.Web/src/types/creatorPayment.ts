export const PaymentStatus = {
  Pending: 1,
  Scheduled: 2,
  Paid: 3,
  Failed: 4,
  Cancelled: 5,
} as const
export type PaymentStatusValue = (typeof PaymentStatus)[keyof typeof PaymentStatus]

export const paymentStatusLabels: Record<PaymentStatusValue, string> = {
  1: 'Pendente',
  2: 'Agendado',
  3: 'Pago',
  4: 'Falhou',
  5: 'Cancelado',
}

export const PaymentMethod = {
  Pix: 1,
  Ted: 2,
  Manual: 3,
} as const
export type PaymentMethodValue = (typeof PaymentMethod)[keyof typeof PaymentMethod]

export const paymentMethodLabels: Record<PaymentMethodValue, string> = {
  1: 'PIX',
  2: 'TED',
  3: 'Manual',
}

export const PixKeyType = {
  Cpf: 1,
  Cnpj: 2,
  Email: 3,
  Phone: 4,
  Random: 5,
} as const
export type PixKeyTypeValue = (typeof PixKeyType)[keyof typeof PixKeyType]

export const pixKeyTypeLabels: Record<PixKeyTypeValue, string> = {
  1: 'CPF',
  2: 'CNPJ',
  3: 'E-mail',
  4: 'Telefone',
  5: 'Aleatória',
}

export const CreatorPaymentEventType = {
  Created: 1,
  Updated: 2,
  Scheduled: 3,
  ProviderAccepted: 4,
  Paid: 5,
  Failed: 6,
  Cancelled: 7,
  InvoiceAttached: 8,
  ProviderSyncError: 9,
} as const
export type CreatorPaymentEventTypeValue =
  (typeof CreatorPaymentEventType)[keyof typeof CreatorPaymentEventType]

export const creatorPaymentEventTypeLabels: Record<CreatorPaymentEventTypeValue, string> = {
  1: 'Criado',
  2: 'Atualizado',
  3: 'Agendado',
  4: 'Aceito pelo provider',
  5: 'Pago',
  6: 'Falhou',
  7: 'Cancelado',
  8: 'NF anexada',
  9: 'Erro de sincronização',
}

export interface CreatorPaymentEvent {
  id: number
  eventType: CreatorPaymentEventTypeValue
  occurredAt: string
  description?: string
  metadata?: string
}

export interface CreatorPayment {
  id: number
  campaignCreatorId: number
  creatorId: number
  creatorName?: string
  creatorPixKey?: string
  creatorPixKeyType?: PixKeyTypeValue
  campaignId?: number
  campaignName?: string
  campaignDocumentId?: number
  grossAmount: number
  discounts: number
  netAmount: number
  description?: string
  method: PaymentMethodValue
  status: PaymentStatusValue
  provider?: string
  providerTransactionId?: string
  pixKey?: string
  pixKeyType?: PixKeyTypeValue
  invoiceNumber?: string
  invoiceUrl?: string
  invoiceIssuedAt?: string
  scheduledFor?: string
  paidAt?: string
  failedAt?: string
  failureReason?: string
  createdAt: string
  updatedAt?: string
  events: CreatorPaymentEvent[]
}
