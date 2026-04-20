import { httpClient } from 'archon-ui'

const BASE_URL = '/Proposals'

export interface ProposalItem {
  id: number
  proposalId: number
  description: string
  quantity: number
  unitPrice: number
  deliveryDeadline?: string
  status: number
  observations?: string
  creatorId?: number
  creator?: {
    id: number
    name: string
    stageName?: string
  }
  total: number
}

export interface Proposal {
  id: number
  brandId: number
  name: string
  description?: string
  status: number
  validityUntil?: string
  totalValue: number
  internalOwnerId: number
  internalOwnerName?: string
  notes?: string
  campaignId?: number
  brand?: {
    id: number
    name: string
  }
  campaign?: {
    id: number
    name: string
  }
  items: ProposalItem[]
  createdAt: string
  updatedAt?: string
}

export interface CreateProposalRequest {
  brandId: number
  name: string
  description?: string
  validityUntil?: string
  notes?: string
  internalOwnerId?: number
  internalOwnerName?: string
}

export interface UpdateProposalRequest extends CreateProposalRequest {
  id: number
}

export interface CreateProposalItemRequest {
  proposalId: number
  description: string
  quantity: number
  unitPrice: number
  deliveryDeadline?: string
  creatorId?: number
  observations?: string
}

export interface UpdateProposalItemRequest {
  description: string
  quantity: number
  unitPrice: number
  deliveryDeadline?: string
  observations?: string
}

export const proposalService = {
  async getAll(params?: { page?: number; pageSize?: number }): Promise<{ items: Proposal[]; total: number }> {
    const searchParams = new URLSearchParams()
    if (params?.page) searchParams.set('page', params.page.toString())
    if (params?.pageSize) searchParams.set('pageSize', params.pageSize.toString())
    
    const query = searchParams.toString()
    const url = query ? `${BASE_URL}/Get?${query}` : `${BASE_URL}/Get`
    const response = await httpClient.get<{ items: Proposal[]; total: number }>(url)
    return response.data ?? { items: [], total: 0 }
  },

  async getById(id: number): Promise<Proposal | undefined> {
    const response = await httpClient.get<Proposal>(`${BASE_URL}/${id}`)
    return response.data
  },

  create(data: CreateProposalRequest) {
    return httpClient.post<Proposal>(`${BASE_URL}/Create`, data)
  },

  update(id: number, data: UpdateProposalRequest) {
    return httpClient.put<Proposal>(`${BASE_URL}/Update/${id}`, data)
  },

  send(id: number) {
    return httpClient.post<Proposal>(`${BASE_URL}/${id}/Send`, {})
  },

  approve(id: number) {
    return httpClient.post<Proposal>(`${BASE_URL}/${id}/Approve`, {})
  },

  reject(id: number) {
    return httpClient.post<Proposal>(`${BASE_URL}/${id}/Reject`, {})
  },

  convertToCampaign(id: number, campaignId: number) {
    return httpClient.post<Proposal>(`${BASE_URL}/${id}/ConvertToCampaign`, { campaignId })
  },

  cancel(id: number) {
    return httpClient.post<Proposal>(`${BASE_URL}/${id}/Cancel`, {})
  },

  // Proposal Items
  async getItems(proposalId: number): Promise<ProposalItem[]> {
    const response = await httpClient.get<ProposalItem[]>(`${BASE_URL}/${proposalId}/items/Get`)
    return response.data ?? []
  },

  createItem(proposalId: number, data: CreateProposalItemRequest) {
    return httpClient.post<ProposalItem>(`${BASE_URL}/${proposalId}/items/Create`, data)
  },

  updateItem(id: number, data: UpdateProposalItemRequest) {
    return httpClient.put<ProposalItem>(`${BASE_URL}/items/Update/${id}`, data)
  },

  deleteItem(id: number) {
    return httpClient.delete(`${BASE_URL}/items/${id}`)
  },
}
