export interface CommercialPipelineStage {
  id: number
  name: string
  description?: string
  displayOrder: number
  color: string
  isInitial: boolean
  isFinal: boolean
  finalBehavior: number
  defaultProbability?: number
  slaInDays?: number
  isActive: boolean
  createdAt: string
  updatedAt?: string
}
