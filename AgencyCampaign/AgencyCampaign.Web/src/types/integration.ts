export interface Integration {
  id: number
  identifier: string
  name: string
  description?: string
  categoryId: number
  isActive: boolean
  createdAt: string
  updatedAt?: string
}

export interface IntegrationPipeline {
  id: number
  identifier: string
  name: string
  description?: string
  integrationId: number
  integrationName: string
  isActive: boolean
  createdAt: string
  updatedAt?: string
}

export interface IntegrationLog {
  id: number
  integrationPipelineId: number
  integrationPipelineName: string
  integrationName: string
  status: number
  payload?: string
  response?: string
  durationMs?: number
  errorMessage?: string
  createdAt: string
  updatedAt?: string
}
