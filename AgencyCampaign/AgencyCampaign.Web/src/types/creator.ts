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
