import { httpClient } from 'archon-ui'

const BASE_URL = '/ProposalBlocks'

export interface ProposalBlock {
  id: number
  name: string
  body: string
  category: string
  isActive: boolean
  createdByUserName?: string
  createdAt: string
  updatedAt?: string
}

export interface CreateProposalBlockRequest {
  name: string
  body: string
  category: string
}

export interface UpdateProposalBlockRequest extends CreateProposalBlockRequest {
  id: number
  isActive: boolean
}

export const proposalBlockService = {
  async getAll(category?: string, includeInactive = false): Promise<ProposalBlock[]> {
    const params = new URLSearchParams()
    if (category) params.set('category', category)
    if (includeInactive) params.set('includeInactive', 'true')
    const query = params.toString()
    const url = query ? `${BASE_URL}/Get?${query}` : `${BASE_URL}/Get`
    const response = await httpClient.get<ProposalBlock[]>(url)
    return response.data ?? []
  },

  async getById(id: number): Promise<ProposalBlock | null> {
    const response = await httpClient.get<ProposalBlock>(`${BASE_URL}/${id}`)
    return response.data ?? null
  },

  create(data: CreateProposalBlockRequest) {
    return httpClient.post<ProposalBlock>(`${BASE_URL}/Create`, data)
  },

  update(id: number, data: UpdateProposalBlockRequest) {
    return httpClient.put<ProposalBlock>(`${BASE_URL}/${id}`, data)
  },

  delete(id: number) {
    return httpClient.delete(`${BASE_URL}/${id}`)
  },
}
