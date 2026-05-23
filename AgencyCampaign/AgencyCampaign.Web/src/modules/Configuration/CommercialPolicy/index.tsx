import { useEffect, useState } from 'react'
import { Button, Input, PageLayout, useApi } from 'archon-ui'
import { ShieldCheck } from 'lucide-react'
import { commercialPolicyService, type UpsertCommercialPolicyRequest } from '../../../services/commercialPolicyService'

const initialFormData: UpsertCommercialPolicyRequest = {
  maxDiscountPercent: null,
  minMarginPercent: null,
  defaultPaymentTermDays: null,
  maxPaymentTermDays: null,
  notes: '',
}

export default function CommercialPolicyAdmin() {
  const [formData, setFormData] = useState<UpsertCommercialPolicyRequest>(initialFormData)
  const [loaded, setLoaded] = useState(false)
  const { execute: load, loading: loading } = useApi({ showErrorMessage: true })
  const { execute: save, loading: saving } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  const fetchData = async () => {
    const result = await load(() => commercialPolicyService.get())
    if (result) {
      setFormData({
        maxDiscountPercent: result.maxDiscountPercent ?? null,
        minMarginPercent: result.minMarginPercent ?? null,
        defaultPaymentTermDays: result.defaultPaymentTermDays ?? null,
        maxPaymentTermDays: result.maxPaymentTermDays ?? null,
        notes: result.notes ?? '',
      })
    }
    setLoaded(true)
  }

  useEffect(() => {
    void fetchData()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const submit = async (e: React.FormEvent) => {
    e.preventDefault()
    await save(() => commercialPolicyService.upsert({ ...formData, notes: formData.notes?.trim() || undefined }))
  }

  const handleNumberChange = (key: keyof UpsertCommercialPolicyRequest, value: string) => {
    setFormData((prev) => ({ ...prev, [key]: value === '' ? null : Number(value) }))
  }

  return (
    <PageLayout
      title="Política comercial"
      subtitle="Limites usados para detectar quando uma negociação requer aprovação."
      showDefaultActions={false}
      onRefresh={() => void fetchData()}
    >
      <form onSubmit={submit} className="max-w-3xl space-y-6">
        <div className="flex items-start gap-3 rounded-lg border border-border bg-muted/30 px-4 py-3 text-sm">
          <ShieldCheck className="mt-0.5 h-4 w-4 shrink-0 text-primary" />
          <div className="text-muted-foreground">
            Quando uma negociação for criada ou editada com valores **fora destes limites**, o Kanvas avisa o operador e pré-popula a aprovação com o diff e o impacto financeiro estimado.
          </div>
        </div>

        <section className="grid gap-4 rounded-lg border border-border bg-card p-5 sm:grid-cols-2">
          <Field label="Desconto máximo permitido (%)" hint="Desconto acima disso exige aprovação.">
            <Input type="number" min={0} max={100} step="0.1" placeholder="Ex.: 10" value={formData.maxDiscountPercent ?? ''} onChange={(e) => handleNumberChange('maxDiscountPercent', e.target.value)} disabled={loading} />
          </Field>
          <Field label="Margem mínima exigida (%)" hint="Margem abaixo disso exige aprovação.">
            <Input type="number" min={0} max={100} step="0.1" placeholder="Ex.: 22" value={formData.minMarginPercent ?? ''} onChange={(e) => handleNumberChange('minMarginPercent', e.target.value)} disabled={loading} />
          </Field>
          <Field label="Prazo padrão de pagamento (dias)" hint="Usado como referência (sem aprovação).">
            <Input type="number" min={0} max={3650} step="1" placeholder="Ex.: 30" value={formData.defaultPaymentTermDays ?? ''} onChange={(e) => handleNumberChange('defaultPaymentTermDays', e.target.value)} disabled={loading} />
          </Field>
          <Field label="Prazo máximo aceito (dias)" hint="Prazo acima disso exige aprovação.">
            <Input type="number" min={0} max={3650} step="1" placeholder="Ex.: 60" value={formData.maxPaymentTermDays ?? ''} onChange={(e) => handleNumberChange('maxPaymentTermDays', e.target.value)} disabled={loading} />
          </Field>
        </section>

        <section className="space-y-2 rounded-lg border border-border bg-card p-5">
          <label className="text-sm font-medium">Notas internas (opcional)</label>
          <textarea
            className="min-h-[80px] w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
            value={formData.notes ?? ''}
            onChange={(e) => setFormData((prev) => ({ ...prev, notes: e.target.value }))}
            placeholder="Contexto da política (ex.: válida a partir de Q2/2026, aprovada pela diretoria)"
          />
        </section>

        <div className="flex justify-end gap-2">
          <Button type="submit" variant="primary" disabled={saving || !loaded}>{saving ? 'Salvando…' : 'Salvar política'}</Button>
        </div>
      </form>
    </PageLayout>
  )
}

function Field({ label, hint, children }: { label: string; hint?: string; children: React.ReactNode }) {
  return (
    <div className="space-y-1.5">
      <label className="text-sm font-medium">{label}</label>
      {children}
      {hint && <p className="text-[11px] text-muted-foreground">{hint}</p>}
    </div>
  )
}
