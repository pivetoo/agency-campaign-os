import { httpClient } from 'archon-ui'
import type { BrandContact, AddBrandContactInput, UpdateBrandContactInput } from '../types/brandContact'

const BASE = '/BrandContact'

export const brandContactService = {
  async getByBrand(brandId: number): Promise<BrandContact[]> {
    const response = await httpClient.get<BrandContact[]>(`${BASE}/brand/${brandId}`)
    return response.data ?? []
  },

  add(brandId: number, data: AddBrandContactInput) {
    return httpClient.post<BrandContact>(`${BASE}/brand/${brandId}`, data)
  },

  update(contactId: number, data: UpdateBrandContactInput) {
    return httpClient.put<BrandContact>(`${BASE}/Update/${contactId}`, data)
  },

  remove(contactId: number) {
    return httpClient.delete(`${BASE}/Delete/${contactId}`)
  },

  setPrimary(contactId: number) {
    return httpClient.post<BrandContact>(`${BASE}/primary/${contactId}`, {})
  },
}
