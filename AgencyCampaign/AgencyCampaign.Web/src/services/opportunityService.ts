import { httpClient } from 'archon-ui'
import type { CommercialPipelineStage } from '../types/commercialPipelineStage'

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
  status: number
  negotiatedAt: string
  notes?: string
  approvalRequests: OpportunityApprovalRequest[]
  createdAt: string
  updatedAt?: string
}

export interface OpportunityApprovalRequest {
  id: number
  opportunityNegotiationId: number
  approvalType: number
  status: number
  reason: string
  requestedByUserId?: number
  requestedByUserName: string
  approvedByUserId?: number
  approvedByUserName?: string
  requestedAt: string
  decidedAt?: string
  decisionNotes?: string
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
  commercialPipelineStageId: number
  commercialPipelineStage?: Pick<CommercialPipelineStage, 'id' | 'name' | 'color' | 'displayOrder' | 'isFinal' | 'finalBehavior'>
  estimatedValue: number
  probability: number
  probabilityIsManual: boolean
  expectedCloseAt?: string
  responsibleUserId?: number
  commercialResponsible?: {
    id: number
    name: string
  }
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
  opportunitySourceId?: number
  opportunitySource?: OpportunitySourceReference
  tags: OpportunityTagReference[]
  negotiations: OpportunityNegotiation[]
  followUps: OpportunityFollowUp[]
  proposals: OpportunityProposalReference[]
  createdAt: string
  updatedAt?: string
}

export interface OpportunitySourceReference {
  id: number
  name: string
  color: string
}

export interface OpportunityTagReference {
  id: number
  name: string
  color: string
}

export interface OpportunityBoardItem {
  id: number
  brandId: number
  brandName: string
  name: string
  commercialPipelineStageId: number
  commercialPipelineStageName: string
  commercialPipelineStageColor: string
  estimatedValue: number
  expectedCloseAt?: string
  commercialResponsibleName?: string
  proposalCount: number
  pendingFollowUpsCount: number
  overdueFollowUpsCount: number
  updatedAt?: string
  stageEnteredAt?: string
  stageSlaInDays?: number
  daysInStage?: number
  slaStatus: 'ok' | 'warning' | 'breached'
}

