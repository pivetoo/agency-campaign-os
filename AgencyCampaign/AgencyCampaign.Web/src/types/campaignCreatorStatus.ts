export interface CampaignCreatorStatus {
  id: number
  name: string
  description?: string
  displayOrder: number
  color: string
  isInitial: boolean
  isFinal: boolean
  category: number
  marksAsConfirmed: boolean
  isActive: boolean
  createdAt: string
  updatedAt?: string
}
