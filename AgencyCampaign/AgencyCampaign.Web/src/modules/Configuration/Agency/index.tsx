import { useEffect, useRef, useState } from 'react'
import { PageLayout, Card, CardContent, Button, Input, useApi, useI18n } from 'archon-ui'
import { ImagePlus, Trash2 } from 'lucide-react'
import { agencySettingsService, resolveAgencyLogoUrl } from '../../../services/agencySettingsService'
import type { AgencySettings } from '../../../types/agencySettings'

const ACCEPTED_TYPES = ['image/png', 'image/jpeg', 'image/jpg', 'image/webp']
const MAX_BYTES = 2 * 1024 * 1024

export default function AgencyConfiguration() {
  const { t } = useI18n()
  const [settings, setSettings] = useState<AgencySettings | null>(null)
  const [agencyName, setAgencyName] = useState('')
  const [tradeName, setTradeName] = useState('')
  const [document, setDocument] = useState('')
  const [logoUrl, setLogoUrl] = useState('')
  const [primaryColor, setPrimaryColor] = useState('#6366f1')

  const fileInputRef = useRef<HTMLInputElement>(null)
  const [logoError, setLogoError] = useState<string | null>(null)
  const { execute: fetchSettings, loading } = useApi<AgencySettings | null>({ showErrorMessage: true })
  const { execute: saveSettings, loading: saving } = useApi({ showSuccessMessage: true, showErrorMessage: true })
  const { execute: runLogoUpload, loading: uploadingLogo } = useApi<AgencySettings | null>({ showSuccessMessage: true, showErrorMessage: true })
  const { execute: runLogoRemove, loading: removingLogo } = useApi<AgencySettings | null>({ showSuccessMessage: true, showErrorMessage: true })

  const load = async () => {
    const result = await fetchSettings(() => agencySettingsService.get())
    if (result) {
      setSettings(result)
      setAgencyName(result.agencyName)
      setTradeName(result.tradeName ?? '')
      setDocument(result.document ?? '')
      setLogoUrl(result.logoUrl ?? '')
      setPrimaryColor(result.primaryColor ?? '#6366f1')
    }
  }

  useEffect(() => {
    void load()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const handleSelectLogo = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0]
    event.target.value = ''
    setLogoError(null)

    if (!file) return

    if (!ACCEPTED_TYPES.includes(file.type)) {
      setLogoError(t('configuration.agency.logoError.format'))
      return
    }

    if (file.size > MAX_BYTES) {
      setLogoError(t('configuration.agency.logoError.size'))
      return
    }

    const result = await runLogoUpload(async () => {
      const response = await agencySettingsService.uploadLogo(file)
      return response.data ?? null
    })

    if (result) setLogoUrl(result.logoUrl ?? '')
  }

  const handleRemoveLogo = async () => {
    if (!logoUrl) return
    const result = await runLogoRemove(async () => {
      const response = await agencySettingsService.removeLogo()
      return response.data ?? null
    })

    if (result) setLogoUrl(result.logoUrl ?? '')
  }

  const submit = async (event: React.FormEvent) => {
    event.preventDefault()
    const result = await saveSettings(() =>
      agencySettingsService.update({
        agencyName: agencyName.trim(),
        tradeName: tradeName.trim() || null,
        document: document.trim() || null,
        logoUrl: logoUrl.trim() || null,
        primaryColor: primaryColor.trim() || null,
        defaultEmailConnectorId: settings?.defaultEmailConnectorId ?? null,
        defaultEmailPipelineId: settings?.defaultEmailPipelineId ?? null,
      }),
    )
    if (result !== null) void load()
  }

  return (
    <PageLayout
      title={t('configuration.agency.title')}
      subtitle={t('configuration.agency.brandIdentity')}
      onRefresh={() => void load()}
      showDefaultActions={false}
    >
      <form onSubmit={submit} className="space-y-4">
        <Card>
          <CardContent className="pt-5 pb-5 space-y-4">
            <h3 className="text-sm font-semibold uppercase tracking-wide text-muted-foreground">{t('configuration.agency.identity')}</h3>
            <div className="grid grid-cols-1 gap-3 md:grid-cols-2">
              <div className="space-y-2">
                <label className="text-sm font-medium">{t('configuration.agency.legalName')}</label>
                <Input value={agencyName} onChange={(e) => setAgencyName(e.target.value)} required />
              </div>
              <div className="space-y-2">
                <label className="text-sm font-medium">{t('common.field.tradeName')}</label>
                <Input value={tradeName} onChange={(e) => setTradeName(e.target.value)} />
              </div>
              <div className="space-y-2">
                <label className="text-sm font-medium">{t('configuration.agency.document')}</label>
                <Input value={document} onChange={(e) => setDocument(e.target.value)} />
              </div>
              <div className="space-y-2">
                <label className="text-sm font-medium">{t('configuration.agency.primaryColor')}</label>
                <Input type="color" value={primaryColor} onChange={(e) => setPrimaryColor(e.target.value)} />
              </div>
              <div className="space-y-2 md:col-span-2">
                <label className="text-sm font-medium">{t('configuration.agency.logo')}</label>
                <div className="flex items-start gap-4">
                  <div
                    className="relative flex items-center justify-center overflow-hidden rounded-lg border border-dashed bg-muted/30 shrink-0"
                    style={{ width: 140, height: 140 }}
                  >
                    {logoUrl ? (
                      <img src={resolveAgencyLogoUrl(logoUrl)} alt="Logo da agência" className="h-full w-full object-contain" />
                    ) : (
                      <div className="flex flex-col items-center gap-1 text-xs text-muted-foreground">
                        <ImagePlus className="h-6 w-6" />
                        <span>{t('configuration.agency.noLogo')}</span>
                      </div>
                    )}
                  </div>
                  <input
                    ref={fileInputRef}
                    type="file"
                    accept={ACCEPTED_TYPES.join(',')}
                    onChange={(e) => void handleSelectLogo(e)}
                    className="hidden"
                  />
                  <div className="flex flex-col gap-2">
                    <Button
                      type="button"
                      variant="outline"
                      size="sm"
                      onClick={() => fileInputRef.current?.click()}
                      disabled={uploadingLogo || removingLogo}
                    >
                      {uploadingLogo ? 'Enviando...' : (logoUrl ? 'Trocar logo' : 'Enviar logo')}
                    </Button>
                    {logoUrl && (
                      <Button
                        type="button"
                        variant="ghost"
                        size="sm"
                        onClick={() => void handleRemoveLogo()}
                        disabled={uploadingLogo || removingLogo}
                      >
                        <Trash2 className="mr-1 h-3 w-3" /> {t('common.action.remove')}
                      </Button>
                    )}
                    <p className="text-xs text-muted-foreground">{t('configuration.agency.logoHint')}</p>
                    {logoError && <p className="text-xs text-destructive">{logoError}</p>}
                  </div>
                </div>
              </div>
            </div>
          </CardContent>
        </Card>

        <div className="flex justify-end">
          <Button type="submit" disabled={loading || saving || !settings}>
            {saving ? 'Salvando...' : 'Salvar configurações'}
          </Button>
        </div>
      </form>
    </PageLayout>
  )
}
