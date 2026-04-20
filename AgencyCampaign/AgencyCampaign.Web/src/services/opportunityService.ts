import { httpClient } from 'archon-ui'

const BASE_URL = '/Opportunities'

export interface OpportunityProposalReference {
  id: number
  name: string
  status: number
  totalValue: number
  validityUntil?: string
  campaignId?: number
}

export interface OpportunityNegotiation {
  id: number
  opportunityId: number
  title: string
  amount: number
  negotiatedAt: string
  notes?: string
  createdAt: string
  updatedAt?: string
}

export interface OpportunityFollowUp {
  id: number
  opportunityId: number
  subject: string
  dueAt: string
  notes?: string
  isCompleted: boolean
  completedAt?: string
  createdAt: string
  updatedAt?: string
}

export interface Opportunity {
  id: number
  brandId: number
  name: string
  description?: string
  stage: number
  estimatedValue: number
  expectedCloseAt?: string
  internalOwnerId?: number
  internalOwnerName?: string
  contactName?: string
  contactEmail?: string
  notes?: string
  closedAt?: string
  lossReason?: string
  wonNotes?: string
  brand?: {
    id: number
    name: string
  }
  negotiations: OpportunityNegotiation[]
  followUps: OpportunityFollowUp[]
  proposals: OpportunityProposalReference[]
  createdAt: string
  updatedAt?: string
}

export interface OpportunityBoardItem {
  id: number
  brandId: number
  brandName: string
  name: string
  stage: number
  estimatedValue: number
  expectedCloseAt?: string
  internalOwnerName?: string
  proposalCount: number
  pendingFollowUpsCount: number
  overdueFollowUpsCount: number
  updatedAt?: string
}

export interface OpportunityBoardStage {
  stage: number
  opportunitiesCount: number
  estimatedValueTotal: number
  items: OpportunityBoardItem[]
}

export interface CommercialDashboardSummary {
  totalOpportunities: number
  openOpportunities: number
  wonOpportunities: number
  lostOpportunities: number
  negotiationsCount: number
  pendingFollowUpsCount: number
  overdueFollowUpsCount: number
  totalPipelineValue: number
  wonValue: number
}

export interface CommercialAlert {
  type: string
  severity: string
  title: string
  description: string
  opportunityId: number
  opportunityName: string
  followUpId?: number
  dueAt?: string
}

export interface CreateOpportunityRequest {
  brandId: number
  name: string
  description?: string
  estimatedValue: number
  expectedCloseAt?: string
  internalOwnerId?: number
  internalOwnerName?: string
  contactName?: string
  contactEmail?: string
  notes?: string
}

export interface UpdateOpportunityRequest extends CreateOpportunityRequest {
  id: number
}

export interface ChangeOpportunityStageRequest {
  stage: number
}

export interface CloseOpportunityAsWonRequest {
  wonNotes?: string
}

export interface CloseOpportunityAsLostRequest {
  lossReason: string
}

export interface CreateOpportunityNegotiationRequest {
  opportunityId: number
  title: string
  amount: number
  negotiatedAt: string
  notes?: string
}

export interface UpdateOpportunityNegotiationRequest {
  title: string
  amount: number
  negotiatedAt: string
  notes?: string
}

export interface CreateOpportunityFollowUpRequest {
  opportunityId: number
  subject: string
  dueAt: string
  notes?: string
}

export interface UpdateOpportunityFollowUpRequest {
  subject: string
  dueAt: string
  notes?: string
}

function normalizePagedItems<T>(payload: unknown): T[] {
  if (Array.isArray(payload)) {
    return payload
  }

  if (payload && typeof payload === 'object') {
    const candidate = payload as { items?: unknown; Items?: unknown }

    if (Array.isArray(candidate.items)) {
      return candidate.items as T[]
    }

    if (Array.isArray(candidate.Items)) {
      return candidate.Items as T[]
    }
  }

  return []
}

