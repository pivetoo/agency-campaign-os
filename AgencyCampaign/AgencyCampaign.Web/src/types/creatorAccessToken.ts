export interface CreatorAccessToken {
  id: number
  creatorId: number
  token: string
  expiresAt?: string
  revokedAt?: string
  lastUsedAt?: string
  usageCount: number
  note?: string
  createdByUserName?: string
  createdAt: string
}
