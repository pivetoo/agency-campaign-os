import { httpClient } from 'archon-ui'

export interface IntegrationCategory {
  id: number
  name: string
  description?: string
  isActive: boolean
  createdAt: string
  updatedAt?: string
}

export const integrationCategoryService = {
  async getActive(): Promise<IntegrationCategory[]> {
    const response = await httpClient.get<IntegrationCategory[]>('/IntegrationCategories/active')
    return response.data ?? []
  },
}
