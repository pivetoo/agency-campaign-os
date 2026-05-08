import { useEffect, useState } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import Joyride, { ACTIONS, EVENTS, STATUS } from 'react-joyride'
import type { CallBackProps, Step } from 'react-joyride'
import { useProductTour } from '../../hooks/useProductTour'

interface KanvasStep extends Step {
  route?: string
}

const steps: KanvasStep[] = [
  // ─── Boas-vindas e tour pela navegação ───
  {
    target: 'body',
    placement: 'center',
    title: 'Bem-vindo ao Kanvas',
    content:
      'Vamos passear pelas principais áreas do sistema em alguns minutos. Você pode pular a qualquer momento.',
    route: '/',
  },
  {
    target: 'aside',
    placement: 'right',
    title: 'Sidebar',
    content:
      'Aqui você navega entre os módulos: Comercial, Operação, Finanças e Configuração. O conteúdo muda conforme o módulo escolhido na topbar.',
  },
  {
    target: '[data-tour="module-switcher"]',
    placement: 'bottom',
    title: 'Módulos: Sistema · Configuração · Auditoria',
    content:
      'Use este botão para alternar entre o operacional do dia-a-dia, os cadastros (configuração) e o histórico de auditoria. A sidebar muda conforme o módulo escolhido.',
  },
  {
    target: '[data-tour="notifications-bell"]',
    placement: 'bottom',
    title: 'Notificações',
    content:
      'O sino mostra eventos importantes: proposta visualizada pela marca, aprovação pendente, pagamento recebido, entrega aprovada. Clique em uma para ir direto ao item.',
  },
  {
    target: '[data-tour="dashboard-kpis"]',
    placement: 'bottom',
    title: 'Indicadores principais',
    content:
      'No header da Dashboard você vê em tempo real: campanhas ativas, marcas, creators e entregas pendentes. Visão rápida da saúde da operação.',
  },

  // ─── Módulo Comercial ───
  {
    target: 'body',
    placement: 'center',
    title: 'Módulo Comercial',
    content:
      'Agora vamos passear pelo Comercial: pipeline de oportunidades, propostas, aprovações e atividades. É onde as vendas começam.',
    route: '/comercial/pipeline',
  },
  {
    target: 'body',
    placement: 'center',
    title: 'Comercial · Pipeline',
    content:
      'Cada coluna é um estágio do funil. Arraste oportunidades para mover entre estágios. Cards com borda colorida indicam SLA atrasado ou em risco.',
  },
  {
    target: 'body',
    placement: 'center',
    title: 'Comercial · Lista de oportunidades',
    content:
      'A mesma base do Pipeline em formato de tabela com filtros por marca, status, origem e tags. Use o que for melhor para a tarefa.',
    route: '/comercial/oportunidades',
  },
  {
    target: 'body',
    placement: 'center',
    title: 'Comercial · Detalhe da oportunidade',
    content:
      'Ao abrir uma oportunidade, você vê tabs com Resumo, Propostas vinculadas, Negociações, Aprovações, Follow-ups e Timeline (audit trail completo). Comentários e anexos centralizam a colaboração interna.',
  },
  {
    target: 'body',
    placement: 'center',
    title: 'Comercial · Propostas',
    content:
      'Lista global das propostas. Você pode filtrar por status (Rascunho, Enviada, Visualizada, Aprovada, Rejeitada, Convertida).',
    route: '/comercial/propostas',
  },
  {
    target: 'body',
    placement: 'center',
    title: 'Comercial · Detalhe da proposta',
    content:
      'Na tela da proposta você gera link público para a marca aprovar (com tracking de visualização), baixa em PDF, versiona, converte em campanha quando aprovada e dispara e-mail automático nos eventos.',
  },
  {
    target: 'body',
    placement: 'center',
    title: 'Comercial · Aprovações',
    content:
      'Centraliza solicitações de aprovação interna em oportunidades — útil quando o vendedor precisa de OK do gerente para fechar uma proposta.',
    route: '/comercial/aprovacoes',
  },
  {
    target: 'body',
    placement: 'center',
    title: 'Comercial · Atividades',
    content:
      'Sua agenda comercial: follow-ups e tarefas pendentes em todas as oportunidades. Notificações lembram quem está vencendo.',
    route: '/comercial/followups',
  },

  // ─── Módulo Operação ───
  {
    target: 'body',
    placement: 'center',
    title: 'Módulo Operação',
    content:
      'Agora a Operação: marcas atendidas, base de creators e execução das campanhas. É onde o trabalho acontece depois que a proposta é aprovada.',
    route: '/marcas',
  },
  {
    target: 'body',
    placement: 'center',
    title: 'Operação · Marcas',
    content:
      'Cadastro dos seus clientes (marcas atendidas). Aqui ficam as informações de contato e configuração de cada marca.',
  },
  {
    target: '[data-tour="creators-table"]',
    placement: 'top',
    title: 'Operação · Creators',
    content:
      'Lista de creators. Cada linha tem o botão "Abrir 360" para o perfil completo.',
    route: '/creators',
  },
  {
    target: 'body',
    placement: 'center',
    title: 'Operação · Creator 360',
    content:
      'Perfil completo do creator: handles sociais (Instagram, TikTok, YouTube) com seguidores e engajamento, métricas de performance por plataforma, histórico de todas as campanhas em que participou, faturamento gerado e taxa de on-time delivery.',
  },
  {
    target: 'body',
    placement: 'center',
    title: 'Operação · Campanhas',
    content:
      'Execução real do trabalho com a marca aprovada. Cada campanha tem creators participantes, deliverables com prazos, documentos e contratos.',
    route: '/campanhas',
  },
  {
    target: 'body',
    placement: 'center',
    title: 'Operação · Detalhe da campanha',
    content:
      'Tabs internas: Creators (status de participação), Documentos (contratos, briefings) e Entregas (deliverables com prazo, valor combinado, share link de aprovação para a marca, regra obrigatória de aprovação antes de publicar).',
  },
  {
    target: 'body',
    placement: 'center',
    title: 'Operação · Aprovações',
    content:
      'Entregas que aguardam aprovação da marca. Você gera um link público, a marca aprova ou rejeita pelo navegador, e só depois a entrega pode ser publicada.',
    route: '/operacao/aprovacoes',
  },

  // ─── Módulo Financeiro ───
  {
    target: 'body',
    placement: 'center',
    title: 'Módulo Financeiro',
    content:
      'Agora as finanças: contas a receber e a pagar, fluxo de caixa, aging e contas bancárias. Lançamentos são gerados automaticamente a partir das ações comerciais e operacionais.',
    route: '/financeiro/receber',
  },
  {
    target: '[data-tour="financial-entries-kpis"]',
    placement: 'bottom',
    title: 'Financeiro · Contas a receber',
    content:
      'KPIs em tempo real: pendente, recebido no mês, vencidos, próximos 7 dias. Lançamentos gerados automaticamente quando você converte uma proposta em campanha.',
  },
  {
    target: '[data-tour="financial-entries-kpis"]',
    placement: 'bottom',
    title: 'Financeiro · Contas a pagar',
    content:
      'Mesma visão dos KPIs, agora para saídas. Inclui repasses para creators (gerados automaticamente ao publicar entregas) e despesas operacionais lançadas manualmente.',
    route: '/financeiro/pagar',
  },
  {
    target: '[data-tour="cashflow-chart"]',
    placement: 'top',
    title: 'Financeiro · Fluxo de caixa',
    content:
      'Entradas e saídas (pendentes e realizadas) por dia, semana ou mês. Use para planejar caixa e ver tendências.',
    route: '/financeiro/fluxo-caixa',
  },
  {
    target: '[data-tour="aging-buckets"]',
    placement: 'top',
    title: 'Financeiro · Aging',
    content:
      'Distribuição de pendências por faixa de atraso (a vencer, 0-30, 31-60, 61-90, 90+). Quanto mais à direita, mais inadimplência.',
    route: '/financeiro/aging',
  },

  // ─── Módulo Configuração ───
  {
    target: 'body',
    placement: 'center',
    title: 'Módulo Configuração',
    content:
      'Para fechar: configurações da agência, integrações e templates de e-mail. Acesse pelo switcher de módulos na topbar a qualquer momento.',
    route: '/configuracao/empresa',
  },
  {
    target: 'body',
    placement: 'center',
    title: 'Configuração · Empresa',
    content:
      'Dados da agência (logo, CNPJ, endereço, e-mail principal) e canal padrão de envio de e-mail. Tudo aqui aparece em propostas e e-mails automáticos.',
  },
  {
    target: 'body',
    placement: 'center',
    title: 'Configuração · Integrações e Automações',
    content:
      'Conecte ferramentas externas via IntegrationPlatform e configure automações: "quando proposta enviada, enviar e-mail X" ou "quando conta paga, dar baixa no ERP".',
    route: '/configuracao/integracoes',
  },

  // ─── Encerramento ───
  {
    target: 'body',
    placement: 'center',
    title: 'Pronto para começar',
    content:
      'Você concluiu o tour. Use o sino de notificações para acompanhar eventos importantes e o switcher Sistema/Configuração na topbar para alternar entre o operacional e os cadastros.',
    route: '/',
  },
]

