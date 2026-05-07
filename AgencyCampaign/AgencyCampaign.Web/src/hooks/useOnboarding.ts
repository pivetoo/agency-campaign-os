import { useCallback, useEffect, useState } from 'react'

export const ONBOARDING_STEPS = [
  { id: 'welcome', phase: 'inicio', title: 'Boas-vindas', summary: 'Conheça o Kanvas' },
  { id: 'agency-identity', phase: 'inicio', title: 'Identidade da agência', summary: 'Nome e logo da empresa' },
  { id: 'agency-contact', phase: 'inicio', title: 'Contato da agência', summary: 'E-mail e telefone principais' },
  { id: 'pipeline-overview', phase: 'comercial', title: 'Pipeline comercial', summary: 'Conheça o funil padrão' },
  { id: 'opportunity-source', phase: 'comercial', title: 'Origens de leads', summary: 'De onde vêm seus leads' },
  { id: 'first-brand', phase: 'comercial', title: 'Primeira marca', summary: 'Cliente que você atende' },
  { id: 'first-opportunity', phase: 'comercial', title: 'Primeira oportunidade', summary: 'Negociação em andamento' },
  { id: 'proposal-template', phase: 'comercial', title: 'Template de proposta', summary: 'Modelo reutilizável (opcional)' },
  { id: 'platforms', phase: 'operacao', title: 'Redes sociais', summary: 'Onde os creators publicam' },
  { id: 'creator-statuses', phase: 'operacao', title: 'Status do creator', summary: 'Convidado, confirmado, etc.' },
  { id: 'first-creator', phase: 'operacao', title: 'Primeiro creator', summary: 'Influenciador da sua base' },
  { id: 'first-campaign', phase: 'operacao', title: 'Primeira campanha', summary: 'Execução com creators e marcas' },
  { id: 'financial-account', phase: 'financeiro', title: 'Conta bancária', summary: 'Onde o dinheiro entra e sai' },
  { id: 'financial-flow', phase: 'financeiro', title: 'Lançamentos automáticos', summary: 'Como o sistema gera receivables' },
  { id: 'email-template', phase: 'comunicacao', title: 'Template de e-mail', summary: 'Mensagens automáticas' },
  { id: 'integrations', phase: 'comunicacao', title: 'Integrações e automações', summary: 'Conecte com ferramentas externas' },
  { id: 'complete', phase: 'fim', title: 'Pronto', summary: 'Resumo do que você configurou' },
] as const

export type OnboardingStepId = (typeof ONBOARDING_STEPS)[number]['id']
export type OnboardingPhase = (typeof ONBOARDING_STEPS)[number]['phase']

export const ONBOARDING_PHASES: { id: OnboardingPhase; label: string }[] = [
  { id: 'inicio', label: 'Início' },
  { id: 'comercial', label: 'Comercial' },
  { id: 'operacao', label: 'Operação' },
  { id: 'financeiro', label: 'Financeiro' },
  { id: 'comunicacao', label: 'Comunicação' },
  { id: 'fim', label: 'Conclusão' },
]

const STORAGE_KEY = 'kanvas_onboarding_state_v1'

interface OnboardingState {
  completedSteps: OnboardingStepId[]
  skippedSteps: OnboardingStepId[]
  lastStep: OnboardingStepId
}

function loadState(): OnboardingState {
  if (typeof window === 'undefined') {
    return { completedSteps: [], skippedSteps: [], lastStep: 'welcome' }
  }
  try {
    const raw = window.localStorage.getItem(STORAGE_KEY)
    if (!raw) return { completedSteps: [], skippedSteps: [], lastStep: 'welcome' }
    const parsed = JSON.parse(raw)
    return {
      completedSteps: Array.isArray(parsed.completedSteps) ? parsed.completedSteps : [],
      skippedSteps: Array.isArray(parsed.skippedSteps) ? parsed.skippedSteps : [],
      lastStep: typeof parsed.lastStep === 'string' ? parsed.lastStep : 'welcome',
    }
  } catch {
    return { completedSteps: [], skippedSteps: [], lastStep: 'welcome' }
  }
}

function saveState(state: OnboardingState) {
  if (typeof window === 'undefined') return
  try {
    window.localStorage.setItem(STORAGE_KEY, JSON.stringify(state))
  } catch {
    // ignore quota errors
  }
}

export function useOnboarding() {
  const [state, setState] = useState<OnboardingState>(() => loadState())

  useEffect(() => {
    saveState(state)
  }, [state])

  const markCompleted = useCallback((stepId: OnboardingStepId) => {
    setState((prev) => {
      const completedSet = new Set(prev.completedSteps)
      completedSet.add(stepId)
      const skippedSet = new Set(prev.skippedSteps)
      skippedSet.delete(stepId)
      return {
        ...prev,
        completedSteps: Array.from(completedSet),
        skippedSteps: Array.from(skippedSet),
      }
    })
  }, [])

  const markSkipped = useCallback((stepId: OnboardingStepId) => {
    setState((prev) => {
      const skippedSet = new Set(prev.skippedSteps)
      skippedSet.add(stepId)
      return {
        ...prev,
        skippedSteps: Array.from(skippedSet),
      }
    })
  }, [])

  const setLastStep = useCallback((stepId: OnboardingStepId) => {
    setState((prev) => ({ ...prev, lastStep: stepId }))
  }, [])

  const reset = useCallback(() => {
    setState({ completedSteps: [], skippedSteps: [], lastStep: 'welcome' })
  }, [])

  const completedCount = state.completedSteps.length
  const totalSteps = ONBOARDING_STEPS.length
  const isCompleted = completedCount === totalSteps
  const progress = Math.round((completedCount / totalSteps) * 100)

  const isStepCompleted = useCallback(
    (stepId: OnboardingStepId) => state.completedSteps.includes(stepId),
    [state.completedSteps],
  )

  const isStepSkipped = useCallback(
    (stepId: OnboardingStepId) => state.skippedSteps.includes(stepId),
    [state.skippedSteps],
  )

  return {
    state,
    completedCount,
    totalSteps,
    isCompleted,
    progress,
    markCompleted,
    markSkipped,
    setLastStep,
    reset,
    isStepCompleted,
    isStepSkipped,
  }
}
