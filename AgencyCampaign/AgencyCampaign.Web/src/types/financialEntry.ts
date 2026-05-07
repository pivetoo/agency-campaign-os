export const FinancialEntryType = {
  Receivable: 1,
  Payable: 2,
} as const

export type FinancialEntryTypeValue = (typeof FinancialEntryType)[keyof typeof FinancialEntryType]

export const FinancialEntryStatus = {
  Pending: 1,
  Paid: 2,
  Overdue: 3,
  Cancelled: 4,
} as const

export type FinancialEntryStatusValue = (typeof FinancialEntryStatus)[keyof typeof FinancialEntryStatus]

export const financialEntryStatusLabels: Record<number, string> = {
  1: 'Pendente',
  2: 'Pago',
  3: 'Vencido',
  4: 'Cancelado',
}

export const financialEntryReceivableStatusLabels: Record<number, string> = {
  1: 'A receber',
  2: 'Recebido',
  3: 'Vencido',
  4: 'Cancelado',
}

export const financialEntryCategoryLabels: Record<number, string> = {
  1: 'Recebível da marca',
  2: 'Repasse creator',
  3: 'Fee da agência',
  4: 'Custo operacional',
  5: 'Bônus',
  6: 'Ajuste',
  7: 'Reembolso',
  8: 'Imposto',
}

export interface FinancialEntry {
  id: number
  accountId: number
  accountName?: string | null
  accountColor?: string | null
  campaignId?: number | null
  campaignName?: string | null
  campaignDeliverableId?: number
  type: number
  category: number
  description: string
  amount: number
  dueAt: string
  occurredAt: string
  paymentMethod?: string
  referenceCode?: string
  paidAt?: string
  status: number
  counterpartyName?: string
  notes?: string
  createdAt: string
  updatedAt?: string
}

export interface FinancialSummary {
  type: number
  totalPending: number
  totalSettledThisMonth: number
  totalOverdue: number
  totalDueNext7Days: number
  pendingCount: number
  overdueCount: number
}
