import { useEffect, useState } from 'react'
import { Sheet, SheetContent, SheetHeader, SheetTitle, SheetDescription, Button, Input, useApi, useI18n } from 'archon-ui'
import { TrendingUp } from 'lucide-react'
import { campaignCreatorService } from '../../services/campaignCreatorService'
import type { CampaignCreator } from '../../types/campaignCreator'

interface Props {
  open: boolean
  onOpenChange: (open: boolean) => void
  campaignCreator: CampaignCreator | null
  onSuccess: () => void
}

export default function CampaignCreatorSalesSheet({ open, onOpenChange, campaignCreator, onSuccess }: Props) {
  const { t } = useI18n()
  const [couponCode, setCouponCode] = useState('')
  const [trackingUrl, setTrackingUrl] = useState('')
  const [attributedOrders, setAttributedOrders] = useState('')
  const [attributedRevenue, setAttributedRevenue] = useState('')
  const { execute: runSave, loading: saving } = useApi({ showErrorMessage: true, showSuccessMessage: true })

  useEffect(() => {
    if (open && campaignCreator) {
      setCouponCode(campaignCreator.couponCode ?? '')
      setTrackingUrl(campaignCreator.trackingUrl ?? '')
      setAttributedOrders(campaignCreator.attributedOrders != null ? String(campaignCreator.attributedOrders) : '')
      setAttributedRevenue(campaignCreator.attributedRevenue != null ? String(campaignCreator.attributedRevenue) : '')
    }
  }, [open, campaignCreator])

  async function handleSave() {
    if (!campaignCreator) return
    const result = await runSave(() => campaignCreatorService.setAttribution(campaignCreator.id, {
      couponCode: couponCode.trim() || null,
      trackingUrl: trackingUrl.trim() || null,
      attributedOrders: attributedOrders.trim() === '' ? null : Number(attributedOrders),
      attributedRevenue: attributedRevenue.trim() === '' ? null : Number(attributedRevenue),
    }))
    if (result !== null) {
      onSuccess()
    }
  }

  const creatorName = campaignCreator?.creator?.stageName || campaignCreator?.creator?.name || ''

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent side="right" className="w-full sm:max-w-md flex flex-col gap-0 p-0">
        <SheetHeader className="px-6 pt-6 pb-4 border-b">
          <div className="flex items-center gap-2">
            <TrendingUp size={18} className="text-muted-foreground" />
            <SheetTitle>{t('campaignCreatorSales.title')}</SheetTitle>
          </div>
          <SheetDescription className="sr-only">{t('campaignCreatorSales.title')}</SheetDescription>
        </SheetHeader>

        <div className="flex-1 overflow-y-auto px-6 py-5 space-y-4">
          {creatorName && <p className="text-sm font-medium text-foreground">{creatorName}</p>}

          <div className="space-y-1">
            <label className="text-xs text-muted-foreground">{t('campaignCreatorSales.field.coupon')}</label>
            <Input value={couponCode} onChange={(e) => setCouponCode(e.target.value)} />
          </div>

          <div className="space-y-1">
            <label className="text-xs text-muted-foreground">{t('campaignCreatorSales.field.trackingUrl')}</label>
            <Input value={trackingUrl} placeholder="https://...utm_source=" onChange={(e) => setTrackingUrl(e.target.value)} />
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1">
              <label className="text-xs text-muted-foreground">{t('campaignCreatorSales.field.orders')}</label>
              <Input type="number" min={0} value={attributedOrders} onChange={(e) => setAttributedOrders(e.target.value)} />
            </div>
            <div className="space-y-1">
              <label className="text-xs text-muted-foreground">{t('campaignCreatorSales.field.revenue')}</label>
              <Input type="number" min={0} step="0.01" value={attributedRevenue} onChange={(e) => setAttributedRevenue(e.target.value)} />
            </div>
          </div>

          <p className="text-xs text-muted-foreground">{t('campaignCreatorSales.hint')}</p>
        </div>

        <div className="border-t px-6 py-4 bg-background flex justify-end gap-2">
          <Button size="sm" variant="outline" onClick={() => onOpenChange(false)}>{t('common.action.cancel')}</Button>
          <Button size="sm" disabled={saving || !campaignCreator} onClick={() => void handleSave()}>{saving ? t('common.action.saving') : t('common.action.save')}</Button>
        </div>
      </SheetContent>
    </Sheet>
  )
}
