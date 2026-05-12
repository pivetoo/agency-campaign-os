export interface IntegrationCategory {
  id: number
  name: string
  description?: string
  isActive: boolean
  createdAt: string
  updatedAt?: string
}

export interface IntegrationPlatformIntegration {
  id: number
  identifier: string
  name: string
  description?: string
  iconUrl?: string
  isActive: boolean
  supportsWebhook: boolean
  createdAt?: string
  updatedAt?: string
}

export interface IntegrationAttribute {
  id: number
  integrationId: number
  field: string
  label: string
  description?: string
  placeholder?: string
  type: number
  defaultValue?: string
  isRequired: boolean
  order: number
  group?: string
  isSensitive: boolean
}

export type FieldType =
  | 1 // Text
  | 2 // LongText
  | 3 // Number
  | 4 // Decimal
  | 5 // Boolean
  | 6 // Date
  | 7 // DateTime
  | 8 // List

export interface Connector {
  id: number
  integrationId: number
  name: string
  isActive: boolean
  systemApplicationId?: string
  webhookToken?: string
  createdAt?: string
}

export interface ConnectorAttributeValue {
  id: number
  connectorId: number
  integrationAttributeId: number
  value: string
}

export interface Pipeline {
  id: number
  integrationId: number
  identifier: string
  name: string
  description?: string
  isActive: boolean
  isTestPipeline: boolean
}

export interface Execution {
  id: number
  connectorId?: number
  pipelineId?: number
  status: number
  createdAt: string
}

export interface ProcessingQueue {
  id: number
  connectorId: number
  pipelineId: number
  priority: number
  status: number
}

export interface CreateConnectorPayload {
  integrationId: number
  name: string
  systemApplicationId?: string
  attributeValues?: ConnectorAttributeValuePayload[]
}

export interface ConnectorAttributeValuePayload {
  integrationAttributeId: number
  value: string
}

export interface ConnectorDetail {
  connector: Connector
  attributeValues: ConnectorAttributeValue[]
}

export interface UpdateConnectorPayload {
  integrationId: number
  name: string
  systemApplicationId?: string
  isActive: boolean
  attributeValues?: ConnectorAttributeValuePayload[]
}

export interface ExecutePipelinePayload {
  connectorId: number
  pipelineId: number
  inputData?: Record<string, unknown>
}

export interface EnqueuePipelinePayload {
  connectorId: number
  pipelineId: number
  payload?: string
  priority: number
}
