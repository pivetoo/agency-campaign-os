import { useEffect, useState, useMemo } from 'react'
import {
  Button,
  Input,
  Modal,
  ModalContent,
  ModalFooter,
  ModalHeader,
  ModalTitle,
  Checkbox,
  useApi,
  useToast,
} from 'archon-ui'
import { ExternalLink, Eye, EyeOff, Sparkles } from 'lucide-react'
import { integrationPlatformService } from '../../services/integrationPlatformService'
import { getDocLink, smtpPresets } from '../../lib/integrationDocs'
import type {
  IntegrationPlatformIntegration,
  IntegrationAttribute,
  Connector,
  ConnectorAttributeValue,
  ConnectorAttributeValuePayload,
} from '../../types/integrationPlatform'

interface ConnectorConfigModalProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  integration: IntegrationPlatformIntegration | null
  connector: Connector | null
  onSuccess: () => void
}

export default function ConnectorConfigModal({
  open,
  onOpenChange,
  integration,
  connector,
  onSuccess,
}: ConnectorConfigModalProps) {
  const isEditing = !!connector
  const [connectorName, setConnectorName] = useState('')
  const [isActive, setIsActive] = useState(true)
  const [webhookToken, setWebhookToken] = useState<string | undefined>()
  const [attributes, setAttributes] = useState<IntegrationAttribute[]>([])
  const [values, setValues] = useState<Record<number, string>>({})
  const [visibleSensitive, setVisibleSensitive] = useState<Record<number, boolean>>({})
  const [errors, setErrors] = useState<Record<number, boolean>>({})

  const { execute: fetchAttributes, loading: loadingAttributes } = useApi<IntegrationAttribute[]>({
    showErrorMessage: true,
  })
  const { execute: fetchDetail, loading: loadingDetail } = useApi<{ connector: Connector; attributeValues: ConnectorAttributeValue[] }>({
    showErrorMessage: true,
  })
  const { execute: saveConnector, loading: saving } = useApi({ showSuccessMessage: true, showErrorMessage: true })
  const { toast } = useToast()

  useEffect(() => {
    if (!open || !integration) {
      setConnectorName('')
      setIsActive(true)
      setAttributes([])
      setValues({})
      setVisibleSensitive({})
      setErrors({})
      return
    }

    void loadAttributes()
  }, [open, integration])

  useEffect(() => {
    if (!open || !connector || !integration) {
      return
    }

    void loadExistingValues()
  }, [open, connector])

  const loadAttributes = async () => {
    if (!integration) return
    const result = await fetchAttributes(() =>
      integrationPlatformService.getIntegrationAttributes(integration.id)
    )
    if (result) {
      const sorted = result.sort((a, b) => a.order - b.order)
      setAttributes(sorted)

      const initialValues: Record<number, string> = {}
      sorted.forEach((attr) => {
        if (attr.defaultValue !== undefined && attr.defaultValue !== null) {
          initialValues[attr.id] = attr.defaultValue
        }
      })
      setValues(initialValues)
    }
  }

  const loadExistingValues = async () => {
    if (!connector) return

    const detail = await fetchDetail(() =>
      integrationPlatformService.getConnectorDetail(connector.id)
    )

    if (detail) {
      setConnectorName(detail.connector.name)
      setIsActive(detail.connector.isActive)
      setWebhookToken(detail.connector.webhookToken)

      const existingValues: Record<number, string> = {}
      detail.attributeValues.forEach((val) => {
        existingValues[val.integrationAttributeId] = val.value
      })
      setValues((prev) => ({ ...prev, ...existingValues }))
    }
  }

  const integrationPlatformBase = useMemo(() => {
    const apiBase = (import.meta.env.VITE_INTEGRATION_PLATFORM_URL as string | undefined)
      ?? (import.meta.env.VITE_INTEGRATION_PLATAFORM_URL as string | undefined)
    if (apiBase && apiBase.trim().length > 0) {
      return apiBase.replace(/\/$/, '')
    }
    return null
  }, [])

  const webhookUrl = useMemo(() => {
    if (!webhookToken) return null
    if (integrationPlatformBase) {
      return `${integrationPlatformBase}/api/webhooks/${webhookToken}`
    }
    return `[base-url-do-integration-platform]/api/webhooks/${webhookToken}`
  }, [webhookToken, integrationPlatformBase])

  const copyWebhookUrl = async () => {
    if (!webhookUrl) return
    try {
      await navigator.clipboard.writeText(webhookUrl)
      toast({ title: 'URL copiada para a área de transferência', variant: 'success' })
    } catch {
      toast({ title: 'Não foi possível copiar', variant: 'destructive' })
    }
  }

  const groupedAttributes = useMemo(() => {
    const groups: Record<string, IntegrationAttribute[]> = {}
    for (const attr of attributes) {
      const group = attr.group || 'Geral'
      if (!groups[group]) groups[group] = []
      groups[group].push(attr)
    }
    return groups
  }, [attributes])

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault()
    if (!integration || !connectorName.trim()) return

    const newErrors: Record<number, boolean> = {}
    let hasError = false

    for (const attr of attributes) {
      if (attr.isRequired && !values[attr.id]?.trim()) {
        newErrors[attr.id] = true
        hasError = true
      }
    }

    setErrors(newErrors)

    if (hasError) {
      toast({
        title: 'Validacao',
        description: 'Preencha todos os campos obrigatorios.',
        variant: 'destructive',
      })
      return
    }

    const attributeValues: ConnectorAttributeValuePayload[] = attributes
      .filter((attr) => values[attr.id] !== undefined && values[attr.id] !== '')
      .map((attr) => ({
        integrationAttributeId: attr.id,
        value: values[attr.id],
      }))

    if (isEditing && connector) {
      const result = await saveConnector(() =>
        integrationPlatformService.updateConnector(connector.id, {
          integrationId: integration.id,
          name: connectorName.trim(),
          isActive,
          attributeValues,
        })
      )

      if (result !== null) {
        onSuccess()
        onOpenChange(false)
      }
    } else {
      const result = await saveConnector(() =>
        integrationPlatformService.createConnector({
          integrationId: integration.id,
          name: connectorName.trim(),
          attributeValues,
        })
      )

      if (result !== null) {
        onSuccess()
        onOpenChange(false)
      }
    }
  }

  const isSensitiveField = (attr: IntegrationAttribute): boolean => {
    if (attr.isSensitive) return true
    const sensitiveKeywords = ['password', 'token', 'key', 'secret', 'senha', 'apikey', 'credential', 'pass', 'senha']
    const fieldLower = attr.field.toLowerCase()
    return sensitiveKeywords.some((kw) => fieldLower.includes(kw))
  }

  const renderInput = (attr: IntegrationAttribute) => {
    const value = values[attr.id] ?? ''
    const hasError = errors[attr.id]
    const isVisible = visibleSensitive[attr.id]
    const sensitive = isSensitiveField(attr)

    const commonClasses = hasError
      ? 'border-destructive focus-visible:ring-destructive'
      : ''

    switch (attr.type) {
      case 1: // Text
        if (sensitive) {
          return (
            <div className="relative">
              <Input
                type={isVisible ? 'text' : 'password'}
                placeholder={attr.placeholder || ''}
                value={value}
                onChange={(e) => setValues((prev) => ({ ...prev, [attr.id]: e.target.value }))}
                className={`pr-10 ${commonClasses}`}
              />
              <button
                type="button"
                onClick={() =>
                  setVisibleSensitive((prev) => ({
                    ...prev,
                    [attr.id]: !prev[attr.id],
                  }))
                }
                className="absolute right-2 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground"
              >
                {isVisible ? <EyeOff size={16} /> : <Eye size={16} />}
              </button>
            </div>
          )
        }
        return (
          <Input
            type="text"
            placeholder={attr.placeholder || ''}
            value={value}
            onChange={(e) =>
              setValues((prev) => ({ ...prev, [attr.id]: e.target.value }))
            }
            className={commonClasses}
          />
        )

      case 2: // LongText
        return (
          <textarea
            placeholder={attr.placeholder || ''}
            value={value}
            onChange={(e) =>
              setValues((prev) => ({ ...prev, [attr.id]: e.target.value }))
            }
            rows={4}
            className={`flex min-h-[80px] w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50 ${commonClasses}`}
          />
        )

      case 3: // Number
        return (
          <Input
            type="number"
            placeholder={attr.placeholder || ''}
            value={value}
            onChange={(e) =>
              setValues((prev) => ({ ...prev, [attr.id]: e.target.value }))
            }
            className={commonClasses}
          />
        )

      case 4: // Decimal
        return (
          <Input
            type="number"
            step="0.01"
            placeholder={attr.placeholder || ''}
            value={value}
            onChange={(e) =>
              setValues((prev) => ({ ...prev, [attr.id]: e.target.value }))
            }
            className={commonClasses}
          />
        )

      case 5: // Boolean
        return (
          <div className="flex items-center gap-2">
            <Checkbox
              checked={value === 'true'}
              onCheckedChange={(checked) =>
                setValues((prev) => ({
                  ...prev,
                  [attr.id]: checked ? 'true' : 'false',
                }))
              }
            />
            <span className="text-sm text-muted-foreground">
              {attr.placeholder || 'Ativar'}
            </span>
          </div>
        )

      case 6: // Date
        return (
          <Input
            type="date"
            value={value}
            onChange={(e) =>
              setValues((prev) => ({ ...prev, [attr.id]: e.target.value }))
            }
            className={commonClasses}
          />
        )

      case 7: // DateTime
        return (
          <Input
            type="datetime-local"
            value={value}
            onChange={(e) =>
              setValues((prev) => ({ ...prev, [attr.id]: e.target.value }))
            }
            className={commonClasses}
          />
        )

      case 8: // List
        return (
          <Input
            type="text"
            placeholder={attr.placeholder || 'Valor separado por vírgula'}
            value={value}
            onChange={(e) =>
              setValues((prev) => ({ ...prev, [attr.id]: e.target.value }))
            }
            className={commonClasses}
          />
        )

      default:
        return (
          <Input
            type="text"
            placeholder={attr.placeholder || ''}
            value={value}
            onChange={(e) =>
              setValues((prev) => ({ ...prev, [attr.id]: e.target.value }))
            }
            className={commonClasses}
          />
        )
    }
  }

  const applySmtpPreset = (presetId: string) => {
    const preset = smtpPresets.find((item) => item.id === presetId)
    if (!preset) return

    setValues((prev) => {
      const next = { ...prev }
      for (const attr of attributes) {
        const presetValue = preset.values[attr.field]
        if (presetValue !== undefined) {
          next[attr.id] = presetValue
        }
      }
      return next
    })
  }

  const showSmtpPresets = integration?.identifier === 'smtp' && !isEditing

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="full" style={{ maxWidth: '640px', width: '95vw' }}>
        <ModalHeader>
          <ModalTitle>
            {isEditing
              ? `Editar ${connector?.name}`
              : integration
                ? `Conectar ${integration.name}`
                : 'Conectar conta'}
          </ModalTitle>
        </ModalHeader>

        <form onSubmit={handleSubmit} className="space-y-5">
          <div className="space-y-2">
            <label className="text-sm font-medium">
              Apelido da conta <span className="text-destructive">*</span>
            </label>
            <Input
              value={connectorName}
              onChange={(e) => setConnectorName(e.target.value)}
              placeholder="Ex.: Conta principal"
              required
            />
            <p className="text-xs text-muted-foreground">
              Use um apelido que ajude a reconhecer (ex.: "Gmail da agência", "SendGrid prod").
            </p>
          </div>

          {showSmtpPresets && (
            <div className="rounded-lg border bg-primary/5 p-3 space-y-2">
              <div className="flex items-center gap-2">
                <Sparkles size={14} className="text-primary" />
                <p className="text-sm font-medium">Configurações rápidas</p>
              </div>
              <p className="text-xs text-muted-foreground">
                Selecione um servidor conhecido para preencher host, porta e SSL automaticamente.
              </p>
              <div className="flex flex-wrap gap-2">
                {smtpPresets.map((preset) => (
                  <button
                    key={preset.id}
                    type="button"
                    onClick={() => applySmtpPreset(preset.id)}
                    title={preset.description}
                    className="rounded-full border border-primary/30 bg-background px-3 py-1 text-xs font-medium text-primary hover:bg-primary/10 transition-colors"
                  >
                    {preset.label}
                  </button>
                ))}
              </div>
            </div>
          )}

          {isEditing && (
            <div className="flex items-center gap-2">
              <label className="flex items-center gap-2 text-sm">
                <input
                  type="checkbox"
                  checked={isActive}
                  onChange={(e) => setIsActive(e.target.checked)}
                  className="h-4 w-4"
                />
                <span>Ativo</span>
              </label>
            </div>
          )}

          {isEditing && webhookUrl && (
            <div className="rounded-lg border bg-primary/5 p-3 space-y-2">
              <div className="flex items-center justify-between gap-3">
                <div>
                  <p className="text-sm font-medium">URL para webhook</p>
                  <p className="text-xs text-muted-foreground">
                    Cadastre esta URL no painel do provedor para receber callbacks. O IntegrationPlatform direciona automaticamente ao pipeline <code className="rounded bg-background px-1 py-0.5">{integration?.identifier ?? 'identifier'}-webhook</code>.
                  </p>
                </div>
                <Button type="button" size="sm" variant="outline" onClick={copyWebhookUrl}>
                  Copiar
                </Button>
              </div>
              <code className="block break-all rounded bg-background p-2 font-mono text-xs">
                {webhookUrl}
              </code>
              {!integrationPlatformBase && (
                <p className="text-xs text-amber-600">
                  Defina <code>VITE_INTEGRATION_PLATFORM_URL</code> no env do frontend para a URL completa aparecer aqui.
                </p>
              )}
            </div>
          )}

          {loadingAttributes || loadingDetail ? (
            <p className="text-sm text-muted-foreground">Carregando...</p>
          ) : attributes.length === 0 ? (
            <p className="text-sm text-muted-foreground">
              Esta integração não exige configurações adicionais.
            </p>
          ) : (
            Object.entries(groupedAttributes).map(([group, groupAttrs]) => (
              <div key={group} className="space-y-3">
                {group !== 'Geral' && (
                  <h4 className="text-sm font-semibold text-muted-foreground border-b pb-1">
                    {group}
                  </h4>
                )}
                <div className="space-y-4">
                  {groupAttrs.map((attr) => {
                    const docLink = getDocLink(integration?.identifier, attr.field)
                    return (
                      <div key={attr.id} className="space-y-1.5">
                        <div className="flex items-center justify-between gap-2">
                          <label className="text-sm font-medium flex items-center gap-1">
                            {attr.label}
                            {attr.isRequired && (
                              <span className="text-destructive">*</span>
                            )}
                            {isSensitiveField(attr) && (
                              <span className="text-xs text-muted-foreground font-normal">
                                (sensível)
                              </span>
                            )}
                          </label>
                          {docLink && (
                            <a
                              href={docLink.url}
                              target="_blank"
                              rel="noopener noreferrer"
                              className="inline-flex items-center gap-1 text-xs text-primary hover:underline"
                            >
                              {docLink.label ?? 'Onde encontrar?'}
                              <ExternalLink size={11} />
                            </a>
                          )}
                        </div>
                        {attr.description && (
                          <p className="text-xs text-muted-foreground">
                            {attr.description}
                          </p>
                        )}
                        {renderInput(attr)}
                        {errors[attr.id] && (
                          <p className="text-xs text-destructive">
                            Campo obrigatório
                          </p>
                        )}
                      </div>
                    )
                  })}
                </div>
              </div>
            ))
          )}

          <ModalFooter>
            <Button
              type="button"
              variant="outline"
              onClick={() => onOpenChange(false)}
            >
              Cancelar
            </Button>
            <Button
              type="submit"
              disabled={saving || !connectorName.trim() || loadingAttributes || loadingDetail}
            >
              {saving ? 'Salvando...' : isEditing ? 'Atualizar' : 'Salvar'}
            </Button>
          </ModalFooter>
        </form>
      </ModalContent>
    </Modal>
  )
}