const tourSteps: KanvasStep[] = steps.map((step) => {
  if (step.placement === 'center') {
    return {
      ...step,
      disableBeacon: true,
      styles: {
        ...(step.styles ?? {}),
        spotlight: {
          display: 'none',
        },
      },
    }
  }
  return { ...step, disableBeacon: true }
})

interface ProductTourProps {
  run: boolean
  onClose: () => void
}

export default function ProductTour({ run, onClose }: ProductTourProps) {
  const navigate = useNavigate()
  const location = useLocation()
  const { markCompleted } = useProductTour()
  const [stepIndex, setStepIndex] = useState(0)
  const [pendingStepIndex, setPendingStepIndex] = useState<number | null>(null)

  useEffect(() => {
    if (run) {
      const firstStep = steps[0]
      setStepIndex(0)
      setPendingStepIndex(null)
      if (firstStep.route && firstStep.route !== location.pathname) {
        navigate(firstStep.route)
      }
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [run])

  useEffect(() => {
    if (pendingStepIndex === null) return
    const targetStep = steps[pendingStepIndex]
    if (!targetStep) return
    if (targetStep.route && targetStep.route !== location.pathname) return

    const targetSelector = typeof targetStep.target === 'string' ? targetStep.target : null
    const isBodyOrEmpty = !targetSelector || targetSelector === 'body'

    if (isBodyOrEmpty) {
      const handle = window.setTimeout(() => {
        setStepIndex(pendingStepIndex)
        setPendingStepIndex(null)
      }, 250)
      return () => window.clearTimeout(handle)
    }

    let elapsed = 0
    const intervalMs = 100
    const maxWaitMs = 6000
    const interval = window.setInterval(() => {
      elapsed += intervalMs
      const element = document.querySelector(targetSelector)
      if (element) {
        window.clearInterval(interval)
        window.setTimeout(() => {
          setStepIndex(pendingStepIndex)
          setPendingStepIndex(null)
        }, 150)
      } else if (elapsed >= maxWaitMs) {
        window.clearInterval(interval)
        setStepIndex(pendingStepIndex)
        setPendingStepIndex(null)
      }
    }, intervalMs)
    return () => window.clearInterval(interval)
  }, [location.pathname, pendingStepIndex])

  const handleCallback = (data: CallBackProps) => {
    const { status, type, action, index } = data

    if (status === STATUS.FINISHED || status === STATUS.SKIPPED) {
      markCompleted()
      onClose()
      return
    }

    if (type === EVENTS.STEP_AFTER) {
      const direction = action === ACTIONS.PREV ? -1 : 1
      const nextIndex = index + direction
      const nextStep = steps[nextIndex]

      if (!nextStep) {
        markCompleted()
        onClose()
        return
      }

      if (nextStep.route && nextStep.route !== location.pathname) {
        setPendingStepIndex(nextIndex)
        navigate(nextStep.route)
      } else {
        setPendingStepIndex(nextIndex)
      }
    }

    if (type === EVENTS.TARGET_NOT_FOUND) {
      // Não avança automaticamente — o polling em pendingStepIndex aguarda o target aparecer
      const currentStep = steps[index]
      if (currentStep) {
        setPendingStepIndex(index)
      }
    }
  }

  return (
    <Joyride
      steps={tourSteps}
      run={run}
      stepIndex={stepIndex}
      continuous
      showProgress
      showSkipButton
      disableScrolling={false}
      scrollToFirstStep
      callback={handleCallback}
      locale={{
        back: 'Voltar',
        close: 'Fechar',
        last: 'Concluir',
        next: 'Próximo',
        skip: 'Pular tour',
      }}
      styles={{
        options: {
          primaryColor: '#6366f1',
          zIndex: 10000,
        },
        tooltip: {
          borderRadius: 8,
          fontSize: 13,
        },
        tooltipTitle: {
          fontSize: 15,
          fontWeight: 600,
        },
        buttonNext: {
          borderRadius: 6,
          fontSize: 13,
        },
        buttonBack: {
          fontSize: 13,
        },
        buttonSkip: {
          fontSize: 12,
        },
      }}
    />
  )
}
