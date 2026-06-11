import { PageLayout, useI18n } from 'archon-ui'
import CommercialAttentionPanel from './CommercialAttentionPanel'

export default function CommercialAttention() {
  const { t } = useI18n()
  return (
    <PageLayout title={t('commercialAttention.title')} subtitle={t('commercialAttention.subtitle')}>
      <div className="max-w-2xl">
        <CommercialAttentionPanel />
      </div>
    </PageLayout>
  )
}
