import { httpClient, buildPaginationQuery } from 'archon-ui'
import type { ApiResponse } from 'archon-ui'
import type { CommercialPipelineStage } from '../types/commercialPipelineStage'
import type { CommercialForecast } from '../types/commercialForecast'
import type { CommercialAnalytics } from '../types/commercialAnalytics'
import type { CommercialOpportunityInsights } from '../types/commercialInsights'
import type { OpportunityApprovalComment } from '../types/opportunityApprovalComment'
import type { OpportunityApprovalReviewer } from '../types/opportunityApprovalReviewer'
import type { OpportunityApprovalDiff } from '../types/opportunityApprovalDiff'
import type { OpportunityApprovalImpact } from '../types/opportunityApprovalImpact'
import type { PolicyEvaluation } from '../types/policyEvaluation'

const BASE_URL = '/Opportunities'

export const OpportunityApprovalStatus = {
  Pending: 1,
  Approved: 2,
  Rejected: 3,
  Cancelled: 4,
  InReview: 5,
  ChangesRequested: 6,
  Merged: 7,
} as const
export type OpportunityApprovalStatusValue = (typeof OpportunityApprovalStatus)[keyof typeof OpportunityApprovalStatus]

export interface OpportunityProposalReference {
  id: number
  name: string
  status: number
  totalValue: number
  validityUntil?: string
  campaignId?: number
}

export interface OpportunityApprovalRequest {
  id: number
  proposalId: number
  approvalType: number
  status: OpportunityApprovalStatusValue
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
  opportunityId?: number
  opportunityName?: string
  proposalName?: string
  proposalTotalValue?: number
  brandId?: number
  brandName?: string
  brandLogoUrl?: string
}

export interface ApprovalSummary {
  pending: number
  approved: number
  rejected: number
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
  opportunityName?: string
  brandName?: string
  estimatedValue?: number
}

export interface FollowUpSummary {
  overdue: number
  today: number
  upcoming: number
  completed: number
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
  contactPhone?: string
  notes?: string
  closedAt?: string
  lossReason?: string
  lossReasonId?: number
  wonNotes?: string
  winReasonId?: number
  brand?: {
    id: number
    name: string
    logoUrl?: string
  }
  opportunitySourceId?: number
  opportunitySource?: OpportunitySourceReference
  tags: OpportunityTagReference[]
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
  brandLogoUrl?: string
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
  proposalsCount: number
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
  isDeleted: boolean
  createdAt: string
  updatedAt?: string
}

export interface CreateOpportunityCommentRequest {
  body: string
  mentionedUserIds?: number[]
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
  contactPhone?: string
  notes?: string
  opportunitySourceId?: number
  tagIds?: number[]
}

export interface UpdateOpportunityRequest extends CreateOpportunityRequest {
  id: number
}

export interface ChangeOpportunityStageRequest {
  commercialPipelineStageId: number
  reason?: string
}

export interface CloseOpportunityAsWonRequest {
  wonNotes?: string
  winReasonId?: number | null
}

export interface CloseOpportunityAsLostRequest {
  lossReason: string
  lossReasonId?: number | null
}

