export interface AgencySettings {
  id: number
  agencyName: string
  tradeName?: string | null
  document?: string | null
  primaryEmail?: string | null
  phone?: string | null
  address?: string | null
  logoUrl?: string | null
  primaryColor?: string | null
  defaultEmailConnectorId?: number | null
  defaultEmailPipelineId?: number | null
}
