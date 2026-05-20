export interface IntegrationCapability {
  id: number
  intentKey: string
  connectorId: number
  isActive: boolean
  createdAt: string
  updatedAt?: string | null
}

export interface IntegrationIntentDescriptor {
  key: string
  label: string
  categoryIdentifier: string
  serviceContractIdentifier: string
}

export interface SetIntegrationCapabilityPayload {
  intentKey: string
  connectorId: number
  isActive: boolean
}

export interface CapabilityConnectorOption {
  id: number
  name: string
  isActive: boolean
}

export interface IntegrationCapabilitySummary {
  intentKey: string
  label: string
  categoryIdentifier: string
  serviceContractIdentifier: string
  configuredConnectorId?: number | null
  isActive: boolean
  availableConnectors: CapabilityConnectorOption[]
}
