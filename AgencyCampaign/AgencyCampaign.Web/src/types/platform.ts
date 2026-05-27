export interface Platform {
  id: number
  name: string
  identifier?: string
  isSystem: boolean
  isActive: boolean
  displayOrder: number
  createdAt: string
  updatedAt?: string
}