export interface OpportunityBoardStage {
  commercialPipelineStageId: number
  name: string
  color: string
  description?: string
  displayOrder: number
  isFinal: boolean
  finalBehavior: number
  slaInDays?: number
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

export interface OpportunityComment {
  id: number
  opportunityId: number
  authorUserId?: number
  authorName: string
  body: string
  createdAt: string
  updatedAt?: string
}

export interface CreateOpportunityCommentRequest {
  body: string
}

export interface UpdateOpportunityCommentRequest {
  body: string
}

export interface OpportunityStageHistoryItem {
  id: number
  opportunityId: number
  fromStageId?: number
  fromStageName?: string
  fromStageColor?: string
  toStageId: number
  toStageName: string
  toStageColor?: string
  changedAt: string
  changedByUserId?: number
  changedByUserName?: string
  reason?: string
}

export interface CreateOpportunityRequest {
  brandId: number
  commercialPipelineStageId?: number
  name: string
  description?: string
  estimatedValue: number
  expectedCloseAt?: string
  responsibleUserId?: number
  contactName?: string
  contactEmail?: string
  notes?: string
  opportunitySourceId?: number
  tagIds?: number[]
}

export interface UpdateOpportunityRequest extends CreateOpportunityRequest {
  id: number
  probability?: number
}

export interface ChangeOpportunityStageRequest {
  commercialPipelineStageId: number
  reason?: string
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
  status?: number
  negotiatedAt: string
  notes?: string
}

export interface ChangeOpportunityNegotiationStatusRequest {
  status: number
}

export interface CreateOpportunityApprovalRequest {
  opportunityNegotiationId: number
  approvalType: number
  reason: string
  requestedByUserId?: number
  requestedByUserName: string
}

export interface DecideOpportunityApprovalRequest {
  approvedByUserId?: number
  approvedByUserName: string
  decisionNotes?: string
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

export interface OpportunityListFilters {
  search?: string
  brandId?: number
  commercialPipelineStageId?: number
  responsibleUserId?: number
  status?: 'open' | 'won' | 'lost'
  minValue?: number
  maxValue?: number
  opportunitySourceId?: number
  opportunityTagId?: number
}

export const opportunityService = {
  async getAll(params?: { page?: number; pageSize?: number } & OpportunityListFilters): Promise<Opportunity[]> {
    const searchParams = new URLSearchParams()
    if (params?.page) searchParams.set('page', params.page.toString())
    if (params?.pageSize) searchParams.set('pageSize', params.pageSize.toString())
    if (params?.search) searchParams.set('search', params.search)
    if (params?.brandId) searchParams.set('brandId', params.brandId.toString())
    if (params?.commercialPipelineStageId) searchParams.set('commercialPipelineStageId', params.commercialPipelineStageId.toString())
    if (params?.responsibleUserId) searchParams.set('responsibleUserId', params.responsibleUserId.toString())
    if (params?.status) searchParams.set('status', params.status)
    if (params?.minValue !== undefined) searchParams.set('minValue', params.minValue.toString())
    if (params?.maxValue !== undefined) searchParams.set('maxValue', params.maxValue.toString())
    if (params?.opportunitySourceId) searchParams.set('opportunitySourceId', params.opportunitySourceId.toString())
    if (params?.opportunityTagId) searchParams.set('opportunityTagId', params.opportunityTagId.toString())

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

  async getStageHistory(opportunityId: number): Promise<OpportunityStageHistoryItem[]> {
    const response = await httpClient.get<OpportunityStageHistoryItem[]>(`${BASE_URL}/${opportunityId}/StageHistory`)
    return response.data ?? []
  },

  async getComments(opportunityId: number): Promise<OpportunityComment[]> {
    const response = await httpClient.get<OpportunityComment[]>(`${BASE_URL}/${opportunityId}/comments/Get`)
    return response.data ?? []
  },

  createComment(opportunityId: number, data: CreateOpportunityCommentRequest) {
    return httpClient.post<OpportunityComment>(`${BASE_URL}/${opportunityId}/comments/Create`, data)
  },

  updateComment(id: number, data: UpdateOpportunityCommentRequest) {
    return httpClient.put<OpportunityComment>(`${BASE_URL}/comments/${id}`, data)
  },

  deleteComment(id: number) {
    return httpClient.delete(`${BASE_URL}/comments/${id}`)
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

  changeNegotiationStatus(id: number, data: ChangeOpportunityNegotiationStatusRequest) {
    return httpClient.post<OpportunityNegotiation>(`${BASE_URL}/negotiations/${id}/ChangeStatus`, data)
  },

  deleteNegotiation(id: number) {
    return httpClient.delete(`${BASE_URL}/negotiations/${id}`)
  },

  async getApprovalRequests(negotiationId: number): Promise<OpportunityApprovalRequest[]> {
    const response = await httpClient.get<OpportunityApprovalRequest[]>(`/OpportunityApprovals/negotiation/${negotiationId}`)
    return response.data ?? []
  },

  createApprovalRequest(data: CreateOpportunityApprovalRequest) {
    return httpClient.post<OpportunityApprovalRequest>('/OpportunityApprovals/Create', data)
  },

  approveRequest(id: number, data: DecideOpportunityApprovalRequest) {
    return httpClient.post<OpportunityApprovalRequest>(`/OpportunityApprovals/${id}/Approve`, data)
  },

  rejectRequest(id: number, data: DecideOpportunityApprovalRequest) {
    return httpClient.post<OpportunityApprovalRequest>(`/OpportunityApprovals/${id}/Reject`, data)
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
