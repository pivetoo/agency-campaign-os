import { httpClient } from 'archon-ui'
import type { Brand } from '../types/brand'

const BASE_URL = '/Brands'

export const brandService = {
  async getAll(): Promise<Brand[]> {
    const response = await httpClient.get<Brand[]>(`${BASE_URL}/Get`)
    return response.data ?? []
  },
}
