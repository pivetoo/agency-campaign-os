export interface Creator {
  id: number
  name: string
  email?: string
  phone?: string
  document?: string
  pixKey?: string
  isActive: boolean
  createdAt: string
  updatedAt?: string
}
