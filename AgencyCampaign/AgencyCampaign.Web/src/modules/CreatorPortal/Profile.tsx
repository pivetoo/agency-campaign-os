import { useEffect, useState } from 'react'
import { Button, Input, SearchableSelect, useApi } from 'archon-ui'
import { creatorPortalService } from '../../services/creatorPortalService'
import { PixKeyType, pixKeyTypeLabels, type PixKeyTypeValue } from '../../types/creatorPayment'
import { usePortalContext } from './hooks'

const pixKeyTypeOptions = Object.values(PixKeyType).map((value) => ({
  value: String(value),
  label: pixKeyTypeLabels[value as PixKeyTypeValue],
}))

export default function CreatorPortalProfile() {
  const { token, session, refresh } = usePortalContext()
  const [pixKey, setPixKey] = useState('')
  const [pixKeyType, setPixKeyType] = useState<PixKeyTypeValue | undefined>()
  const [document, setDocumentValue] = useState('')
  const { execute, loading } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  useEffect(() => {
    setPixKey(session.creator.pixKey ?? '')
    setPixKeyType(session.creator.pixKeyType)
    setDocumentValue(session.creator.document ?? '')
  }, [session.creator.id])

  const isValid = pixKey.trim().length > 0 && !!pixKeyType

  const submit = async (event: React.FormEvent) => {
    event.preventDefault()
    if (!isValid || !pixKeyType) return
    const result = await execute(() =>
      creatorPortalService.updateBankInfo(token, {
        pixKey: pixKey.trim(),
        pixKeyType,
        document: document.trim() || undefined,
      }),
    )
    if (result !== null) {
      refresh()
    }
  }

  return (
    <div className="space-y-4">
      <h2 className="text-base font-semibold">Dados de pagamento</h2>
      <p className="text-xs text-muted-foreground">
        Esses dados são usados pela agência para fazer seus repasses via PIX. Mantenha sempre atualizado.
      </p>

      <form onSubmit={submit} className="space-y-3">
        <div className="space-y-1">
          <label className="text-sm font-medium">Tipo da chave PIX</label>
          <SearchableSelect
            value={pixKeyType ? String(pixKeyType) : ''}
            onValueChange={(value) => setPixKeyType(value ? (Number(value) as PixKeyTypeValue) : undefined)}
            options={pixKeyTypeOptions}
            placeholder="Selecione"
            searchPlaceholder="Buscar"
          />
        </div>
        <div className="space-y-1">
          <label className="text-sm font-medium">Chave PIX</label>
          <Input value={pixKey} onChange={(e) => setPixKey(e.target.value)} required placeholder="Digite sua chave" />
        </div>
        <div className="space-y-1">
          <label className="text-sm font-medium">CPF/CNPJ do titular (opcional)</label>
          <Input value={document} onChange={(e) => setDocumentValue(e.target.value)} placeholder="Apenas números" />
        </div>
        <Button type="submit" disabled={loading || !isValid} className="w-full">
          {loading ? 'Salvando...' : 'Salvar'}
        </Button>
      </form>

      <div className="rounded-lg border bg-primary/5 p-3 text-xs">
        <p className="text-muted-foreground">Email cadastrado</p>
        <p className="font-medium">{session.creator.email ?? 'Não informado'}</p>
      </div>
    </div>
  )
}
