export const FinancialAccountType = {
  Bank: 1,
  Cash: 2,
  Wallet: 3,
  CreditCard: 4,
} as const

export type FinancialAccountTypeValue = (typeof FinancialAccountType)[keyof typeof FinancialAccountType]

export const financialAccountTypeLabels: Record<number, string> = {
  1: 'Banco',
  2: 'Caixa',
  3: 'Carteira/Pix',
  4: 'Cartão de crédito',
}

export interface FinancialAccount {
  id: number
  name: string
  type: number
  bank?: string | null
  agency?: string | null
  number?: string | null
  initialBalance: number
  currentBalance: number
  color: string
  isActive: boolean
}
