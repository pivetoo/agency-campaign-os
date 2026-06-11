import { useEffect, useState } from 'react'
import { useI18n, useToast } from 'archon-ui'
import { creatorPortalService } from '../../services/creatorPortalService'
import type { CampaignDeliverable } from '../../types/campaignDeliverable'
import { usePortalContext } from './hooks'

interface InsightForm {
  reach: string
  impressions: string
  saves: string
}

export default function CreatorPortalResults() {
  const { t } = useI18n()
  const { toast } = useToast()
  const { token } = usePortalContext()
  const [deliverables, setDeliverables] = useState<CampaignDeliverable[]>([])
  const [forms, setForms] = useState<Record<number, InsightForm>>({})
  const [loading, setLoading] = useState(true)
  const [savingId, setSavingId] = useState<number | null>(null)
  const [savedId, setSavedId] = useState<number | null>(null)

  useEffect(() => {
    let cancelled = false
    setLoading(true)
    creatorPortalService.getDeliverables(token).then((res) => {
      if (cancelled) return
      const published = res.filter((item) => item.status === 4)
      setDeliverables(published)
      const initial: Record<number, InsightForm> = {}
      published.forEach((item) => {
        initial[item.id] = {
          reach: item.reach?.toString() ?? '',
          impressions: item.impressions?.toString() ?? '',
          saves: item.saves?.toString() ?? '',
        }
      })
      setForms(initial)
      setLoading(false)
    })
    return () => {
      cancelled = true
    }
  }, [token])

  const update = (id: number, field: keyof InsightForm, value: string) => {
    setForms((prev) => ({ ...prev, [id]: { ...prev[id], [field]: value } }))
    setSavedId(null)
  }

  const toNum = (value: string) => (value.trim() === '' ? null : Number(value))

  const save = async (id: number) => {
    const form = forms[id]
    setSavingId(id)
    try {
      const response = await creatorPortalService.submitInsights(token, id, {
        reach: toNum(form.reach),
        impressions: toNum(form.impressions),
        saves: toNum(form.saves),
      })
      const updated = response.data
      if (updated) {
        setDeliverables((prev) => prev.map((item) => (item.id === id ? updated : item)))
        setSavedId(id)
      }
    } catch (error) {
      const message = (error as { message?: string } | null)?.message
      toast({ title: message && message.trim() ? message : 'Não foi possível salvar. Tente novamente.', variant: 'destructive' })
    } finally {
      setSavingId(null)
    }
  }

  if (loading) return <p className="text-sm text-muted-foreground">{t('creatorPortal.results.loading')}</p>
  if (deliverables.length === 0) return <p className="text-sm text-muted-foreground">{t('creatorPortal.results.empty')}</p>

  return (
    <div className="space-y-3">
      <div>
        <h2 className="text-base font-semibold">{t('creatorPortal.results.title')}</h2>
        <p className="text-xs text-muted-foreground">{t('creatorPortal.results.hint')}</p>
      </div>
      {deliverables.map((item) => {
        const form = forms[item.id]
        return (
          <div key={item.id} className="rounded-lg border bg-background p-3">
            <p className="font-medium">{item.title}</p>
            <p className="text-xs text-muted-foreground">
              {item.campaign?.name}
              {item.platform?.name ? ` · ${item.platform.name}` : ''}
            </p>
            <div className="mt-3 grid grid-cols-3 gap-2">
              <InsightInput label={t('creatorPortal.results.field.reach')} value={form?.reach ?? ''} onChange={(value) => update(item.id, 'reach', value)} />
              <InsightInput label={t('creatorPortal.results.field.impressions')} value={form?.impressions ?? ''} onChange={(value) => update(item.id, 'impressions', value)} />
              <InsightInput label={t('creatorPortal.results.field.saves')} value={form?.saves ?? ''} onChange={(value) => update(item.id, 'saves', value)} />
            </div>
            <div className="mt-3 flex items-center justify-end gap-2">
              {savedId === item.id && <span className="text-xs font-medium text-emerald-600">{t('creatorPortal.results.saved')}</span>}
              <button
                type="button"
                onClick={() => void save(item.id)}
                disabled={savingId === item.id}
                className="rounded-md bg-primary px-3 py-1.5 text-xs font-medium text-white disabled:opacity-50"
              >
                {savingId === item.id ? t('common.action.saving') : t('common.action.save')}
              </button>
            </div>
          </div>
        )
      })}
    </div>
  )
}

function InsightInput({ label, value, onChange }: { label: string; value: string; onChange: (value: string) => void }) {
  return (
    <div>
      <label className="text-[10px] uppercase tracking-wide text-muted-foreground">{label}</label>
      <input
        type="number"
        min={0}
        value={value}
        onChange={(event) => onChange(event.target.value)}
        className="mt-0.5 w-full rounded-md border bg-background px-2 py-1 text-sm"
      />
    </div>
  )
}
