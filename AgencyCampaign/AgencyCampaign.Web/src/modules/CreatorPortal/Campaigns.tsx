import { useEffect, useState } from 'react'
import { useI18n } from 'archon-ui'
import { creatorPortalService, type PortalCampaign } from '../../services/creatorPortalService'
import { usePortalContext } from './hooks'

export default function CreatorPortalCampaigns() {
  const { t } = useI18n()
  const { token } = usePortalContext()
  const [campaigns, setCampaigns] = useState<PortalCampaign[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    let cancelled = false
    setLoading(true)
    creatorPortalService.getCampaigns(token).then((res) => {
      if (!cancelled) {
        setCampaigns(res)
        setLoading(false)
      }
    })
    return () => {
      cancelled = true
    }
  }, [token])

  if (loading) return <p className="text-sm text-muted-foreground">Carregando...</p>
  if (campaigns.length === 0) return <p className="text-sm text-muted-foreground">Nenhuma campanha ainda.</p>

  return (
    <div className="space-y-3">
      <h2 className="text-base font-semibold">{t('creatorPortal.campaigns.title')}</h2>
      {campaigns.map((c) => (
        <div key={c.id} className="rounded-lg border bg-background p-3">
          <div className="flex items-start justify-between gap-2">
            <div>
              <p className="font-medium">{c.campaignName}</p>
              <p className="text-xs text-muted-foreground">{c.brandName}</p>
            </div>
            {c.statusName && (
              <span
                className="rounded-full px-2 py-0.5 text-[10px] font-medium"
                style={{
                  background: c.statusColor ? `${c.statusColor}20` : 'rgba(0,0,0,0.05)',
                  color: c.statusColor ?? 'inherit',
                }}
              >
                {c.statusName}
              </span>
            )}
          </div>
          <div className="mt-2 grid grid-cols-2 gap-2 text-xs">
            <Field label={t('creatorPortal.campaigns.field.fee')} value={`R$ ${c.agreedAmount.toFixed(2)}`} />
            <Field label={t('creatorPortal.campaigns.field.agencyFee')} value={`${c.agencyFeePercent.toFixed(2)}%`} />
            {c.startsAt && <Field label={t('common.field.startDate')} value={new Date(c.startsAt).toLocaleDateString('pt-BR')} />}
            {c.endsAt && <Field label={t('creatorPortal.campaigns.field.endDate')} value={new Date(c.endsAt).toLocaleDateString('pt-BR')} />}
          </div>
          {c.notes && <p className="mt-2 text-xs text-muted-foreground">{c.notes}</p>}
        </div>
      ))}
    </div>
  )
}

function Field({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <p className="text-[10px] uppercase tracking-wide text-muted-foreground">{label}</p>
      <p className="font-medium">{value}</p>
    </div>
  )
}
