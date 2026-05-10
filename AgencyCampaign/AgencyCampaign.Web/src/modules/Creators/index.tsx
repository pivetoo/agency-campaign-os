import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { PageLayout, DataTable, useApi, Sheet, SheetContent, SheetPreviewField, SheetPreviewGrid, SheetPreviewHeader, SheetPreviewSection, Badge, useI18n } from 'archon-ui'
import type { DataTableColumn } from 'archon-ui'
import { Link as LinkIcon } from 'lucide-react'

import { creatorService, resolveCreatorPhotoUrl } from '../../services/creatorService'
import type { Creator } from '../../types/creator'
import CreatorFormModal from '../../components/modals/CreatorFormModal'
import CreatorAccessTokensModal from '../../components/modals/CreatorAccessTokensModal'

export default function Creators() {
  const { t } = useI18n()
  const navigate = useNavigate()
  const [creators, setCreators] = useState<Creator[]>([])
  const [selectedCreator, setSelectedCreator] = useState<Creator | null>(null)
  const [previewCreator, setPreviewCreator] = useState<Creator | null>(null)
  const [isFormOpen, setIsFormOpen] = useState(false)
  const [isTokensOpen, setIsTokensOpen] = useState(false)
  const { execute: fetchCreators, loading } = useApi<Creator[]>({ showErrorMessage: true })

  const loadCreators = async () => {
    const result = await fetchCreators(() => creatorService.getAll())
    if (result) {
      setCreators(result)
    }
  }

  useEffect(() => {
    void loadCreators()
  }, [])

  const renderPhotoCell = (_: unknown, record: Creator) => {
    const url = resolveCreatorPhotoUrl(record.photoUrl)
    const initial = (record.stageName?.trim() || record.name?.trim() || '?').charAt(0).toUpperCase()
    return (
      <div className="flex h-9 w-9 items-center justify-center overflow-hidden rounded-full border bg-muted/30">
        {url ? (
          <img src={url} alt={record.name} className="h-full w-full object-cover" />
        ) : (
          <span className="text-xs font-semibold text-muted-foreground">{initial}</span>
        )}
      </div>
    )
  }

  const columns: DataTableColumn<Creator>[] = [
    { key: 'photo', title: '', dataIndex: 'photoUrl', width: 56, render: renderPhotoCell },
    { key: 'name', title: t('common.field.name'), dataIndex: 'name' },
    { key: 'stageName', title: t('creators.field.stageName'), dataIndex: 'stageName', render: (value?: string) => value || '-' },
    { key: 'primaryNiche', title: t('creators.field.niche'), dataIndex: 'primaryNiche', render: (value?: string) => value || '-' },
    { key: 'city', title: t('common.field.city'), dataIndex: 'city', render: (value?: string) => value || '-' },
    {
      key: 'isActive',
      title: t('common.field.status'),
      dataIndex: 'isActive',
      render: (value: boolean) => (
        <Badge variant={value ? 'success' : 'destructive'}>
          {value ? t('common.status.active') : t('common.status.inactive')}
        </Badge>
      ),
    },
    {
      key: 'actions',
      title: '',
      width: 96,
      render: (_: unknown, record: Creator) => (
        <button
          type="button"
          className="text-xs text-primary hover:underline"
          onClick={(event) => { event.stopPropagation(); navigate(`/creators/${record.id}`) }}
        >
          {t('creators.action.open360')}
        </button>
      ),
    },
  ]

  return (
    <>
      <PageLayout
        title={t('creators.title')}
        subtitle={t('creators.subtitle')}
        onAdd={() => { setSelectedCreator(null); setIsFormOpen(true) }}
        onEdit={() => selectedCreator && setIsFormOpen(true)}
        onRefresh={() => void loadCreators()}
        selectedRowsCount={selectedCreator ? 1 : 0}
        actions={[
          {
            key: 'access-tokens',
            label: t('creators.action.portalLinks'),
            icon: <LinkIcon className="h-4 w-4" />,
            variant: 'outline',
            disabled: !selectedCreator,
            onClick: () => selectedCreator && setIsTokensOpen(true),
          },
        ]}
      >
        <div data-tour="creators-table">
          <DataTable
            columns={columns}
            data={creators}
            rowKey="id"
            selectedRows={selectedCreator ? [selectedCreator] : []}
            onSelectionChange={(rows) => setSelectedCreator(rows[0] ?? null)}
            onRowDoubleClick={setPreviewCreator}
            emptyText={t('creators.empty')}
            loading={loading}
            pageSize={5}
            pageSizeOptions={[5, 10, 20, 50]}
          />
        </div>
      </PageLayout>

      <CreatorFormModal
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        creator={selectedCreator}
        onSuccess={() => {
          setIsFormOpen(false)
          setSelectedCreator(null)
          void loadCreators()
        }}
      />

      <CreatorAccessTokensModal
        open={isTokensOpen}
        onOpenChange={setIsTokensOpen}
        creator={selectedCreator}
      />

      <Sheet open={!!previewCreator} onOpenChange={(open) => !open && setPreviewCreator(null)}>
        <SheetContent side="right" className="w-full sm:max-w-md">
          {previewCreator ? (
            <div className="flex h-full flex-col">
              {(() => {
                const photoSrc = resolveCreatorPhotoUrl(previewCreator.photoUrl)
                const initial = (previewCreator.stageName?.trim() || previewCreator.name?.trim() || '?').charAt(0).toUpperCase()
                return (
                  <div className="mb-4 flex items-center justify-center">
                    <div className="flex items-center justify-center overflow-hidden rounded-full border bg-muted/20" style={{ width: 140, height: 140 }}>
                      {photoSrc ? (
                        <img src={photoSrc} alt={previewCreator.name} className="h-full w-full object-cover" />
                      ) : (
                        <span className="text-4xl font-semibold text-muted-foreground">{initial}</span>
                      )}
                    </div>
                  </div>
                )
              })()}
              <SheetPreviewHeader
                title={previewCreator.stageName || previewCreator.name}
                meta={
                  <Badge variant={previewCreator.isActive ? 'success' : 'destructive'}>
                    {previewCreator.isActive ? t('common.status.active') : t('common.status.inactive')}
                  </Badge>
                }
                description={t('creators.preview.description')}
              />

              <div className="mt-6 flex-1 space-y-4 overflow-y-auto">
                <SheetPreviewSection title={t('creators.preview.dataSection')} description={t('creators.preview.dataSectionDesc')}>
                  <SheetPreviewGrid>
                    <SheetPreviewField label={t('common.field.name')} value={previewCreator.name} />
                    <SheetPreviewField label={t('creators.field.stageName')} value={previewCreator.stageName || '-'} />
                    <SheetPreviewField label={t('common.field.document')} value={previewCreator.document || '-'} />
                    <SheetPreviewField label={t('common.field.phone')} value={previewCreator.phone || '-'} />
                    <SheetPreviewField label={t('creators.field.pixKey')} value={previewCreator.pixKey || '-'} />
                    <SheetPreviewField label={t('creators.field.niche')} value={previewCreator.primaryNiche || '-'} />
                    <SheetPreviewField label={t('common.field.city')} value={previewCreator.city || '-'} />
                    <SheetPreviewField label={t('common.field.state')} value={previewCreator.state || '-'} />
                    <SheetPreviewField label={t('creators.field.defaultFee')} value={previewCreator.defaultAgencyFeePercent.toFixed(2)} />
                    <SheetPreviewField className="sm:col-span-2" label={t('common.field.email')} value={previewCreator.email || '-'} />
                    <SheetPreviewField className="sm:col-span-2" label={t('common.field.notes')} value={previewCreator.notes || '-'} />
                  </SheetPreviewGrid>
                </SheetPreviewSection>
              </div>
            </div>
          ) : null}
        </SheetContent>
      </Sheet>
    </>
  )
}
