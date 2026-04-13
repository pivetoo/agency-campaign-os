import { httpClient } from 'archon-ui'
import type { Creator } from '../types/creator'

const BASE_URL = '/Creators'

export const creatorService = {
  async getAll(): Promise<Creator[]> {
    const response = await httpClient.get<Creator[]>(`${BASE_URL}/Get`)
    return response.data ?? []
  },
}
