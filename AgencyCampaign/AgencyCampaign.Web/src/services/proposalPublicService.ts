import { httpClient } from 'archon-ui'

const BASE_URL = '/proposal-public'

export interface ProposalPublicItem {
  id: number
  creatorId?: number
  creatorName?: string
  description: string
  quantity: number
  unitPrice: number
  total: number
  deliveryDeadline?: string
  observations?: string
  status: number
}

export interface ProposalPublicSnapshot {
  proposalId: number
  name: string
  description?: string
  totalValue: number
  validityUntil?: string
  notes?: string
  items: ProposalPublicItem[]
}

export interface ProposalPublicView {
  proposalId: number
  versionNumber: number
  name: string
  description?: string
  agencyName: string
  brandName: string
  totalValue: number
  validityUntil?: string
  sentAt: string
  snapshotJson: string
}

export const proposalPublicService = {
  async getByToken(token: string): Promise<ProposalPublicView | null> {
    try {
      const response = await httpClient.get<ProposalPublicView>(`${BASE_URL}/${encodeURIComponent(token)}`)
      return response.data ?? null
    } catch {
      return null
    }
  },

  parseSnapshot(snapshotJson: string): ProposalPublicSnapshot | null {
    try {
      return JSON.parse(snapshotJson) as ProposalPublicSnapshot
    } catch {
      return null
    }
  },
}
