export interface CommercialPolicy {
  id: number
  maxDiscountPercent?: number
  minMarginPercent?: number
  defaultPaymentTermDays?: number
  maxPaymentTermDays?: number
  notes?: string
  createdAt: string
  updatedAt?: string
}
