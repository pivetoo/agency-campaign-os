import { httpClient } from 'archon-ui'
import type { CreatorAccessToken } from '../types/creatorAccessToken'

const BASE_URL = '/CreatorAccessTokens'

export interface IssueCreatorAccessTokenRequest {
  creatorId: number
  expiresAt?: string
  note?: string
}

export const creatorAccessTokenService = {
  async getByCreator(creatorId: number): Promise<CreatorAccessToken[]> {
    const response = await httpClient.get<CreatorAccessToken[]>(`${BASE_URL}/creator/${creatorId}`)
    return response.data ?? []
  },

  issue(data: IssueCreatorAccessTokenRequest) {
    return httpClient.post<CreatorAccessToken>(`${BASE_URL}/Issue`, data)
  },

  revoke(id: number) {
    return httpClient.post<{ revoked: boolean }>(`${BASE_URL}/Revoke/${id}/revoke`, {})
  },
}
