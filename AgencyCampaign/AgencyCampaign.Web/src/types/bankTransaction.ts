export const BankTransactionDirection = {
  Credit: 1,
  Debit: 2,
} as const
export type BankTransactionDirectionValue = (typeof BankTransactionDirection)[keyof typeof BankTransactionDirection]

export const bankTransactionDirectionLabels: Record<number, string> = {
  1: 'Entrada',
  2: 'Saída',
}

export const BankTransactionMatchKind = {
  Auto: 1,
  Manual: 2,
} as const

export const bankTransactionMatchKindLabels: Record<number, string> = {
  1: 'Automática',
  2: 'Manual',
}

export interface BankTransaction {
  id: number
  accountId: number
  externalId: string
  occurredAt: string
  amount: number
  direction: number
  description: string
  category?: string | null
  financialEntryId?: number | null
  matchedAt?: string | null
  matchKind?: number | null
  importedAt: string
}
