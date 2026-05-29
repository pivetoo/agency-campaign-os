export type BrandContactType = 1 | 2

export const BrandContactType = { Email: 1, Phone: 2 } as const

export interface BrandContact {
  id: number
  brandId: number
  type: BrandContactType
  value: string
  label?: string | null
  isPrimary: boolean
}

export interface AddBrandContactInput {
  type: BrandContactType
  value: string
  label?: string | null
}

export interface UpdateBrandContactInput {
  value: string
  label?: string | null
}