export const opportunityService = {
  async getAll(params?: { page?: number; pageSize?: number }): Promise<Opportunity[]> {
    const searchParams = new URLSearchParams()
    if (params?.page) searchParams.set('page', params.page.toString())
    if (params?.pageSize) searchParams.set('pageSize', params.pageSize.toString())

    const query = searchParams.toString()
    const url = query ? `${BASE_URL}/Get?${query}` : `${BASE_URL}/Get`
    const response = await httpClient.get<unknown>(url)
    return normalizePagedItems<Opportunity>(response.data)
  },

  async getById(id: number): Promise<Opportunity | null> {
    const response = await httpClient.get<Opportunity>(`${BASE_URL}/${id}`)
    return response.data ?? null
  },

  async getBoard(): Promise<OpportunityBoardStage[]> {
    const response = await httpClient.get<OpportunityBoardStage[]>(`${BASE_URL}/Board`)
    return response.data ?? []
  },

  async getDashboard(): Promise<CommercialDashboardSummary | null> {
    const response = await httpClient.get<CommercialDashboardSummary>(`${BASE_URL}/Dashboard`)
    return response.data ?? null
  },

  async getAlerts(): Promise<CommercialAlert[]> {
    const response = await httpClient.get<CommercialAlert[]>(`${BASE_URL}/Alerts`)
    return response.data ?? []
  },

  create(data: CreateOpportunityRequest) {
    return httpClient.post<Opportunity>(`${BASE_URL}/Create`, data)
  },

  update(id: number, data: UpdateOpportunityRequest) {
    return httpClient.put<Opportunity>(`${BASE_URL}/${id}`, data)
  },

  changeStage(id: number, data: ChangeOpportunityStageRequest) {
    return httpClient.post<Opportunity>(`${BASE_URL}/${id}/ChangeStage`, data)
  },

  closeAsWon(id: number, data: CloseOpportunityAsWonRequest) {
    return httpClient.post<Opportunity>(`${BASE_URL}/${id}/CloseAsWon`, data)
  },

  closeAsLost(id: number, data: CloseOpportunityAsLostRequest) {
    return httpClient.post<Opportunity>(`${BASE_URL}/${id}/CloseAsLost`, data)
  },

  delete(id: number) {
    return httpClient.delete(`${BASE_URL}/${id}`)
  },

  async getNegotiations(opportunityId: number): Promise<OpportunityNegotiation[]> {
    const response = await httpClient.get<OpportunityNegotiation[]>(`${BASE_URL}/${opportunityId}/negotiations/GetNegotiations`)
    return response.data ?? []
  },

  createNegotiation(opportunityId: number, data: CreateOpportunityNegotiationRequest) {
    return httpClient.post<OpportunityNegotiation>(`${BASE_URL}/${opportunityId}/negotiations/CreateNegotiation`, data)
  },

  updateNegotiation(id: number, data: UpdateOpportunityNegotiationRequest) {
    return httpClient.put<OpportunityNegotiation>(`${BASE_URL}/negotiations/${id}`, data)
  },

  deleteNegotiation(id: number) {
    return httpClient.delete(`${BASE_URL}/negotiations/${id}`)
  },

  async getFollowUps(opportunityId: number): Promise<OpportunityFollowUp[]> {
    const response = await httpClient.get<OpportunityFollowUp[]>(`${BASE_URL}/${opportunityId}/followups/GetFollowUps`)
    return response.data ?? []
  },

  createFollowUp(opportunityId: number, data: CreateOpportunityFollowUpRequest) {
    return httpClient.post<OpportunityFollowUp>(`${BASE_URL}/${opportunityId}/followups/CreateFollowUp`, data)
  },

  updateFollowUp(id: number, data: UpdateOpportunityFollowUpRequest) {
    return httpClient.put<OpportunityFollowUp>(`${BASE_URL}/followups/${id}`, data)
  },

  completeFollowUp(id: number) {
    return httpClient.post<OpportunityFollowUp>(`${BASE_URL}/followups/${id}/Complete`, {})
  },

  deleteFollowUp(id: number) {
    return httpClient.delete(`${BASE_URL}/followups/${id}`)
  },
}
