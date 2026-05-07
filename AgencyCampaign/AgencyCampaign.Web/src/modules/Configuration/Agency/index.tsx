import { useEffect, useState } from 'react'
import { PageLayout, Card, CardContent, Button, Input, useApi, SearchableSelect } from 'archon-ui'
import { agencySettingsService } from '../../../services/agencySettingsService'
import { integrationPlatformService } from '../../../services/integrationPlatformService'
import type { AgencySettings } from '../../../types/agencySettings'
import type {
  Connector,
  IntegrationCategory,
  IntegrationPlatformIntegration,
  Pipeline,
} from '../../../types/integrationPlatform'

export default function AgencyConfiguration() {
  const [settings, setSettings] = useState<AgencySettings | null>(null)
  const [agencyName, setAgencyName] = useState('')
  const [tradeName, setTradeName] = useState('')
  const [document, setDocument] = useState('')
  const [primaryEmail, setPrimaryEmail] = useState('')
  const [phone, setPhone] = useState('')
  const [address, setAddress] = useState('')
  const [logoUrl, setLogoUrl] = useState('')
  const [primaryColor, setPrimaryColor] = useState('#6366f1')
  const [emailCategoryId, setEmailCategoryId] = useState<number | null>(null)
  const [emailIntegrationId, setEmailIntegrationId] = useState<number | null>(null)
  const [emailConnectorId, setEmailConnectorId] = useState<number | null>(null)
  const [emailPipelineId, setEmailPipelineId] = useState<number | null>(null)

  const [categories, setCategories] = useState<IntegrationCategory[]>([])
  const [integrations, setIntegrations] = useState<IntegrationPlatformIntegration[]>([])
  const [connectors, setConnectors] = useState<Connector[]>([])
  const [pipelines, setPipelines] = useState<Pipeline[]>([])

  const { execute: fetchSettings, loading } = useApi<AgencySettings | null>({ showErrorMessage: true })
  const { execute: saveSettings, loading: saving } = useApi({ showSuccessMessage: true, showErrorMessage: true })

  const load = async () => {
    const result = await fetchSettings(() => agencySettingsService.get())
    if (result) {
      setSettings(result)
      setAgencyName(result.agencyName)
      setTradeName(result.tradeName ?? '')
      setDocument(result.document ?? '')
      setPrimaryEmail(result.primaryEmail ?? '')
      setPhone(result.phone ?? '')
      setAddress(result.address ?? '')
      setLogoUrl(result.logoUrl ?? '')
      setPrimaryColor(result.primaryColor ?? '#6366f1')
      setEmailConnectorId(result.defaultEmailConnectorId ?? null)
      setEmailPipelineId(result.defaultEmailPipelineId ?? null)
    }
  }

  useEffect(() => {
    void load()
    void integrationPlatformService.getActiveIntegrationCategories().then(setCategories)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  useEffect(() => {
    if (!emailConnectorId) return
    void integrationPlatformService.getConnectorDetail(emailConnectorId).then((detail) => {
      if (detail?.connector?.integrationId) setEmailIntegrationId(detail.connector.integrationId)
    })
  }, [emailConnectorId])

  useEffect(() => {
    if (!emailCategoryId) {
      setIntegrations([])
      return
    }
    void integrationPlatformService.getIntegrationsByCategory(emailCategoryId).then(setIntegrations)
  }, [emailCategoryId])

  useEffect(() => {
    if (!emailIntegrationId) {
      setConnectors([])
      setPipelines([])
      return
    }
    void integrationPlatformService.getConnectorsByIntegration(emailIntegrationId).then(setConnectors)
    void integrationPlatformService.getPipelinesByIntegration(emailIntegrationId).then(setPipelines)
  }, [emailIntegrationId])

  const submit = async (event: React.FormEvent) => {
    event.preventDefault()
    const result = await saveSettings(() =>
      agencySettingsService.update({
        agencyName: agencyName.trim(),
        tradeName: tradeName.trim() || null,
        document: document.trim() || null,
        primaryEmail: primaryEmail.trim() || null,
        phone: phone.trim() || null,
        address: address.trim() || null,
        logoUrl: logoUrl.trim() || null,
        primaryColor: primaryColor.trim() || null,
        defaultEmailConnectorId: emailConnectorId,
        defaultEmailPipelineId: emailPipelineId,
      }),
    )
    if (result !== null) void load()
  }

  return (
    <PageLayout
      title="Configurações da agência"
      subtitle="Identidade, contato e canal padrão de envio de e-mail"
      onRefresh={() => void load()}
      showDefaultActions={false}
    >
      <form onSubmit={submit} className="space-y-4">
        <Card>
          <CardContent className="pt-5 pb-5 space-y-4">
            <h3 className="text-sm font-semibold uppercase tracking-wide text-muted-foreground">Identidade</h3>
            <div className="grid grid-cols-1 gap-3 md:grid-cols-2">
              <div className="space-y-2">
                <label className="text-sm font-medium">Razão social *</label>
                <Input value={agencyName} onChange={(e) => setAgencyName(e.target.value)} required />
              </div>
              <div className="space-y-2">
                <label className="text-sm font-medium">Nome fantasia</label>
                <Input value={tradeName} onChange={(e) => setTradeName(e.target.value)} />
              </div>
              <div className="space-y-2">
                <label className="text-sm font-medium">CNPJ/CPF</label>
                <Input value={document} onChange={(e) => setDocument(e.target.value)} />
              </div>
              <div className="space-y-2">
                <label className="text-sm font-medium">Cor primária</label>
                <Input type="color" value={primaryColor} onChange={(e) => setPrimaryColor(e.target.value)} />
              </div>
              <div className="space-y-2 md:col-span-2">
                <label className="text-sm font-medium">URL do logo</label>
                <Input value={logoUrl} onChange={(e) => setLogoUrl(e.target.value)} placeholder="https://..." />
              </div>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="pt-5 pb-5 space-y-4">
            <h3 className="text-sm font-semibold uppercase tracking-wide text-muted-foreground">Contato</h3>
            <div className="grid grid-cols-1 gap-3 md:grid-cols-2">
              <div className="space-y-2">
                <label className="text-sm font-medium">E-mail principal</label>
                <Input type="email" value={primaryEmail} onChange={(e) => setPrimaryEmail(e.target.value)} />
              </div>
              <div className="space-y-2">
                <label className="text-sm font-medium">Telefone</label>
                <Input value={phone} onChange={(e) => setPhone(e.target.value)} />
              </div>
              <div className="space-y-2 md:col-span-2">
                <label className="text-sm font-medium">Endereço</label>
                <Input value={address} onChange={(e) => setAddress(e.target.value)} />
              </div>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="pt-5 pb-5 space-y-4">
            <h3 className="text-sm font-semibold uppercase tracking-wide text-muted-foreground">Canal padrão de envio de e-mail</h3>
            <p className="text-xs text-muted-foreground">
              Conector e pipeline a serem usados automaticamente em fluxos que enviam e-mail (templates de proposta, follow-up, etc.).
            </p>
            <div className="grid grid-cols-1 gap-3 md:grid-cols-2">
              <div className="space-y-2">
                <label className="text-sm font-medium">Categoria</label>
                <SearchableSelect
                  value={emailCategoryId ? String(emailCategoryId) : ''}
                  onValueChange={(value) => {
                    setEmailCategoryId(Number(value))
                    setEmailIntegrationId(null)
                    setEmailConnectorId(null)
                    setEmailPipelineId(null)
                  }}
                  options={categories.map((cat) => ({ value: String(cat.id), label: cat.name }))}
                  placeholder="Selecione"
                />
              </div>
              <div className="space-y-2">
                <label className="text-sm font-medium">Integração</label>
                <SearchableSelect
                  value={emailIntegrationId ? String(emailIntegrationId) : ''}
                  onValueChange={(value) => {
                    setEmailIntegrationId(Number(value))
                    setEmailConnectorId(null)
                    setEmailPipelineId(null)
                  }}
                  options={integrations.map((integ) => ({ value: String(integ.id), label: integ.name }))}
                  placeholder="Selecione"
                />
              </div>
              <div className="space-y-2">
                <label className="text-sm font-medium">Conector</label>
                <SearchableSelect
                  value={emailConnectorId ? String(emailConnectorId) : ''}
                  onValueChange={(value) => setEmailConnectorId(Number(value))}
                  options={connectors.filter((c) => c.isActive).map((c) => ({ value: String(c.id), label: c.name }))}
                  placeholder="Selecione"
                />
              </div>
              <div className="space-y-2">
                <label className="text-sm font-medium">Pipeline</label>
                <SearchableSelect
                  value={emailPipelineId ? String(emailPipelineId) : ''}
                  onValueChange={(value) => setEmailPipelineId(Number(value))}
                  options={pipelines.map((p) => ({ value: String(p.id), label: p.name }))}
                  placeholder="Selecione"
                />
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
