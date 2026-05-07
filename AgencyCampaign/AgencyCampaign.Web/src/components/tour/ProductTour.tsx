import { useEffect, useState } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import Joyride, { ACTIONS, EVENTS, STATUS } from 'react-joyride'
import type { CallBackProps, Step } from 'react-joyride'
import { useProductTour } from '../../hooks/useProductTour'

interface KanvasStep extends Step {
  route?: string
}

const steps: KanvasStep[] = [
  {
    target: 'body',
    placement: 'center',
    disableBeacon: true,
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
    target: '[data-tour="onboarding-button"]',
    placement: 'bottom',
    title: 'Onboarding guiado',
    content:
      'Este botão abre um wizard que ajuda você a cadastrar os dados iniciais (marca, creator, primeira oportunidade, etc.).',
  },
  {
    target: '[data-tour="kanban-board"]',
    placement: 'top',
    title: 'Pipeline comercial',
    content:
      'Cada coluna é um estágio do funil. Arraste oportunidades para mover. Cards com borda colorida indicam SLA por estágio.',
    route: '/comercial/pipeline',
  },
  {
    target: 'body',
    placement: 'center',
    title: 'Lista de oportunidades',
    content:
      'A mesma base do Pipeline também aparece em formato de tabela com filtros por marca, status, origem e tags. Use o que for melhor para a tarefa.',
    route: '/comercial/oportunidades',
  },
  {
    target: 'body',
    placement: 'center',
    title: 'Propostas',
    content:
      'Crie propostas a partir de uma oportunidade, gere link público para a marca, baixe em PDF e converta em campanha quando aprovada.',
    route: '/comercial/propostas',
  },
  {
    target: 'body',
    placement: 'center',
    title: 'Aprovações',
    content:
      'Centraliza solicitações de aprovação interna em oportunidades — útil quando o vendedor precisa de OK do gerente para fechar uma proposta.',
    route: '/comercial/aprovacoes',
  },
  {
    target: 'body',
    placement: 'center',
    title: 'Atividades',
    content:
      'Sua agenda comercial: follow-ups e tarefas pendentes em todas as oportunidades. Notificações lembram quem está vencendo.',
    route: '/comercial/followups',
  },
  {
    target: 'body',
    placement: 'center',
    title: 'Marcas',
    content:
      'Cadastro dos seus clientes (marcas atendidas). Aqui ficam as informações de contato e configuração de cada marca.',
    route: '/marcas',
  },
  {
    target: 'body',
    placement: 'center',
    title: 'Creators 360',
    content:
      'Cadastro de creators com handles sociais, performance por plataforma, histórico de campanhas e on-time delivery. Clique em "Abrir 360" em um creator para ver o perfil completo.',
    route: '/creators',
  },
  {
    target: 'body',
    placement: 'center',
    title: 'Campanhas',
    content:
      'Execução real do trabalho: creators participantes, deliverables com prazos, documentos e share link público para aprovação da marca.',
    route: '/campanhas',
  },
  {
    target: 'body',
    placement: 'center',
    title: 'Aprovações de operação',
    content:
      'Entregas que aguardam aprovação da marca. Você gera um link público, a marca aprova ou rejeita, e só depois a entrega pode ser publicada.',
    route: '/operacao/aprovacoes',
  },
  {
    target: 'body',
    placement: 'center',
    title: 'Contas a receber',
    content:
      'Lançamentos gerados automaticamente quando você converte uma proposta em campanha. KPIs no topo: pendente, recebido no mês, vencidos, próximos 7 dias.',
    route: '/financeiro/receber',
  },
  {
    target: 'body',
    placement: 'center',
    title: 'Contas a pagar',
    content:
      'Repasses para creators (gerados automaticamente quando você publica uma entrega) e despesas operacionais que você lança manualmente.',
    route: '/financeiro/pagar',
  },
  {
    target: 'body',
    placement: 'center',
    title: 'Fluxo de caixa',
    content:
      'Gráfico de entradas e saídas (pendentes e realizadas) por dia, semana ou mês. Use para planejar caixa e ver tendências.',
    route: '/financeiro/fluxo-caixa',
  },
  {
    target: 'body',
    placement: 'center',
    title: 'Aging financeiro',
    content:
      'Distribuição de pendências por faixa de atraso. Quanto mais alto o bucket "90+ dias", mais inadimplência.',
    route: '/financeiro/aging',
  },
  {
    target: 'body',
    placement: 'center',
    title: 'Configuração da empresa',
    content:
      'Dados da agência (logo, CNPJ, endereço, e-mail principal) e canal padrão de envio de e-mail. Tudo aqui aparece em propostas e e-mails automáticos.',
    route: '/configuracao/empresa',
  },
  {
    target: 'body',
    placement: 'center',
    title: 'Integrações e Automações',
    content:
      'Conecte ferramentas externas via IntegrationPlatform e configure automações: "quando proposta enviada, enviar e-mail X" ou "quando conta paga, dar baixa no ERP".',
    route: '/configuracao/integracoes',
  },
  {
    target: 'body',
    placement: 'center',
    title: 'Pronto para começar',
    content:
      'Você concluiu o tour. Use o sino de notificações para acompanhar eventos importantes e o switcher Sistema/Configuração na topbar para alternar entre o operacional e os cadastros.',
    route: '/',
  },
]

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

    const handle = window.setTimeout(() => {
      setStepIndex(pendingStepIndex)
      setPendingStepIndex(null)
    }, 350)
    return () => window.clearTimeout(handle)
  }, [location.pathname, pendingStepIndex])

  const handleCallback = (data: CallBackProps) => {
    const { status, type, action, index } = data

    if (status === STATUS.FINISHED || status === STATUS.SKIPPED) {
      markCompleted()
      onClose()
      return
    }

    if (type === EVENTS.STEP_AFTER || type === EVENTS.TARGET_NOT_FOUND) {
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
        setStepIndex(nextIndex)
      }
    }
  }

  return (
    <Joyride
      steps={steps}
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
