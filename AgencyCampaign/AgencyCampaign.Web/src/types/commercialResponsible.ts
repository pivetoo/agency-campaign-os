export interface CommercialResponsible {
  id: number
  userId: number
  name: string
  email?: string
  phone?: string
  notes?: string
  isActive: boolean
  createdAt: string
  updatedAt?: string
}

export interface CommercialUser {
  id: number
  username: string
  email: string
  name: string
  avatarUrl?: string
  isActive: boolean
}
