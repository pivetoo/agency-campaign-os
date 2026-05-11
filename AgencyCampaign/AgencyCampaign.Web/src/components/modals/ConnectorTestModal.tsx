import { useEffect, useState } from 'react'
import {
  Button,
  Input,
  Modal,
  ModalContent,
  ModalFooter,
  ModalHeader,
  ModalTitle,
  useI18n,
} from 'archon-ui'
import { CheckCircle2, Loader2, XCircle } from 'lucide-react'
import { integrationPlatformService, type TestConnectorResult } from '../../services/integrationPlatformService'
import type { Connector, IntegrationCategory, IntegrationPlatformIntegration } from '../../types/integrationPlatform'

interface Props {
  open: boolean
  onOpenChange: (open: boolean) => void
  connector: Connector | null
  integration: IntegrationPlatformIntegration | null
  category: IntegrationCategory | null
}

interface TestField {
  key: string
  label: string
  placeholder: string
  type?: 'text' | 'email' | 'tel'
  defaultValue?: string
}

function getTestFields(categoryName: string | undefined): TestField[] {
  const name = (categoryName ?? '').toLowerCase()
  if (name.includes('email')) {
    return [{ key: 'to', label: 'Email destino', placeholder: 'voce@empresa.com', type: 'email' }]
  }
  if (name.includes('whatsapp')) {
    return [{ key: 'to', label: 'Número (com DDI)', placeholder: '+5511999999999', type: 'tel' }]
  }
  if (name.includes('receber') || name.includes('cobr')) {
    return [
      { key: 'amount', label: 'Valor R$', placeholder: '1.00', defaultValue: '1.00' },
      { key: 'description', label: 'Descrição', placeholder: 'Cobrança de teste', defaultValue: 'Cobrança de teste Kanvas' },
    ]
  }
  if (name.includes('pagar') || name.includes('repasse')) {
    return [
      { key: 'pix_key', label: 'Chave PIX destino', placeholder: 'email ou CPF/CNPJ' },
      { key: 'amount', label: 'Valor R$', placeholder: '0.01', defaultValue: '0.01' },
    ]
  }
  if (name.includes('assinatura')) {
    return [{ key: 'email', label: 'Email do signatário', placeholder: 'signatario@example.com', type: 'email' }]
  }
  return []
}

export default function ConnectorTestModal({ open, onOpenChange, connector, integration, category }: Props) {
  const { t } = useI18n()
  const [inputs, setInputs] = useState<Record<string, string>>({})
  const [testing, setTesting] = useState(false)
  const [result, setResult] = useState<TestConnectorResult | null>(null)

  const fields = getTestFields(category?.name)

  useEffect(() => {
    if (!open) {
      setInputs({})
      setResult(null)
      setTesting(false)
      return
    }
    const initial: Record<string, string> = {}
    for (const field of fields) {
      if (field.defaultValue) initial[field.key] = field.defaultValue
    }
    setInputs(initial)
    setResult(null)
  }, [open, category?.id])

  const runTest = async () => {
    if (!connector) return

    setTesting(true)
    setResult(null)
    try {
      const inputData: Record<string, unknown> = { test: true, ...inputs }
      const response = await integrationPlatformService.testConnector(connector.id, inputData)
      setResult(response)
    } catch (error) {
      setResult({
        success: false,
        message: error instanceof Error ? error.message : 'Erro inesperado.',
        latencyMs: 0,
      })
    } finally {
      setTesting(false)
    }
  }

  const isValid = fields.every((field) => inputs[field.key]?.trim().length)

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="full" style={{ maxWidth: '520px', width: '95vw' }}>
        <ModalHeader>
          <ModalTitle>
            {t('modal.connector.title.test').replace('{0}', connector?.name ?? 'conta')}
          </ModalTitle>
        </ModalHeader>

        <div className="space-y-4">
          <div className="rounded-lg border bg-muted/30 p-3 text-sm">
            <p className="font-medium">{integration?.name ?? '—'}</p>
            <p className="text-xs text-muted-foreground">
              Vamos executar o pipeline <code className="rounded bg-background px-1 py-0.5 text-[10px]">{integration?.identifier}-test-connection</code> com os dados abaixo.
            </p>
          </div>

          {fields.length === 0 ? (
            <p className="text-sm text-muted-foreground">
              Esta categoria não pede dados extras. Vamos validar apenas a conexão e credenciais.
            </p>
          ) : (
            fields.map((field) => (
              <div key={field.key} className="space-y-1.5">
                <label className="text-sm font-medium">{field.label}</label>
                <Input
                  type={field.type ?? 'text'}
                  placeholder={field.placeholder}
                  value={inputs[field.key] ?? ''}
                  onChange={(e) => setInputs((prev) => ({ ...prev, [field.key]: e.target.value }))}
                />
              </div>
            ))
          )}

          {result && (
            <div
              className={[
                'flex items-start gap-2 rounded-lg border p-3 text-sm',
                result.success
                  ? 'border-emerald-500/30 bg-emerald-500/5 text-emerald-700 dark:text-emerald-300'
                  : 'border-destructive/30 bg-destructive/5 text-destructive',
              ].join(' ')}
            >
              {result.success ? (
                <CheckCircle2 size={18} className="mt-0.5 flex-shrink-0" />
              ) : (
                <XCircle size={18} className="mt-0.5 flex-shrink-0" />
              )}
              <div className="space-y-0.5">
                <p className="font-medium">{result.success ? 'Funcionou!' : 'Falhou.'}</p>
                <p className="text-xs">{result.message}</p>
                {result.latencyMs > 0 && (
                  <p className="text-[10px] opacity-70">Tempo: {result.latencyMs}ms</p>
                )}
              </div>
            </div>
          )}
        </div>

        <ModalFooter>
          <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
            {t('common.action.close')}
          </Button>
          <Button type="button" onClick={runTest} disabled={testing || !isValid}>
            {testing && <Loader2 size={14} className="mr-1.5 animate-spin" />}
            {testing ? t('common.action.testing') : result ? t('common.action.retest') : t('common.action.test')}
          </Button>
        </ModalFooter>
      </ModalContent>
    </Modal>
  )
}
