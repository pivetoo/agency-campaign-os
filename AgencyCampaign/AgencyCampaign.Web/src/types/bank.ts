export interface Bank {
  id: number
  compe: string
  ispb?: string | null
  name: string
  shortName: string
  logoUrl?: string | null
  isActive: boolean
  isSystem: boolean
}

export interface CreateBankRequest {
  compe: string
  ispb?: string
  name: string
  shortName: string
  logoUrl?: string
}

export interface UpdateBankRequest extends CreateBankRequest {
  id: number
  isActive: boolean
}
