export type ContentLicenseType = 1 | 2 | 3 | 4
export type ContentLicenseStatus = 1 | 2 | 3

export interface ContentLicense {
  id: number
  deliverableId: number
  type: ContentLicenseType
  channels?: string
  startsAt?: string
  expiresAt?: string
  value?: number
  notes?: string
  campaignDocumentId?: number
  status: ContentLicenseStatus
  daysUntilExpiry?: number
  campaignId: number
  deliverableTitle?: string
}

export interface ContentLicenseInput {
  type: ContentLicenseType
  channels?: string | null
  startsAt?: string | null
  expiresAt?: string | null
  value?: number | null
  notes?: string | null
  campaignDocumentId?: number | null
}
