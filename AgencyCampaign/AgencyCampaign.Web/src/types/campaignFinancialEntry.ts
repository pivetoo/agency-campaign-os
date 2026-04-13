export interface CampaignFinancialEntry {
  id: number
  campaignId: number
  campaignDeliverableId?: number
  type: number
  category: number
  description: string
  amount: number
  dueAt: string
  occurredAt: string
  paymentMethod?: string
  referenceCode?: string
  paidAt?: string
  status: number
  counterpartyName?: string
  notes?: string
  createdAt: string
  updatedAt?: string
}
