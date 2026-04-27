export interface CampaignCreatorStatus {
  id: number
  name: string
  description?: string
  displayOrder: number
  color: string
  isInitial: boolean
  isFinal: boolean
  category: number
  isActive: boolean
  createdAt: string
  updatedAt?: string
}
