import { useI18n } from 'archon-ui'
import FinancialEntriesPage from './EntriesPage'

export default function FinancialPayables() {
  const { t } = useI18n()
  return (
    <FinancialEntriesPage
      type={2}
      title={t('financial.payables.title')}
      subtitle={t('financial.payables.subtitle')}
    />
  )
}
