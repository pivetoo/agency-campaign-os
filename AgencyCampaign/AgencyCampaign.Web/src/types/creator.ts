export interface Creator {
  id: number
  name: string
  stageName?: string
  email?: string
  phone?: string
  document?: string
  pixKey?: string
  primaryNiche?: string
  city?: string
  state?: string
  notes?: string
  defaultAgencyFeePercent: number
  isActive: boolean
  createdAt: string
  updatedAt?: string
}