export interface CreateOpportunityApprovalRequest {
  proposalId: number
  approvalType: number
  reason: string
  requestedByUserId?: number
  requestedByUserName: string
  approvers?: { userId: number; userName: string }[]
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

function buildListQuery(params?: { page?: number; pageSize?: number } & OpportunityListFilters): string {
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
  return searchParams.toString()
}

export const opportunityService = {
  getAll(params?: { page?: number; pageSize?: number } & OpportunityListFilters): Promise<ApiResponse<Opportunity[]>> {
    const query = buildListQuery(params)
    const url = query ? `${BASE_URL}/Get?${query}` : `${BASE_URL}/Get`
    return httpClient.get<Opportunity[]>(url)
  },

  getAllMine(params?: { page?: number; pageSize?: number } & OpportunityListFilters): Promise<ApiResponse<Opportunity[]>> {
    const query = buildListQuery(params)
    const url = query ? `${BASE_URL}/GetMine?${query}` : `${BASE_URL}/GetMine`
    return httpClient.get<Opportunity[]>(url)
  },

  async getById(id: number): Promise<Opportunity | null> {
    const response = await httpClient.get<Opportunity>(`${BASE_URL}/${id}`)
    return response.data ?? null
  },

  async getBoard(): Promise<OpportunityBoardStage[]> {
    const response = await httpClient.get<OpportunityBoardStage[]>(`${BASE_URL}/Board`)
    return response.data ?? []
  },

  async getBoardMine(): Promise<OpportunityBoardStage[]> {
    const response = await httpClient.get<OpportunityBoardStage[]>(`${BASE_URL}/BoardMine`)
    return response.data ?? []
  },

  async getForecast(params?: { periodStart?: string; periodEnd?: string; userId?: number }): Promise<CommercialForecast | null> {
    const search = new URLSearchParams()
    if (params?.periodStart) search.set('periodStart', params.periodStart)
    if (params?.periodEnd) search.set('periodEnd', params.periodEnd)
    if (params?.userId) search.set('userId', String(params.userId))
    const query = search.toString()
    const response = await httpClient.get<CommercialForecast>(query ? `${BASE_URL}/Forecast?${query}` : `${BASE_URL}/Forecast`)
    return response.data ?? null
  },

  async getForecastMine(params?: { periodStart?: string; periodEnd?: string }): Promise<CommercialForecast | null> {
    const search = new URLSearchParams()
    if (params?.periodStart) search.set('periodStart', params.periodStart)
    if (params?.periodEnd) search.set('periodEnd', params.periodEnd)
    const query = search.toString()
    const response = await httpClient.get<CommercialForecast>(query ? `${BASE_URL}/ForecastMine?${query}` : `${BASE_URL}/ForecastMine`)
    return response.data ?? null
  },

  async getAnalytics(params?: { periodStart?: string; periodEnd?: string; userId?: number }): Promise<CommercialAnalytics | null> {
    const search = new URLSearchParams()
    if (params?.periodStart) search.set('periodStart', params.periodStart)
    if (params?.periodEnd) search.set('periodEnd', params.periodEnd)
    if (params?.userId) search.set('userId', String(params.userId))
    const query = search.toString()
    const response = await httpClient.get<CommercialAnalytics>(query ? `${BASE_URL}/Analytics?${query}` : `${BASE_URL}/Analytics`)
    return response.data ?? null
  },

  async getAnalyticsMine(params?: { periodStart?: string; periodEnd?: string }): Promise<CommercialAnalytics | null> {
    const search = new URLSearchParams()
    if (params?.periodStart) search.set('periodStart', params.periodStart)
    if (params?.periodEnd) search.set('periodEnd', params.periodEnd)
    const query = search.toString()
    const response = await httpClient.get<CommercialAnalytics>(query ? `${BASE_URL}/AnalyticsMine?${query}` : `${BASE_URL}/AnalyticsMine`)
    return response.data ?? null
  },

  async getInsights(scope: 'all' | 'mine', params?: { agingThresholdDays?: number; take?: number }): Promise<CommercialOpportunityInsights | null> {
    const endpoint = scope === 'mine' ? 'InsightsMine' : 'Insights'
    const search = new URLSearchParams()
    if (params?.agingThresholdDays !== undefined) search.set('agingThresholdDays', String(params.agingThresholdDays))
    if (params?.take !== undefined) search.set('take', String(params.take))
    const query = search.toString()
    const response = await httpClient.get<CommercialOpportunityInsights>(query ? `${BASE_URL}/${endpoint}?${query}` : `${BASE_URL}/${endpoint}`)
    return response.data ?? null
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

  async getApprovalRequests(proposalId: number): Promise<OpportunityApprovalRequest[]> {
    const response = await httpClient.get<OpportunityApprovalRequest[]>(`/OpportunityApprovals/proposal/${proposalId}`)
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

  recordReviewerDecision(id: number, data: { approved: boolean; notes?: string }) {
    return httpClient.post<OpportunityApprovalRequest>(`/OpportunityApprovals/${id}/Reviewers/Decision`, data)
  },

  markApprovalInReview(id: number) {
    return httpClient.post<OpportunityApprovalRequest>(`/OpportunityApprovals/${id}/MarkInReview`, {})
  },

  requestApprovalChanges(id: number, data: DecideOpportunityApprovalRequest) {
    return httpClient.post<OpportunityApprovalRequest>(`/OpportunityApprovals/${id}/RequestChanges`, data)
  },

  resubmitApproval(id: number, data: { requestedByUserName: string; requestedByUserId?: number; reason?: string }) {
    return httpClient.post<OpportunityApprovalRequest>(`/OpportunityApprovals/${id}/Resubmit`, data)
  },

  markApprovalMerged(id: number) {
    return httpClient.post<OpportunityApprovalRequest>(`/OpportunityApprovals/${id}/MarkMerged`, {})
  },

  async getApprovalComments(approvalId: number): Promise<OpportunityApprovalComment[]> {
    const response = await httpClient.get<OpportunityApprovalComment[]>(`/OpportunityApprovals/${approvalId}/Comments`)
    return response.data ?? []
  },

  createApprovalComment(approvalId: number, data: { userName: string; userId?: number; role?: string; body: string }) {
    return httpClient.post<OpportunityApprovalComment>(`/OpportunityApprovals/${approvalId}/Comments`, data)
  },

  updateApprovalComment(commentId: number, body: string) {
    return httpClient.put<OpportunityApprovalComment>(`/OpportunityApprovals/Comments/${commentId}`, { id: commentId, body })
  },

  deleteApprovalComment(commentId: number) {
    return httpClient.delete(`/OpportunityApprovals/Comments/${commentId}`)
  },

  async getApprovalReviewers(approvalId: number): Promise<OpportunityApprovalReviewer[]> {
    const response = await httpClient.get<OpportunityApprovalReviewer[]>(`/OpportunityApprovals/${approvalId}/Reviewers`)
    return response.data ?? []
  },

  addApprovalReviewer(approvalId: number, data: { userName: string; userId?: number; role?: string; required?: boolean }) {
    return httpClient.post<OpportunityApprovalReviewer>(`/OpportunityApprovals/${approvalId}/Reviewers`, data)
  },

  removeApprovalReviewer(reviewerId: number) {
    return httpClient.delete(`/OpportunityApprovals/Reviewers/${reviewerId}`)
  },

  async getApprovalDiffs(approvalId: number): Promise<OpportunityApprovalDiff[]> {
    const response = await httpClient.get<OpportunityApprovalDiff[]>(`/OpportunityApprovals/${approvalId}/Diffs`)
    return response.data ?? []
  },

  addApprovalDiff(approvalId: number, data: { field: string; policyValue?: string; requestedValue?: string; delta?: string; kind?: number; displayOrder?: number }) {
    return httpClient.post<OpportunityApprovalDiff>(`/OpportunityApprovals/${approvalId}/Diffs`, data)
  },

  removeApprovalDiff(diffId: number) {
    return httpClient.delete(`/OpportunityApprovals/Diffs/${diffId}`)
  },

  async getApprovalImpacts(approvalId: number): Promise<OpportunityApprovalImpact[]> {
    const response = await httpClient.get<OpportunityApprovalImpact[]>(`/OpportunityApprovals/${approvalId}/Impacts`)
    return response.data ?? []
  },

  addApprovalImpact(approvalId: number, data: { label: string; value: string; isGood: boolean; displayOrder?: number }) {
    return httpClient.post<OpportunityApprovalImpact>(`/OpportunityApprovals/${approvalId}/Impacts`, data)
  },

  removeApprovalImpact(impactId: number) {
    return httpClient.delete(`/OpportunityApprovals/Impacts/${impactId}`)
  },

  async evaluateProposalPolicy(proposalId: number): Promise<PolicyEvaluation | null> {
    const response = await httpClient.get<PolicyEvaluation>(`/OpportunityApprovals/evaluate-policy/${proposalId}`)
    return response.data ?? null
  },

  populateApprovalFromPolicy(approvalId: number) {
    return httpClient.post<{ ok: boolean }>(`/OpportunityApprovals/${approvalId}/PopulateFromPolicy`, {})
  },

  getAllApprovals(params?: { page?: number; pageSize?: number }): Promise<ApiResponse<OpportunityApprovalRequest[]>> {
    const query = buildPaginationQuery(params)
    return httpClient.get<OpportunityApprovalRequest[]>(`/OpportunityApprovals/Get${query}`)
  },

  async getApprovalsSummary(): Promise<ApprovalSummary | null> {
    const response = await httpClient.get<ApprovalSummary>('/OpportunityApprovals/Summary')
    return response.data ?? null
  },

  async getAllFollowUps(status?: string): Promise<OpportunityFollowUp[]> {
    const url = status ? `${BASE_URL}/followups/Get?status=${encodeURIComponent(status)}` : `${BASE_URL}/followups/Get`
    const response = await httpClient.get<OpportunityFollowUp[]>(url)
    return response.data ?? []
  },

  async getFollowUpsSummary(): Promise<FollowUpSummary | null> {
    const response = await httpClient.get<FollowUpSummary>(`${BASE_URL}/followups/Summary`)
    return response.data ?? null
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
