import { useI18n } from 'archon-ui'
import FinancialEntriesPage from './EntriesPage'

export default function FinancialReceivables() {
  const { t } = useI18n()
  return (
    <FinancialEntriesPage
      type={1}
      title={t('financial.receivables.title')}
      subtitle={t('financial.receivables.subtitle')}
    />
  )
}
