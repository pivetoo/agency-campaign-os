import type { PixKeyTypeValue } from './creatorPayment'

export interface Creator {
  id: number
  name: string
  stageName?: string
  email?: string
  phone?: string
  document?: string
  pixKey?: string
  pixKeyType?: PixKeyTypeValue
  taxRegime?: TaxRegimeValue
  primaryNiche?: string
  city?: string
  state?: string
  notes?: string
  defaultAgencyFeePercent: number
  photoUrl?: string
  isActive: boolean
  createdAt: string
  updatedAt?: string
}

export const TaxRegime = {
  IndividualPF: 1,
  Mei: 2,
  SimplesNacional: 3,
  PresumedRealProfit: 4,
} as const
export type TaxRegimeValue = (typeof TaxRegime)[keyof typeof TaxRegime]

export const taxRegimeLabels: Record<TaxRegimeValue, string> = {
  1: 'Pessoa Física (PF)',
  2: 'MEI',
  3: 'PJ - Simples Nacional',
  4: 'PJ - Lucro Presumido/Real',
}
