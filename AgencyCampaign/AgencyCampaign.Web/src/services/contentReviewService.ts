import { httpClient } from 'archon-ui'
import type { ContentReview, ContentAssetInput } from '../types/contentReview'

const BASE = '/ContentReview'

export const contentReviewService = {
  async get(deliverableId: number): Promise<ContentReview | null> {
    const res = await httpClient.get<ContentReview>(`${BASE}/deliverable/${deliverableId}`)
    return res.data ?? null
  },

  uploadFile(deliverableId: number, file: File) {
    const form = new FormData()
    form.append('file', file)
    return httpClient.post<{ storageKey: string; previewUrl: string; fileName: string; contentType: string }>(`${BASE}/upload/${deliverableId}`, form)
  },

  addVersion(deliverableId: number, assets: ContentAssetInput[], note?: string) {
    return httpClient.post<ContentReview>(`${BASE}/deliverable/${deliverableId}/version`, { assets, note })
  },

  requestChanges(versionId: number, body: string) {
    return httpClient.post<ContentReview>(`${BASE}/version/${versionId}/request-changes`, { body, visibility: 2 })
  },

  sendToBrand(versionId: number) {
    return httpClient.post<ContentReview>(`${BASE}/version/${versionId}/send-to-brand`, {})
  },

  agencyApprove(versionId: number) {
    return httpClient.post<ContentReview>(`${BASE}/version/${versionId}/agency-approve`, {})
  },

  addComment(deliverableId: number, body: string, visibility: 1 | 2, versionId?: number) {
    return httpClient.post<ContentReview>(`${BASE}/deliverable/${deliverableId}/comment`, { versionId, body, visibility })
  },
}
