import { useEffect, useMemo, useState } from 'react'
import { Button, Modal, ModalContent, ModalDescription, ModalFooter, ModalHeader, ModalTitle, SearchableSelect, useApi, useI18n } from 'archon-ui'
import { financialAccountService } from '../../services/financialAccountService'
import { integrationPlatformService } from '../../services/integrationPlatformService'
import { IntegrationCategoryIdentifier, type Connector } from '../../types/integrationPlatform'
import type { FinancialAccount } from '../../types/financialAccount'

interface Props {
  open: boolean
  onOpenChange: (open: boolean) => void
  account: FinancialAccount | null
  onSuccess: () => void
}

export default function FinancialAccountConnectorBindingModal({ open, onOpenChange, account, onSuccess }: Props) {
  const { t } = useI18n()
  const [connectors, setConnectors] = useState<Connector[]>([])
  const [loadingConnectors, setLoadingConnectors] = useState(false)
  const [selectedConnectorId, setSelectedConnectorId] = useState<string>('')
  const { execute: runAttach, loading: attaching } = useApi({ showSuccessMessage: true, showErrorMessage: true })
  const { execute: runDetach, loading: detaching } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  const currentConnectorName = useMemo(() => {
    if (!account?.integrationConnectorId) return null
    return connectors.find((connector) => connector.id === account.integrationConnectorId)?.name ?? null
  }, [account, connectors])

  useEffect(() => {
    if (!open) return
    let cancelled = false
    setLoadingConnectors(true)
    integrationPlatformService.getConnectorsByCategoryIdentifier(IntegrationCategoryIdentifier.Banking)
      .then((list) => { if (!cancelled) setConnectors(list) })
      .catch(() => { if (!cancelled) setConnectors([]) })
      .finally(() => { if (!cancelled) setLoadingConnectors(false) })
    setSelectedConnectorId(account?.integrationConnectorId ? String(account.integrationConnectorId) : '')
    return () => { cancelled = true }
  }, [open, account])

  const connectorOptions = useMemo(
    () => connectors.map((connector) => ({ value: String(connector.id), label: connector.name })),
    [connectors],
  )

  const handleAttach = async () => {
    if (!account || !selectedConnectorId) return
    const result = await runAttach(() => financialAccountService.attachConnector(account.id, Number(selectedConnectorId)))
    if (result !== null) onSuccess()
  }

  const handleDetach = async () => {
    if (!account) return
    const result = await runDetach(() => financialAccountService.detachConnector(account.id))
    if (result !== null) onSuccess()
  }

  const hasConnector = !!account?.integrationConnectorId
  const canAttach = !!selectedConnectorId && selectedConnectorId !== String(account?.integrationConnectorId ?? '')

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="form">
        <ModalHeader>
          <ModalTitle>{t('configuration.bankAccounts.connector.modal.title')}</ModalTitle>
          <ModalDescription>{t('configuration.bankAccounts.connector.modal.description')}</ModalDescription>
        </ModalHeader>
        <div className="space-y-4">
          {hasConnector && (
            <div className="rounded-md border bg-muted/30 p-3 text-sm">
              <div className="font-medium">{t('configuration.bankAccounts.connector.modal.current')}</div>
              <div className="text-muted-foreground">{currentConnectorName ?? `#${account?.integrationConnectorId}`}</div>
            </div>
          )}
          <div className="space-y-2">
            <label className="text-sm font-medium">{t('configuration.bankAccounts.connector.modal.selectConnector')}</label>
            <SearchableSelect
              value={selectedConnectorId}
              onValueChange={setSelectedConnectorId}
              options={connectorOptions}
              placeholder={loadingConnectors ? t('common.action.saving') : t('configuration.bankAccounts.connector.modal.selectConnector')}
            />
            {!loadingConnectors && connectorOptions.length === 0 && (
              <p className="text-xs text-muted-foreground">{t('configuration.bankAccounts.connector.modal.empty')}</p>
            )}
          </div>
        </div>
        <ModalFooter>
          {hasConnector && (
            <Button type="button" variant="outline-danger" onClick={() => void handleDetach()} disabled={detaching || attaching}>
              {detaching ? t('common.action.saving') : t('configuration.bankAccounts.action.detachConnector')}
            </Button>
          )}
          <div className="flex-1" />
          <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>{t('common.action.cancel')}</Button>
          <Button type="button" onClick={() => void handleAttach()} disabled={!canAttach || attaching || detaching}>
            {attaching ? t('common.action.saving') : hasConnector
              ? t('configuration.bankAccounts.action.changeConnector')
              : t('configuration.bankAccounts.action.attachConnector')}
          </Button>
        </ModalFooter>
      </ModalContent>
    </Modal>
  )
}
