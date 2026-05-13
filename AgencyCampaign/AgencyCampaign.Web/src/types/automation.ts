export interface Automation {
  id: number
  name: string
  trigger: string
  triggerCondition?: string
  connectorId: number
  pipelineId: number
  variableMappingJson: string
  isActive: boolean
  createdAt: string
  updatedAt?: string
}

export interface CreateAutomationPayload {
  name: string
  trigger: string
  triggerCondition?: string
  connectorId: number
  pipelineId: number
  variableMapping: Record<string, string>
  isActive: boolean
}

export interface UpdateAutomationPayload {
  id: number
  name: string
  trigger: string
  triggerCondition?: string
  connectorId: number
  pipelineId: number
  variableMapping: Record<string, string>
  isActive: boolean
}

export interface AutomationExecutionLog {
  id: number
  automationId: number
  automationName: string
  trigger: string
  succeeded: boolean
  renderedPayload?: string
  errorMessage?: string
  createdAt: string
}
