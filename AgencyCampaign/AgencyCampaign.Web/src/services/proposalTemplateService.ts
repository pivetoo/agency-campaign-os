import { httpClient } from 'archon-ui'

const BASE_URL = '/ProposalTemplates'

export interface ProposalTemplateItem {
  id?: number
  proposalTemplateId?: number
  description: string
  defaultQuantity: number
  defaultUnitPrice: number
  defaultDeliveryDays?: number
  observations?: string
  displayOrder: number
}

export interface ProposalTemplate {
  id: number
  name: string
  description?: string
  isActive: boolean
  createdByUserName?: string
  createdAt: string
  updatedAt?: string
  items: ProposalTemplateItem[]
}

export interface CreateProposalTemplateRequest {
  name: string
  description?: string
  items: ProposalTemplateItem[]
}

export interface UpdateProposalTemplateRequest extends CreateProposalTemplateRequest {
  id: number
  isActive: boolean
}

export const proposalTemplateService = {
  async getAll(includeInactive = false): Promise<ProposalTemplate[]> {
    const url = includeInactive ? `${BASE_URL}/Get?includeInactive=true` : `${BASE_URL}/Get`
    const response = await httpClient.get<ProposalTemplate[]>(url)
    return response.data ?? []
  },

  async getById(id: number): Promise<ProposalTemplate | null> {
    const response = await httpClient.get<ProposalTemplate>(`${BASE_URL}/${id}`)
    return response.data ?? null
  },

  create(data: CreateProposalTemplateRequest) {
    return httpClient.post<ProposalTemplate>(`${BASE_URL}/Create`, data)
  },

  update(id: number, data: UpdateProposalTemplateRequest) {
    return httpClient.put<ProposalTemplate>(`${BASE_URL}/${id}`, data)
  },

  delete(id: number) {
    return httpClient.delete(`${BASE_URL}/${id}`)
  },

  applyToProposal(templateId: number, proposalId: number) {
    return httpClient.post<{ itemsCreated: number }>(`${BASE_URL}/${templateId}/ApplyToProposal/${proposalId}`, {})
  },
}
