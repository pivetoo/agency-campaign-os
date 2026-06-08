import { PageLayout } from 'archon-ui'
import CapabilityList from '../../Configuration/Integrations/CapabilityList'

export default function IntegrationActions() {
  return (
    <PageLayout title="Ações disponíveis" subtitle="Operações que as integrações expõem para uso nas automações.">
      <CapabilityList />
    </PageLayout>
  )
}
