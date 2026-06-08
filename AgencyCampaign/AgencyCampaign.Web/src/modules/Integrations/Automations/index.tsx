import { useState } from 'react'
import { PageLayout } from 'archon-ui'
import AutomationList from '../../Configuration/Automations/AutomationList'
import AutomationFormModal from '../../../components/modals/AutomationFormModal'
import type { Automation } from '../../../types/automation'

export default function IntegrationAutomations() {
  const [isModalOpen, setIsModalOpen] = useState(false)
  const [selectedAutomation, setSelectedAutomation] = useState<Automation | null>(null)
  const [refreshKey, setRefreshKey] = useState(0)

  return (
    <>
      <PageLayout title="Automações" subtitle="Regras que conectam eventos do sistema a ações das integrações.">
        <AutomationList
          key={refreshKey}
          onCreate={() => { setSelectedAutomation(null); setIsModalOpen(true) }}
          onEdit={(automation) => { setSelectedAutomation(automation); setIsModalOpen(true) }}
        />
      </PageLayout>

      <AutomationFormModal
        open={isModalOpen}
        onOpenChange={setIsModalOpen}
        automation={selectedAutomation}
        presetConnectorId={null}
        presetCategoryId={null}
        presetIntegrationId={null}
        onSuccess={() => {
          setIsModalOpen(false)
          setSelectedAutomation(null)
          setRefreshKey((prev) => prev + 1)
        }}
      />
    </>
  )
}
