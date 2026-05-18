export interface Bank {
  id: number
  compe: string
  ispb?: string | null
  name: string
  shortName: string
  logoUrl?: string | null
  isActive: boolean
  isSystem: boolean
  createdByUserName?: string | null
}

export interface CreateBankRequest {
  compe: string
  ispb?: string
  name: string
  shortName: string
}

export interface UpdateBankRequest extends CreateBankRequest {
  id: number
  isActive: boolean
}
