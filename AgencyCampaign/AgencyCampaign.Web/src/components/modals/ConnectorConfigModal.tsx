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
import { Eye, EyeOff } from 'lucide-react'
import { integrationPlataformService } from '../../services/integrationPlataformService'
import type {
  IntegrationPlataformIntegration,
  IntegrationAttribute,
} from '../../types/integrationPlataform'

interface ConnectorConfigModalProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  integration: IntegrationPlataformIntegration | null
  onSuccess: () => void
}

export default function ConnectorConfigModal({
  open,
  onOpenChange,
  integration,
  onSuccess,
}: ConnectorConfigModalProps) {
  const [connectorName, setConnectorName] = useState('')
  const [attributes, setAttributes] = useState<IntegrationAttribute[]>([])
  const [values, setValues] = useState<Record<number, string>>({})
  const [visibleSensitive, setVisibleSensitive] = useState<Record<number, boolean>>({})
  const [errors, setErrors] = useState<Record<number, boolean>>({})

  const { execute: fetchAttributes, loading: loadingAttributes } = useApi<IntegrationAttribute[]>({
    showErrorMessage: true,
  })
  const { execute: createConnector, loading: saving } = useApi({ showSuccessMessage: true, showErrorMessage: true })
  const { toast } = useToast()

  useEffect(() => {
    if (!open || !integration) {
      setConnectorName('')
      setAttributes([])
      setValues({})
      setVisibleSensitive({})
      setErrors({})
      return
    }

    void loadAttributes()
  }, [open, integration])

  const loadAttributes = async () => {
    if (!integration) return
    const result = await fetchAttributes(() =>
      integrationPlataformService.getIntegrationAttributes(integration.id)
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

    const attributeValues = attributes
      .filter((attr) => values[attr.id] !== undefined && values[attr.id] !== '')
      .map((attr) => ({
        integrationAttributeId: attr.id,
        value: values[attr.id],
      }))

    const result = await createConnector(() =>
      integrationPlataformService.createConnector({
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

  const renderInput = (attr: IntegrationAttribute) => {
    const value = values[attr.id] ?? ''
    const hasError = errors[attr.id]
    const isVisible = visibleSensitive[attr.id]

    const commonClasses = hasError
      ? 'border-destructive focus-visible:ring-destructive'
      : ''

    switch (attr.type) {
      case 1: // Text
        if (attr.isSensitive) {
          return (
            <div className="relative">
              <Input
                type={isVisible ? 'text' : 'password'}
                placeholder={attr.placeholder || ''}
                value={value}
                onChange={(e) =>
                  setValues((prev) => ({ ...prev, [attr.id]: e.target.value }))
                }
                className={commonClasses}
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

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="full" style={{ maxWidth: '640px', width: '95vw' }}>
        <ModalHeader>
          <ModalTitle>
            {integration ? `Configurar ${integration.name}` : 'Configurar integração'}
          </ModalTitle>
        </ModalHeader>

        <form onSubmit={handleSubmit} className="space-y-5">
          <div className="space-y-2">
            <label className="text-sm font-medium">
              Nome do conector <span className="text-destructive">*</span>
            </label>
            <Input
              value={connectorName}
              onChange={(e) => setConnectorName(e.target.value)}
              placeholder="Ex: Conector de produção"
              required
            />
          </div>

          {loadingAttributes ? (
            <p className="text-sm text-muted-foreground">Carregando atributos...</p>
          ) : attributes.length === 0 ? (
            <p className="text-sm text-muted-foreground">
              Esta integração não possui atributos configuráveis.
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
                  {groupAttrs.map((attr) => (
                    <div key={attr.id} className="space-y-1.5">
                      <label className="text-sm font-medium flex items-center gap-1">
                        {attr.label}
                        {attr.isRequired && (
                          <span className="text-destructive">*</span>
                        )}
                        {attr.isSensitive && (
                          <span className="text-xs text-muted-foreground font-normal">
                            (sensível)
                          </span>
                        )}
                      </label>
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
                  ))}
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
              disabled={saving || !connectorName.trim() || loadingAttributes}
            >
              {saving ? 'Salvando...' : 'Salvar'}
            </Button>
          </ModalFooter>
        </form>
      </ModalContent>
    </Modal>
  )
}
