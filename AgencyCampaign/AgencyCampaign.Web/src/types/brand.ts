export interface Brand {
  id: number
  name: string
  tradeName?: string
  document?: string
  contactName?: string
  contactEmail?: string
  contactPhone?: string
  notes?: string
  logoUrl?: string
  isActive: boolean
  createdAt: string
  updatedAt?: string
}
