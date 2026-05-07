import { useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import {
  Modal,
  ModalContent,
  Button,
  Input,
  Badge,
  SearchableSelect,
  useApi,
} from 'archon-ui'
import {
  CheckCircle2,
  Circle,
  ArrowRight,
  ArrowLeft,
  SkipForward,
  Sparkles,
  Building2,
  Users,
  Megaphone,
  ListChecks,
  Wallet,
  Mail,
  Plug,
  Trophy,
  ExternalLink,
  Columns3,
  Tag,
  Globe,
  UserCheck,
  FileText,
} from 'lucide-react'
import {
  ONBOARDING_PHASES,
  ONBOARDING_STEPS,
  useOnboarding,
  type OnboardingPhase,
  type OnboardingStepId,
} from '../../hooks/useOnboarding'
import { agencySettingsService } from '../../services/agencySettingsService'
import { brandService } from '../../services/brandService'
import { creatorService } from '../../services/creatorService'
import { campaignService } from '../../services/campaignService'
import { commercialPipelineStageService } from '../../services/commercialPipelineStageService'
import { campaignCreatorStatusService } from '../../services/campaignCreatorStatusService'
import { platformService } from '../../services/platformService'
import { opportunityService } from '../../services/opportunityService'
import { opportunitySourceService } from '../../services/opportunitySourceService'
import { financialAccountService } from '../../services/financialAccountService'
import { emailTemplateService } from '../../services/emailTemplateService'
import { proposalTemplateService, type ProposalTemplate } from '../../services/proposalTemplateService'
import type { AgencySettings } from '../../types/agencySettings'
import type { Brand } from '../../types/brand'
import type { CommercialPipelineStage } from '../../types/commercialPipelineStage'
import type { CampaignCreatorStatus } from '../../types/campaignCreatorStatus'
import type { Platform } from '../../types/platform'
import type { OpportunitySource } from '../../types/opportunitySource'

interface OnboardingWizardProps {
  open: boolean
  onOpenChange: (open: boolean) => void
}

interface CreatedRefs {
  brandId?: number
  opportunitySourceId?: number
  creatorId?: number
  campaignId?: number
  financialAccountId?: number
}

const phaseIcons: Record<OnboardingPhase, React.ReactNode> = {
  inicio: <Sparkles size={14} />,
  comercial: <Columns3 size={14} />,
  operacao: <Megaphone size={14} />,
  financeiro: <Wallet size={14} />,
  comunicacao: <Mail size={14} />,
  fim: <Trophy size={14} />,
}

const stepIcons: Record<OnboardingStepId, React.ReactNode> = {
  welcome: <Sparkles size={20} />,
  'agency-identity': <Building2 size={20} />,
  'agency-contact': <Mail size={20} />,
  'pipeline-overview': <Columns3 size={20} />,
  'opportunity-source': <Sparkles size={20} />,
  'first-brand': <Building2 size={20} />,
  'first-opportunity': <ListChecks size={20} />,
  'proposal-template': <FileText size={20} />,
  platforms: <Globe size={20} />,
  'creator-statuses': <UserCheck size={20} />,
  'first-creator': <Users size={20} />,
  'first-campaign': <Megaphone size={20} />,
  'financial-account': <Wallet size={20} />,
  'financial-flow': <Tag size={20} />,
  'email-template': <Mail size={20} />,
  integrations: <Plug size={20} />,
  complete: <Trophy size={20} />,
}

export default function OnboardingWizard({ open, onOpenChange }: OnboardingWizardProps) {
  const navigate = useNavigate()
  const onboarding = useOnboarding()
  const [stepIndex, setStepIndex] = useState(0)
  const [createdRefs, setCreatedRefs] = useState<CreatedRefs>({})
  const [agency, setAgency] = useState<AgencySettings | null>(null)
  const [pipelineStages, setPipelineStages] = useState<CommercialPipelineStage[]>([])
  const [creatorStatuses, setCreatorStatuses] = useState<CampaignCreatorStatus[]>([])
  const [platforms, setPlatforms] = useState<Platform[]>([])
  const [sources, setSources] = useState<OpportunitySource[]>([])
  const [brands, setBrands] = useState<Brand[]>([])
  const [proposalTemplates, setProposalTemplates] = useState<ProposalTemplate[]>([])

  const { execute, loading } = useApi({ showSuccessMessage: false, showErrorMessage: true })

  const currentStep = ONBOARDING_STEPS[stepIndex]

  useEffect(() => {
    if (!open) return
    const lastIndex = ONBOARDING_STEPS.findIndex((s) => s.id === onboarding.state.lastStep)
    setStepIndex(lastIndex >= 0 ? lastIndex : 0)
    void Promise.all([
      agencySettingsService.get(),
      commercialPipelineStageService.getAll(),
      campaignCreatorStatusService.getAll(),
      platformService.getAll(),
      opportunitySourceService.getAll(false),
      brandService.getAll(),
      proposalTemplateService.getAll(),
    ]).then(([agencyResult, stages, statuses, plats, src, brandsResult, templates]) => {
      setAgency(agencyResult)
      setPipelineStages(stages)
      setCreatorStatuses(statuses)
      setPlatforms(plats)
      setSources(src)
      setBrands(brandsResult)
      setProposalTemplates(templates)
    })
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open])

  useEffect(() => {
    if (!open) return
    onboarding.setLastStep(currentStep.id)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [stepIndex, open])

  const goNext = () => {
    if (stepIndex < ONBOARDING_STEPS.length - 1) {
      setStepIndex(stepIndex + 1)
    }
  }
  const goBack = () => {
    if (stepIndex > 0) setStepIndex(stepIndex - 1)
  }
  const skipCurrent = () => {
    onboarding.markSkipped(currentStep.id)
    goNext()
  }
  const completeCurrent = () => {
    onboarding.markCompleted(currentStep.id)
    goNext()
  }

  const phaseProgress = useMemo(() => {
    return ONBOARDING_PHASES.map((phase) => {
      const stepsInPhase = ONBOARDING_STEPS.filter((s) => s.phase === phase.id)
      const completedInPhase = stepsInPhase.filter((s) => onboarding.isStepCompleted(s.id)).length
      return { ...phase, total: stepsInPhase.length, completed: completedInPhase }
    })
  }, [onboarding])

  return (
    <Modal open={open} onOpenChange={onOpenChange}>
      <ModalContent size="full" style={{ maxWidth: '1100px', width: '95vw', height: '85vh' }}>
        <div className="flex h-full flex-col">
          <div className="flex items-center justify-between border-b px-6 py-4">
            <div>
              <h2 className="text-base font-semibold">Configuração guiada do Kanvas</h2>
              <p className="text-xs text-muted-foreground">
                Passo {stepIndex + 1} de {ONBOARDING_STEPS.length} · {onboarding.completedCount} de {onboarding.totalSteps} concluídos ({onboarding.progress}%)
              </p>
            </div>
            <Button variant="ghost" size="sm" onClick={() => onOpenChange(false)}>
              Continuar depois
            </Button>
          </div>

          <div className="h-1 w-full bg-muted">
            <div
              className="h-full bg-primary transition-all"
              style={{ width: `${onboarding.progress}%` }}
            />
          </div>

          <div className="flex flex-1 overflow-hidden">
            <aside className="w-64 shrink-0 overflow-y-auto border-r bg-muted/20 p-3">
              {phaseProgress.map((phase) => {
                const stepsInPhase = ONBOARDING_STEPS.filter((s) => s.phase === phase.id)
                return (
                  <div key={phase.id} className="mb-4">
                    <div className="flex items-center gap-2 px-2 py-1 text-[10px] font-semibold uppercase tracking-wide text-muted-foreground">
                      {phaseIcons[phase.id as OnboardingPhase]}
                      {phase.label} ({phase.completed}/{phase.total})
                    </div>
                    <div className="space-y-1">
                      {stepsInPhase.map((step) => {
                        const isCurrent = step.id === currentStep.id
                        const isDone = onboarding.isStepCompleted(step.id)
                        const isSkipped = onboarding.isStepSkipped(step.id)
                        const idx = ONBOARDING_STEPS.findIndex((s) => s.id === step.id)
                        return (
                          <button
                            key={step.id}
                            type="button"
                            className={`flex w-full items-center gap-2 rounded-md px-2 py-1.5 text-left text-xs transition ${
                              isCurrent ? 'bg-primary/15 text-primary' : 'hover:bg-muted'
                            }`}
                            onClick={() => setStepIndex(idx)}
                          >
                            {isDone ? (
                              <CheckCircle2 size={12} className="text-emerald-500" />
                            ) : isSkipped ? (
                              <SkipForward size={12} className="text-muted-foreground" />
                            ) : (
                              <Circle size={12} className="text-muted-foreground" />
                            )}
                            <span className={isDone ? 'line-through opacity-60' : ''}>{step.title}</span>
                          </button>
                        )
                      })}
                    </div>
                  </div>
                )
              })}
            </aside>

            <main className="flex flex-1 flex-col overflow-y-auto p-6">
              <div className="mb-4 flex items-center gap-3">
                <span className="rounded-md bg-primary/15 p-2 text-primary">
                  {stepIcons[currentStep.id]}
                </span>
                <div>
                  <h3 className="text-lg font-semibold">{currentStep.title}</h3>
                  <p className="text-sm text-muted-foreground">{currentStep.summary}</p>
                </div>
              </div>

              <div className="flex-1">
                <StepBody
                  step={currentStep.id}
                  agency={agency}
                  setAgency={setAgency}
                  pipelineStages={pipelineStages}
                  creatorStatuses={creatorStatuses}
                  platforms={platforms}
                  sources={sources}
                  setSources={setSources}
                  brands={brands}
                  setBrands={setBrands}
                  proposalTemplates={proposalTemplates}
                  createdRefs={createdRefs}
                  setCreatedRefs={setCreatedRefs}
                  execute={execute}
                  loading={loading}
                  onComplete={completeCurrent}
                  navigate={navigate}
                  closeWizard={() => onOpenChange(false)}
                  onboarding={onboarding}
                />
              </div>

              <div className="mt-4 flex items-center justify-between border-t pt-4">
                <Button variant="outline" size="sm" onClick={goBack} disabled={stepIndex === 0}>
                  <ArrowLeft size={14} className="mr-1.5" />
                  Voltar
                </Button>
                <div className="flex items-center gap-2">
                  {currentStep.id !== 'complete' && currentStep.id !== 'welcome' && (
                    <Button variant="ghost" size="sm" onClick={skipCurrent}>
                      <SkipForward size={14} className="mr-1.5" />
                      Pular
                    </Button>
                  )}
                  {currentStep.id === 'complete' ? (
                    <Button size="sm" onClick={() => onOpenChange(false)}>Fechar</Button>
                  ) : currentStep.id === 'welcome' ? (
                    <Button size="sm" onClick={completeCurrent}>
                      Começar
                      <ArrowRight size={14} className="ml-1.5" />
                    </Button>
                  ) : (
                    <Button size="sm" onClick={completeCurrent}>
                      Marcar como concluído
                      <ArrowRight size={14} className="ml-1.5" />
                    </Button>
                  )}
                </div>
              </div>
            </main>
          </div>
        </div>
      </ModalContent>
    </Modal>
  )
}

interface StepBodyProps {
  step: OnboardingStepId
  agency: AgencySettings | null
  setAgency: (a: AgencySettings | null) => void
  pipelineStages: CommercialPipelineStage[]
  creatorStatuses: CampaignCreatorStatus[]
  platforms: Platform[]
  sources: OpportunitySource[]
  setSources: (s: OpportunitySource[]) => void
  brands: Brand[]
  setBrands: (b: Brand[]) => void
  proposalTemplates: ProposalTemplate[]
  createdRefs: CreatedRefs
  setCreatedRefs: React.Dispatch<React.SetStateAction<CreatedRefs>>
  execute: <T>(fn: () => Promise<T>) => Promise<T | null>
  loading: boolean
  onComplete: () => void
  navigate: (path: string) => void
  closeWizard: () => void
  onboarding: ReturnType<typeof useOnboarding>
}

function StepBody(props: StepBodyProps) {
  switch (props.step) {
    case 'welcome':
      return <WelcomeStep />
    case 'agency-identity':
      return <AgencyIdentityStep {...props} />
    case 'agency-contact':
      return <AgencyContactStep {...props} />
    case 'pipeline-overview':
      return <PipelineOverviewStep {...props} />
    case 'opportunity-source':
      return <OpportunitySourceStep {...props} />
    case 'first-brand':
      return <FirstBrandStep {...props} />
    case 'first-opportunity':
      return <FirstOpportunityStep {...props} />
    case 'proposal-template':
      return <ProposalTemplateStep {...props} />
    case 'platforms':
      return <PlatformsStep {...props} />
    case 'creator-statuses':
      return <CreatorStatusesStep {...props} />
    case 'first-creator':
      return <FirstCreatorStep {...props} />
    case 'first-campaign':
      return <FirstCampaignStep {...props} />
    case 'financial-account':
      return <FinancialAccountStep {...props} />
    case 'financial-flow':
      return <FinancialFlowStep />
    case 'email-template':
      return <EmailTemplateStep {...props} />
    case 'integrations':
      return <IntegrationsStep {...props} />
    case 'complete':
      return <CompleteStep {...props} />
    default:
      return null
  }
}

function WelcomeStep() {
  return (
    <div className="space-y-3">
      <p className="text-sm">
        Bem-vindo ao <strong>Kanvas</strong>! Este onboarding vai guiar você por todas as áreas do sistema —
        Comercial, Operação, Financeiro e Comunicação. São 17 passos curtos.
      </p>
      <p className="text-sm">
        Não é obrigatório. Você pode pular passos e voltar quando quiser. O progresso fica salvo no seu navegador.
      </p>
      <p className="text-sm text-muted-foreground">
        Em cada passo você pode preencher os dados ali mesmo ou apenas marcar como concluído se já cadastrou pela tela específica.
      </p>
    </div>
  )
}

function AgencyIdentityStep({ agency, setAgency, execute, onComplete }: StepBodyProps) {
  const [name, setName] = useState(agency?.agencyName ?? '')
  const [tradeName, setTradeName] = useState(agency?.tradeName ?? '')
  const [logoUrl, setLogoUrl] = useState(agency?.logoUrl ?? '')

  useEffect(() => {
    setName(agency?.agencyName ?? '')
    setTradeName(agency?.tradeName ?? '')
    setLogoUrl(agency?.logoUrl ?? '')
  }, [agency])

  const save = async () => {
    if (!agency) return
    const result = await execute(() =>
      agencySettingsService.update({
        agencyName: name.trim() || agency.agencyName,
        tradeName: tradeName.trim() || null,
        document: agency.document ?? null,
        primaryEmail: agency.primaryEmail ?? null,
        phone: agency.phone ?? null,
        address: agency.address ?? null,
        logoUrl: logoUrl.trim() || null,
        primaryColor: agency.primaryColor ?? null,
        defaultEmailConnectorId: agency.defaultEmailConnectorId ?? null,
        defaultEmailPipelineId: agency.defaultEmailPipelineId ?? null,
      }),
    )
    if (result?.data) {
      setAgency(result.data)
      onComplete()
    }
  }

  return (
    <div className="space-y-3">
      <p className="text-sm text-muted-foreground">
        Estes dados aparecem em propostas, e-mails e na página de configuração da empresa.
      </p>
      <div className="space-y-2">
        <label className="text-sm font-medium">Razão social *</label>
        <Input value={name} onChange={(e) => setName(e.target.value)} />
      </div>
      <div className="space-y-2">
        <label className="text-sm font-medium">Nome fantasia</label>
        <Input value={tradeName} onChange={(e) => setTradeName(e.target.value)} />
      </div>
      <div className="space-y-2">
        <label className="text-sm font-medium">URL do logo</label>
        <Input value={logoUrl} onChange={(e) => setLogoUrl(e.target.value)} placeholder="https://..." />
      </div>
      <Button size="sm" onClick={save} disabled={!name.trim()}>Salvar identidade</Button>
    </div>
  )
}

function AgencyContactStep({ agency, setAgency, execute, onComplete }: StepBodyProps) {
  const [primaryEmail, setPrimaryEmail] = useState(agency?.primaryEmail ?? '')
  const [phone, setPhone] = useState(agency?.phone ?? '')
  const [address, setAddress] = useState(agency?.address ?? '')

  useEffect(() => {
    setPrimaryEmail(agency?.primaryEmail ?? '')
    setPhone(agency?.phone ?? '')
    setAddress(agency?.address ?? '')
  }, [agency])

  const save = async () => {
    if (!agency) return
    const result = await execute(() =>
      agencySettingsService.update({
        agencyName: agency.agencyName,
        tradeName: agency.tradeName ?? null,
        document: agency.document ?? null,
        primaryEmail: primaryEmail.trim() || null,
        phone: phone.trim() || null,
        address: address.trim() || null,
        logoUrl: agency.logoUrl ?? null,
        primaryColor: agency.primaryColor ?? null,
        defaultEmailConnectorId: agency.defaultEmailConnectorId ?? null,
        defaultEmailPipelineId: agency.defaultEmailPipelineId ?? null,
      }),
    )
    if (result?.data) {
      setAgency(result.data)
      onComplete()
    }
  }

  return (
    <div className="space-y-3">
      <p className="text-sm text-muted-foreground">Como a marca/creator pode falar com você fora do sistema.</p>
      <div className="space-y-2">
        <label className="text-sm font-medium">E-mail principal</label>
        <Input type="email" value={primaryEmail} onChange={(e) => setPrimaryEmail(e.target.value)} />
      </div>
      <div className="space-y-2">
        <label className="text-sm font-medium">Telefone</label>
        <Input value={phone} onChange={(e) => setPhone(e.target.value)} />
      </div>
      <div className="space-y-2">
        <label className="text-sm font-medium">Endereço</label>
        <Input value={address} onChange={(e) => setAddress(e.target.value)} />
      </div>
      <Button size="sm" onClick={save}>Salvar contato</Button>
    </div>
  )
}

function PipelineOverviewStep({ pipelineStages, navigate, closeWizard }: StepBodyProps) {
  return (
    <div className="space-y-3">
      <p className="text-sm">
        Seu pipeline comercial determina os estágios pelos quais uma oportunidade passa até virar campanha.
      </p>
      <div className="rounded-md border bg-muted/40 p-3">
        <p className="text-xs text-muted-foreground mb-2">Estágios atuais ({pipelineStages.length}):</p>
        <div className="flex flex-wrap gap-2">
          {pipelineStages.map((stage) => (
            <Badge key={stage.id} variant="outline" style={{ backgroundColor: `${stage.color}20`, color: stage.color, borderColor: stage.color }}>
              {stage.name}
            </Badge>
          ))}
        </div>
      </div>
      <Button
        size="sm"
        variant="outline"
        onClick={() => {
          closeWizard()
          navigate('/configuracao/pipeline-comercial')
        }}
      >
        <ExternalLink size={14} className="mr-1.5" />
        Editar estágios
      </Button>
    </div>
  )
}

function OpportunitySourceStep({ sources, setSources, execute, onComplete }: StepBodyProps) {
  const [name, setName] = useState('')
  const [color, setColor] = useState('#6366f1')

  const save = async () => {
    const result = await execute(() =>
      opportunitySourceService.create({ name: name.trim(), color, displayOrder: sources.length + 1 }),
    )
    if (result?.data) {
      setSources([...sources, result.data])
      setName('')
      onComplete()
    }
  }

  return (
    <div className="space-y-3">
      <p className="text-sm text-muted-foreground">
        Origens classificam de onde vêm seus leads (Inbound, Indicação, Evento...). Já existem {sources.length} cadastradas.
      </p>
      {sources.length > 0 && (
        <div className="flex flex-wrap gap-2">
          {sources.map((s) => (
            <Badge key={s.id} variant="outline" style={{ borderColor: s.color, color: s.color }}>{s.name}</Badge>
          ))}
        </div>
      )}
      <div className="rounded-md border bg-muted/30 p-3 space-y-2">
        <p className="text-xs font-semibold">Adicionar nova origem</p>
        <div className="grid grid-cols-1 gap-2 md:grid-cols-[1fr_120px]">
          <Input value={name} onChange={(e) => setName(e.target.value)} placeholder="Ex.: Indicação Patrocinador" />
          <Input type="color" value={color} onChange={(e) => setColor(e.target.value)} />
        </div>
        <Button size="sm" onClick={save} disabled={name.trim().length < 2}>Cadastrar origem</Button>
      </div>
    </div>
  )
}

function FirstBrandStep({ brands, setBrands, setCreatedRefs, execute, onComplete }: StepBodyProps) {
  const [name, setName] = useState('')
  const [contactName, setContactName] = useState('')
  const [contactEmail, setContactEmail] = useState('')

  const save = async () => {
    const result = await execute(() =>
      brandService.create({
        name: name.trim(),
        contactName: contactName.trim() || undefined,
        contactEmail: contactEmail.trim() || undefined,
      }),
    )
    if (result?.data) {
      setBrands([...brands, result.data])
      setCreatedRefs((prev) => ({ ...prev, brandId: result.data!.id }))
      onComplete()
    }
  }

  return (
    <div className="space-y-3">
      <p className="text-sm text-muted-foreground">
        Marca = cliente final que paga pelas campanhas. Cadastre uma marca real para usar nos próximos passos.
        {brands.length > 0 && ` Você já tem ${brands.length} marca(s) cadastrada(s).`}
      </p>
      <div className="space-y-2">
        <label className="text-sm font-medium">Nome da marca *</label>
        <Input value={name} onChange={(e) => setName(e.target.value)} placeholder="Ex.: Acme Bebidas" />
      </div>
      <div className="grid grid-cols-1 gap-2 md:grid-cols-2">
        <div className="space-y-2">
          <label className="text-sm font-medium">Contato</label>
          <Input value={contactName} onChange={(e) => setContactName(e.target.value)} />
        </div>
        <div className="space-y-2">
          <label className="text-sm font-medium">E-mail</label>
          <Input type="email" value={contactEmail} onChange={(e) => setContactEmail(e.target.value)} />
        </div>
      </div>
      <Button size="sm" onClick={save} disabled={name.trim().length < 2}>Cadastrar marca</Button>
    </div>
  )
}

function FirstOpportunityStep({ brands, sources, createdRefs, setCreatedRefs, execute, onComplete }: StepBodyProps) {
  const [name, setName] = useState('')
  const [brandId, setBrandId] = useState<number>(createdRefs.brandId ?? brands[0]?.id ?? 0)
  const [sourceId, setSourceId] = useState<number | undefined>(sources[0]?.id)
  const [estimatedValue, setEstimatedValue] = useState<string>('')

  useEffect(() => {
    if (createdRefs.brandId) setBrandId(createdRefs.brandId)
    else if (brands.length > 0 && brandId === 0) setBrandId(brands[0].id)
  }, [createdRefs.brandId, brands, brandId])

  const save = async () => {
    if (!brandId) return
    const result = await execute(() =>
      opportunityService.create({
        brandId,
        name: name.trim(),
        estimatedValue: estimatedValue ? Number(estimatedValue) : 0,
        opportunitySourceId: sourceId,
        tagIds: [],
      }),
    )
    if (result?.data) {
      setCreatedRefs((prev) => ({ ...prev, opportunityId: result.data!.id }))
      onComplete()
    }
  }

  if (brands.length === 0) {
    return (
      <p className="text-sm text-muted-foreground">
        Você precisa de pelo menos uma marca para criar uma oportunidade. Volte ao passo "Primeira marca" ou pule este.
      </p>
    )
  }

  return (
    <div className="space-y-3">
      <p className="text-sm text-muted-foreground">
        Oportunidade = uma negociação em andamento. Aparece no Pipeline e em "Oportunidades".
      </p>
      <div className="space-y-2">
        <label className="text-sm font-medium">Nome da oportunidade *</label>
        <Input value={name} onChange={(e) => setName(e.target.value)} placeholder="Ex.: Campanha Verão 2026" />
      </div>
      <div className="grid grid-cols-1 gap-2 md:grid-cols-2">
        <div className="space-y-2">
          <label className="text-sm font-medium">Marca *</label>
          <SearchableSelect
            value={brandId ? String(brandId) : ''}
            onValueChange={(v) => setBrandId(Number(v))}
            options={brands.map((b) => ({ value: String(b.id), label: b.name }))}
          />
        </div>
        <div className="space-y-2">
          <label className="text-sm font-medium">Valor estimado</label>
          <Input type="number" value={estimatedValue} onChange={(e) => setEstimatedValue(e.target.value)} />
        </div>
        {sources.length > 0 && (
          <div className="space-y-2 md:col-span-2">
            <label className="text-sm font-medium">Origem</label>
            <SearchableSelect
              value={sourceId ? String(sourceId) : ''}
              onValueChange={(v) => setSourceId(v ? Number(v) : undefined)}
              options={[{ value: '', label: 'Sem origem' }, ...sources.map((s) => ({ value: String(s.id), label: s.name }))]}
            />
          </div>
        )}
      </div>
      <Button size="sm" onClick={save} disabled={!brandId || name.trim().length < 2}>Cadastrar oportunidade</Button>
    </div>
  )
}

function ProposalTemplateStep({ proposalTemplates, navigate, closeWizard }: StepBodyProps) {
  return (
    <div className="space-y-3">
      <p className="text-sm">
        Templates aceleram a criação de propostas com blocos pré-definidos (escopo, valor, validade, observações).
      </p>
      <p className="text-sm text-muted-foreground">
        Você tem <strong>{proposalTemplates.length}</strong> template(s) cadastrado(s). Esta etapa é opcional.
      </p>
      <Button
        size="sm"
        variant="outline"
        onClick={() => {
          closeWizard()
          navigate('/configuracao/templates-proposta')
        }}
      >
        <ExternalLink size={14} className="mr-1.5" />
        Criar template de proposta
      </Button>
    </div>
  )
}

function PlatformsStep({ platforms, navigate, closeWizard }: StepBodyProps) {
  const active = platforms.filter((p) => p.isActive)
  return (
    <div className="space-y-3">
      <p className="text-sm">
        Plataformas onde os creators publicam (Instagram, TikTok, YouTube, etc.). Usadas em handles de creator e em entregas de campanha.
      </p>
      <div className="rounded-md border bg-muted/40 p-3">
        <p className="text-xs text-muted-foreground mb-2">Plataformas ativas ({active.length}):</p>
        <div className="flex flex-wrap gap-2">
          {active.map((p) => (
            <Badge key={p.id} variant="outline">{p.name}</Badge>
          ))}
        </div>
      </div>
      <Button
        size="sm"
        variant="outline"
        onClick={() => {
          closeWizard()
          navigate('/configuracao/plataformas')
        }}
      >
        <ExternalLink size={14} className="mr-1.5" />
        Gerenciar plataformas
      </Button>
    </div>
  )
}

function CreatorStatusesStep({ creatorStatuses, navigate, closeWizard }: StepBodyProps) {
  return (
    <div className="space-y-3">
      <p className="text-sm">
        Status mostram em que ponto cada creator está dentro de uma campanha (Convidado, Confirmado, Em execução...).
      </p>
      <div className="rounded-md border bg-muted/40 p-3">
        <p className="text-xs text-muted-foreground mb-2">Status configurados ({creatorStatuses.length}):</p>
        <div className="flex flex-wrap gap-2">
          {creatorStatuses.map((s) => (
            <Badge key={s.id} variant="outline" style={{ borderColor: s.color, color: s.color }}>{s.name}</Badge>
          ))}
        </div>
      </div>
      <Button
        size="sm"
        variant="outline"
        onClick={() => {
          closeWizard()
          navigate('/configuracao/status-creators')
        }}
      >
        <ExternalLink size={14} className="mr-1.5" />
        Editar status
      </Button>
    </div>
  )
}

function FirstCreatorStep({ setCreatedRefs, execute, onComplete }: StepBodyProps) {
  const [name, setName] = useState('')
  const [stageName, setStageName] = useState('')
  const [primaryNiche, setPrimaryNiche] = useState('')

  const save = async () => {
    const result = await execute(() =>
      creatorService.create({
        name: name.trim(),
        stageName: stageName.trim() || undefined,
        primaryNiche: primaryNiche.trim() || undefined,
        defaultAgencyFeePercent: 20,
      }),
    )
    if (result?.data) {
      setCreatedRefs((prev) => ({ ...prev, creatorId: result.data!.id }))
      onComplete()
    }
  }

  return (
    <div className="space-y-3">
      <p className="text-sm text-muted-foreground">
        Creator = influenciador da sua base. Você poderá depois adicionar handles sociais e métricas.
      </p>
      <div className="grid grid-cols-1 gap-2 md:grid-cols-2">
        <div className="space-y-2">
          <label className="text-sm font-medium">Nome real *</label>
          <Input value={name} onChange={(e) => setName(e.target.value)} />
        </div>
        <div className="space-y-2">
          <label className="text-sm font-medium">Nome artístico</label>
          <Input value={stageName} onChange={(e) => setStageName(e.target.value)} />
        </div>
        <div className="space-y-2 md:col-span-2">
          <label className="text-sm font-medium">Nicho principal</label>
          <Input value={primaryNiche} onChange={(e) => setPrimaryNiche(e.target.value)} placeholder="Lifestyle, gastronomia, fitness..." />
        </div>
      </div>
      <Button size="sm" onClick={save} disabled={name.trim().length < 2}>Cadastrar creator</Button>
    </div>
  )
}

function FirstCampaignStep({ brands, createdRefs, setCreatedRefs, execute, onComplete }: StepBodyProps) {
  const [name, setName] = useState('')
  const [brandId, setBrandId] = useState<number>(createdRefs.brandId ?? brands[0]?.id ?? 0)
  const [budget, setBudget] = useState<string>('')

  useEffect(() => {
    if (createdRefs.brandId) setBrandId(createdRefs.brandId)
    else if (brands.length > 0 && brandId === 0) setBrandId(brands[0].id)
  }, [createdRefs.brandId, brands, brandId])

  const save = async () => {
    if (!brandId) return
    const result = await execute(() =>
      campaignService.create({
        brandId,
        name: name.trim(),
        budget: budget ? Number(budget) : 0,
        startsAt: new Date().toISOString(),
        status: 1,
      }),
    )
    if (result?.data) {
      setCreatedRefs((prev) => ({ ...prev, campaignId: result.data!.id }))
      onComplete()
    }
  }

  if (brands.length === 0) {
    return <p className="text-sm text-muted-foreground">Cadastre uma marca primeiro.</p>
  }

  return (
    <div className="space-y-3">
      <p className="text-sm text-muted-foreground">
        Campanha = execução real com creators e marcas. Hospedará entregas, documentos e lançamentos financeiros.
      </p>
      <div className="space-y-2">
        <label className="text-sm font-medium">Nome da campanha *</label>
        <Input value={name} onChange={(e) => setName(e.target.value)} />
      </div>
      <div className="grid grid-cols-1 gap-2 md:grid-cols-2">
        <div className="space-y-2">
          <label className="text-sm font-medium">Marca</label>
          <SearchableSelect
            value={brandId ? String(brandId) : ''}
            onValueChange={(v) => setBrandId(Number(v))}
            options={brands.map((b) => ({ value: String(b.id), label: b.name }))}
          />
        </div>
        <div className="space-y-2">
          <label className="text-sm font-medium">Budget</label>
          <Input type="number" value={budget} onChange={(e) => setBudget(e.target.value)} />
        </div>
      </div>
      <Button size="sm" onClick={save} disabled={!brandId || name.trim().length < 2}>Cadastrar campanha</Button>
    </div>
  )
}

function FinancialAccountStep({ setCreatedRefs, execute, onComplete }: StepBodyProps) {
  const [name, setName] = useState('')
  const [type, setType] = useState<number>(1)
  const [bank, setBank] = useState('')
  const [initialBalance, setInitialBalance] = useState('0')

  const save = async () => {
    const result = await execute(() =>
      financialAccountService.create({
        name: name.trim(),
        type,
        bank: bank.trim() || undefined,
        initialBalance: Number(initialBalance) || 0,
        color: '#6366f1',
      }),
    )
    if (result?.data) {
      setCreatedRefs((prev) => ({ ...prev, financialAccountId: result.data!.id }))
      onComplete()
    }
  }

  return (
    <div className="space-y-3">
      <p className="text-sm text-muted-foreground">
        Toda movimentação financeira vai para uma conta. Cadastre a principal — você pode adicionar outras depois.
      </p>
      <div className="grid grid-cols-1 gap-2 md:grid-cols-2">
        <div className="space-y-2 md:col-span-2">
          <label className="text-sm font-medium">Nome *</label>
          <Input value={name} onChange={(e) => setName(e.target.value)} placeholder="Ex.: PJ Itaú" />
        </div>
        <div className="space-y-2">
          <label className="text-sm font-medium">Tipo</label>
          <SearchableSelect
            value={String(type)}
            onValueChange={(v) => setType(Number(v))}
            options={[
              { value: '1', label: 'Banco' },
              { value: '2', label: 'Caixa' },
              { value: '3', label: 'Carteira/Pix' },
              { value: '4', label: 'Cartão de crédito' },
            ]}
          />
        </div>
        <div className="space-y-2">
          <label className="text-sm font-medium">Banco</label>
          <Input value={bank} onChange={(e) => setBank(e.target.value)} />
        </div>
        <div className="space-y-2 md:col-span-2">
          <label className="text-sm font-medium">Saldo inicial</label>
          <Input type="number" step="0.01" value={initialBalance} onChange={(e) => setInitialBalance(e.target.value)} />
        </div>
      </div>
      <Button size="sm" onClick={save} disabled={name.trim().length < 2}>Cadastrar conta</Button>
    </div>
  )
}

function FinancialFlowStep() {
  return (
    <div className="space-y-3">
      <p className="text-sm">O Kanvas gera lançamentos financeiros automaticamente em dois momentos:</p>
      <ul className="space-y-2 text-sm">
        <li className="flex items-start gap-2 rounded-md border bg-muted/30 p-3">
          <span className="rounded-md bg-emerald-100 p-1 text-emerald-600 dark:bg-emerald-900/30"><CheckCircle2 size={14} /></span>
          <div>
            <p className="font-medium">Conversão de proposta em campanha</p>
            <p className="text-xs text-muted-foreground">Cria automaticamente um lançamento de <strong>conta a receber</strong> da marca pelo valor total da proposta.</p>
          </div>
        </li>
        <li className="flex items-start gap-2 rounded-md border bg-muted/30 p-3">
          <span className="rounded-md bg-emerald-100 p-1 text-emerald-600 dark:bg-emerald-900/30"><CheckCircle2 size={14} /></span>
          <div>
            <p className="font-medium">Publicação de entrega</p>
            <p className="text-xs text-muted-foreground">Gera <strong>conta a pagar</strong> ao creator com base no valor combinado da entrega.</p>
          </div>
        </li>
      </ul>
      <p className="text-sm text-muted-foreground">
        Você também pode lançar manualmente despesas operacionais (aluguel, software, salário) sem vincular a campanha.
      </p>
    </div>
  )
}

function EmailTemplateStep({ execute, onComplete, navigate, closeWizard }: StepBodyProps) {
  const [name, setName] = useState('')
  const [subject, setSubject] = useState('Sua proposta {{ proposalName }} chegou')
  const [body, setBody] = useState('<p>Olá {{ contactName }},</p>\n<p>Segue a proposta {{ proposalName }} no valor de R$ {{ totalValue }}.</p>')

  const save = async () => {
    const result = await execute(() =>
      emailTemplateService.create({
        name: name.trim() || 'Template inicial',
        eventType: 1,
        subject: subject.trim(),
        htmlBody: body,
      }),
    )
    if (result?.data) onComplete()
  }

  return (
    <div className="space-y-3">
      <p className="text-sm text-muted-foreground">
        Templates de e-mail disparam automaticamente em eventos como "proposta enviada" ou "follow-up vencido".
        O envio real exige uma Automação configurada com pipeline SMTP do IntegrationPlatform.
      </p>
      <div className="space-y-2">
        <label className="text-sm font-medium">Nome do template</label>
        <Input value={name} onChange={(e) => setName(e.target.value)} placeholder="Ex.: Confirmação de envio de proposta" />
      </div>
      <div className="space-y-2">
        <label className="text-sm font-medium">Assunto</label>
        <Input value={subject} onChange={(e) => setSubject(e.target.value)} />
      </div>
      <div className="space-y-2">
        <label className="text-sm font-medium">Corpo (HTML)</label>
        <textarea className="min-h-[140px] w-full rounded-md border bg-background p-2 font-mono text-xs" value={body} onChange={(e) => setBody(e.target.value)} />
      </div>
      <div className="flex flex-wrap gap-2">
        <Button size="sm" onClick={save}>Cadastrar template</Button>
        <Button size="sm" variant="outline" onClick={() => { closeWizard(); navigate('/configuracao/templates-email') }}>
          <ExternalLink size={14} className="mr-1.5" />
          Ver todos os templates
        </Button>
      </div>
    </div>
  )
}

function IntegrationsStep({ navigate, closeWizard }: StepBodyProps) {
  return (
    <div className="space-y-3">
      <p className="text-sm">
        Integrações conectam o Kanvas a serviços externos (SMTP, banco, ERP). Automações disparam pipelines do IntegrationPlatform em eventos do sistema.
      </p>
      <div className="rounded-md border bg-muted/30 p-3 space-y-2 text-xs">
        <p><strong>Eventos disponíveis hoje:</strong></p>
        <ul className="space-y-1 ml-4 list-disc">
          <li>Proposta enviada / aprovada / rejeitada / convertida</li>
          <li>Entrega aprovada pela marca / publicada</li>
          <li>Conta a receber / pagar criada / paga</li>
          <li>Follow-up atrasado</li>
        </ul>
      </div>
      <Button
        size="sm"
        variant="outline"
        onClick={() => { closeWizard(); navigate('/configuracao/integracoes') }}
      >
        <ExternalLink size={14} className="mr-1.5" />
        Configurar integrações
      </Button>
    </div>
  )
}

function CompleteStep({ onboarding, createdRefs, navigate, closeWizard }: StepBodyProps) {
  return (
    <div className="space-y-4">
      <div className="rounded-md border border-emerald-200 bg-emerald-50 p-4 text-sm dark:border-emerald-800 dark:bg-emerald-950/30">
        <p className="font-semibold text-emerald-900 dark:text-emerald-200">Configuração concluída.</p>
        <p className="mt-1 text-emerald-800 dark:text-emerald-300">
          Você completou {onboarding.completedCount} de {onboarding.totalSteps} passos ({onboarding.progress}%).
        </p>
      </div>

      <div className="space-y-2">
        <p className="text-sm font-medium">O que vem agora</p>
        <div className="grid grid-cols-1 gap-2 md:grid-cols-2">
          <button type="button" className="flex items-center gap-2 rounded-md border bg-background px-3 py-2 text-left text-sm hover:border-primary/40" onClick={() => { closeWizard(); navigate('/comercial/pipeline') }}>
            <Columns3 size={14} className="text-primary" /> Ir para o Pipeline
          </button>
          <button type="button" className="flex items-center gap-2 rounded-md border bg-background px-3 py-2 text-left text-sm hover:border-primary/40" onClick={() => { closeWizard(); navigate('/campanhas') }}>
            <Megaphone size={14} className="text-primary" /> Ver minhas campanhas
          </button>
          <button type="button" className="flex items-center gap-2 rounded-md border bg-background px-3 py-2 text-left text-sm hover:border-primary/40" onClick={() => { closeWizard(); navigate('/financeiro/fluxo-caixa') }}>
            <Wallet size={14} className="text-primary" /> Ver fluxo de caixa
          </button>
          <button type="button" className="flex items-center gap-2 rounded-md border bg-background px-3 py-2 text-left text-sm hover:border-primary/40" onClick={() => { closeWizard(); navigate('/configuracao') }}>
            <Plug size={14} className="text-primary" /> Configurar integrações
          </button>
        </div>
      </div>

      {createdRefs.brandId || createdRefs.creatorId || createdRefs.campaignId || createdRefs.financialAccountId ? (
        <div className="rounded-md border bg-muted/30 p-3 text-xs">
          <p className="font-medium">Você cadastrou nesta sessão:</p>
          <ul className="mt-1 space-y-1 ml-4 list-disc text-muted-foreground">
            {createdRefs.brandId && <li>1 marca</li>}
            {createdRefs.opportunitySourceId && <li>1 origem de oportunidade</li>}
            {createdRefs.creatorId && <li>1 creator</li>}
            {createdRefs.campaignId && <li>1 campanha</li>}
            {createdRefs.financialAccountId && <li>1 conta financeira</li>}
          </ul>
        </div>
      ) : null}

      <p className="text-xs text-muted-foreground">
        Você pode reabrir este onboarding a qualquer momento pelo botão na Dashboard.
      </p>
    </div>
  )
}
