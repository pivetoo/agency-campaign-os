export interface CampaignFinancialEntry {
  id: number
  campaignId: number
  campaignDeliverableId?: number
  type: number
  description: string
  amount: number
  dueAt: string
  paidAt?: string
  status: number
  counterpartyName?: string
  notes?: string
  createdAt: string
  updatedAt?: string
}
