# Sistema para Agências de Marketing de Influência

O Kanvas é um sistema operacional completo para agências que trabalham com marketing de influência. Ele cobre o ciclo de ponta a ponta: prospecção comercial, gestão de propostas, operação de campanhas com creators, controle financeiro e auditoria.

A plataforma é organizada em três módulos de negócio:

- **Comercial** — prospecção, pipeline, propostas, negociações e aprovações até o fechamento.
- **Operacional** — execução das campanhas com creators: entregáveis, aprovações de conteúdo e documentos.
- **Financeiro** — contas, lançamentos, conciliação bancária e pagamentos a creators.

> Documentação por módulo em evolução. Esta versão documenta o **Módulo Comercial** em detalhe; **Operacional** e **Financeiro** estão com a estrutura preparada e serão detalhados na sequência.

## Sumário

- [Módulo Comercial](#módulo-comercial)
  - [Pipeline e oportunidades](#pipeline-e-oportunidades)
  - [Propostas](#propostas)
  - [Negociações e aprovações](#negociações-e-aprovações)
  - [Follow-ups](#follow-ups)
  - [Metas comerciais](#metas-comerciais)
  - [Painel, forecast e analytics](#painel-forecast-e-analytics)
  - [Configurações do comercial](#configurações-do-comercial)
  - [Telas e rotas](#telas-e-rotas)
  - [Principais endpoints](#principais-endpoints)
- [Módulo Operacional](#módulo-operacional)
- [Módulo Financeiro](#módulo-financeiro)

---

## Módulo Comercial

Cobre todo o funil comercial: da entrada do lead ao fechamento (ganho/perdido) e à conversão da proposta em campanha. Reúne pipeline visual, gestão de oportunidades, propostas com versionamento e link público, negociações com gate de aprovação por política comercial, follow-ups, metas e analytics.

### Pipeline e oportunidades

**Funcionalidades**

- Pipeline visual (kanban) com as oportunidades em colunas por estágio; arrastar e soltar entre estágios no desktop. No mobile, vira um seletor de etapa com a lista vertical das oportunidades daquela etapa.
- Lista de oportunidades com paginação, busca e filtros (estágio, responsável, status aberta/ganha/perdida).
- Detalhe da oportunidade com abas: **Resumo**, **Negociações**, **Aprovações**, **Propostas**, **Follow-ups** e **Atividade**.
- Fechamento como **ganha** ou **perdida** com registro de motivo. Cada oportunidade guarda marca, valor estimado, probabilidade, data prevista de fechamento, contato (nome/e-mail/telefone), origem e tags.
- Escopo "minhas" oportunidades (restritas ao usuário) além da visão geral, conforme permissão.

**Estágios do pipeline (`CommercialPipelineStage`)**

- Configuráveis: nome, cor, ordem de exibição, SLA em dias, probabilidade padrão, marcação de estágio inicial e final.
- `FinalBehavior`: `Won` (ao mover a oportunidade, fecha como ganha e probabilidade vai a 100%) ou `Lost` (fecha como perdida e probabilidade vai a 0%). Estágios intermediários (`None`) aplicam a probabilidade padrão do estágio.

**Regras de negócio**

- Mover uma oportunidade para um estágio final dispara o fechamento automático (ganho/perdido) com nota padrão e ajuste da probabilidade.
- A probabilidade é definida automaticamente pelo estágio (a partir da `DefaultProbability` dele), exceto quando o usuário a fixa manualmente no formulário da oportunidade. Mover de estágio reaplica a probabilidade padrão enquanto não for manual.
- O SLA por estágio gera indicadores de risco (estourado / em atenção) nos cards do pipeline.
- Histórico de mudanças de estágio é registrado e exibido na aba Atividade.

### Propostas

**Funcionalidades**

- Lista de propostas com paginação, busca e filtros (status, responsável). Detalhe com a aba de **Dados** (itens) e a aba de **Compartilhamento** (links públicos e versões).
- Itens da proposta com creator opcional, quantidade, valor unitário, prazo de entrega e total calculado; o total da proposta é a soma dos itens.
- **Templates de proposta** reutilizáveis, aplicáveis a uma proposta via modal.
- **Versionamento**: cada versão guarda um snapshot completo (JSON) para rastreabilidade.
- **Link público**: gera um link com token (sem necessidade de login) para o cliente visualizar a proposta, com expiração opcional, revogação e contagem de visualizações (IP/user agent). Página pública em `/p/:token`.
- **Envio** por e-mail ou WhatsApp (marca a proposta como enviada) e **geração de PDF**.
- **Conversão**: proposta aprovada pode ser convertida em uma campanha existente ou gerar uma nova campanha.

**Ciclo de vida (`ProposalStatus`)**

```
Draft → Sent → Viewed → Approved → Converted
          │       │          │
       Expired  Rejected   Cancelled
```

- `Draft` (rascunho) → `Sent` (enviada) → `Viewed` (visualizada) → `Approved` (aprovada) → `Converted` (virou campanha).
- `Expired` (validade vencida, automático quando `ValidityUntil` passa e estava enviada), `Rejected` (rejeitada) e `Cancelled` (cancelada) são terminais.

### Negociações e aprovações

O coração das regras comerciais: registra as tratativas de valor/condição de uma oportunidade e aplica o gate de aprovação quando a negociação fere a política comercial.

**Negociações (`OpportunityNegotiation`)**

- Registram título, valor negociado, percentual de desconto, margem e prazo de pagamento.
- Ciclo: `Draft` → `PendingApproval` → `Approved`/`Rejected` → `SentToClient` → `AcceptedByClient` (ou `Cancelled`).

**Política comercial (`CommercialPolicy`)**

- Singleton com os limites: desconto máximo, margem mínima, prazo de pagamento padrão e máximo.
- O `PolicyEvaluator` compara a negociação com a política vigente e identifica as violações.

**Gate de aprovação (regra central)**

- Aprovar uma negociação é direto **quando não há desvio**. A revisão só se torna **obrigatória** quando existe uma política comercial e ela é **estourada** (desconto acima do máximo, margem abaixo da mínima ou prazo acima do máximo). Sem política definida, não há gate.
- Ao criar a requisição de aprovação (`OpportunityApprovalRequest`), o sistema popula automaticamente, a partir do `PolicyEvaluator`:
  - **Diffs** (`OpportunityApprovalDiff`): o que diverge da política (valor da política vs. valor solicitado e o delta).
  - **Impactos** (`OpportunityApprovalImpact`): o efeito da exceção (ex.: redução de margem).
- Tipos de aprovação: desconto, margem, prazo ou exceção.

**Fluxo de revisão**

- A requisição pode ter múltiplos **revisores** (`OpportunityApprovalReviewer`), obrigatórios ou não, cada um com status próprio (pendente/aprovou/rejeitou/comentou).
- Decisão automática: quando todos os revisores obrigatórios decidem, a requisição é aprovada (todos aprovaram) ou rejeitada (algum rejeitou).
- Estados da requisição: `Pending` → (opcional) `InReview` → `Approved`/`Rejected`/`ChangesRequested` → `Merged`. "Solicitar mudanças" devolve para o solicitante, que pode **resubmeter** (volta a `Pending`).
- **Comentários** (`OpportunityApprovalComment`) permitem discussão durante a revisão, com menções a usuários.
- A tela de Aprovações é uma caixa de **Solicitações** (master-detail): lista à esquerda com filtros (pendentes/aprovadas/rejeitadas/todas) e o detalhe à direita (no mobile, o detalhe abre como bottom-sheet ao tocar na solicitação).

### Follow-ups

- Agendamento de ações de acompanhamento por oportunidade: assunto, data de vencimento e notas; marcação de concluído.
- Tela dedicada com abas por situação (atrasados, hoje, próximos, concluídos) e acesso direto à oportunidade relacionada. Indicadores de atraso aparecem também no pipeline e no detalhe da oportunidade.

### Metas comerciais

- Metas por período (mensal, trimestral ou anual), globais (agência) ou por usuário, com valor-alvo.
- Cálculo de **progresso** comparando o realizado (oportunidades ganhas no período) com o alvo.
- Widget de metas no painel/insights com barra de progresso e cores por nível de atingimento.

### Painel, forecast e analytics

- **Board (kanban)**: oportunidades agrupadas por estágio, com contagem e valor por coluna.
- **Forecast**: projeção ponderada de receita no período (valor estimado × probabilidade), decomposta em ganho/ponderado/perdido.
- **Analytics**: oportunidades fechadas (ganhas/perdidas), taxa de ganho, ciclo médio, conversão e tempo médio por estágio, top performers e motivos de ganho/perda.
- **Insights**: próximos fechamentos e oportunidades em risco (aging), além de alertas comerciais.

### Configurações do comercial

- **Estágios do pipeline**: criar/editar (cor, ordem, SLA, probabilidade, comportamento final). Apenas um estágio inicial por pipeline.
- **Política comercial**: limites de desconto, margem e prazo (alimenta o gate de aprovação).
- **Fontes de oportunidade** (`OpportunitySource`): origem do lead (ex.: inbound, indicação), com cor e ordenação; podem ser desativadas preservando o histórico.
- **Motivos de ganho/perda**: catálogo usado no fechamento das oportunidades.
- **Templates de proposta**: modelos reutilizáveis de itens.

### Telas e rotas

| Rota | Tela | Função |
|------|------|--------|
| `/comercial` | — | Redireciona para o pipeline |
| `/comercial/pipeline` | `Pipeline.tsx` | Kanban de oportunidades por estágio |
| `/comercial/oportunidades` | `Opportunities.tsx` | Lista de oportunidades |
| `/comercial/oportunidades/:id` | `OpportunityDetail.tsx` | Detalhe com abas (`?tab=`) |
| `/comercial/propostas` | `Proposals.tsx` | Lista de propostas |
| `/comercial/propostas/:id` | `ProposalDetail.tsx` | Detalhe da proposta (dados, itens, compartilhamento) |
| `/comercial/aprovacoes` | `Approvals.tsx` | Caixa de solicitações de aprovação |
| `/comercial/followups` | `FollowUps.tsx` | Follow-ups por situação |
| `/comercial/metas` | `Goals.tsx` | Metas comerciais |
| `/comercial/analytics` | `Analytics.tsx` | Indicadores e análises do funil |
| `/p/:token` | `Public/Proposal.tsx` | Visualização pública de proposta (sem login) |

Principais modais: criar/editar oportunidade, negociação, follow-up, solicitação de aprovação, proposta, item de proposta, envio de proposta, aplicação de template, meta comercial e configuração de estágio/motivos.

### Principais endpoints

Todos protegidos por `[RequireAccess]` (exceto os públicos de proposta). Rotas relativas à API do AgencyCampaign.

- **Oportunidades** (`/opportunities`): listar, `mine`, detalhe, `board`, `forecast`, `analytics`, `insights`, `StageHistory`; criar, atualizar, excluir; `ChangeStage`, `CloseAsWon`, `CloseAsLost`. O mesmo controller agrupa, como sub-recursos da oportunidade, os comentários, as negociações e os follow-ups. A probabilidade não tem endpoint próprio: vai no criar/editar e é aplicada pelo estágio.
- **Propostas** (`/proposals`): listar, detalhe, `pdf`; criar, atualizar; itens (criar/editar/remover); `SendByEmail`, `SendByWhatsapp`, `MarkAsSent`, `MarkAsViewed`, `Approve`, `Reject`, `ConvertToCampaign`, `ConvertToNewCampaign`, `Cancel`, `StatusHistory`; links de compartilhamento (criar/listar/revogar) e versões (listar). Não há exclusão de proposta pela API.
- **Proposta pública** (`/public/proposals/{token}` e `/pdf`): acesso por token, registra visualização.
- **Aprovações** (`/opportunityApprovals`): listar, por negociação; criar; `Approve`, `Reject`, `MarkInReview`, `RequestChanges`, `Resubmit`, `MarkMerged`; decisão de revisor (`Reviewers/Decision`); `Comments` (CRUD); `Reviewers`, `Diffs` e `Impacts` (gerenciáveis); `evaluate-policy` e `PopulateFromPolicy` (avaliam/regeneram os desvios pela política).
- **Política comercial** (`/commercialPolicy`): obter e atualizar (upsert).
- **Estágios** (`/commercialPipelineStages`): listar, `active`, detalhe, criar/atualizar/excluir.
- **Metas** (`/commercialGoals`): listar, `progress`, criar/atualizar/excluir.
- **Fontes** (`/opportunitySources`): listar, criar/atualizar/excluir.
- **Templates de proposta** (`/proposalTemplates`): listar, detalhe, criar/atualizar, versões.

---

## Módulo Operacional

> A documentar. Cobre a execução das campanhas com creators (entregáveis, aprovações de conteúdo, documentos e assinaturas, portal do creator).

Estrutura prevista para esta seção:

- Visão geral do módulo
- Campanhas e creators na campanha
- Entregáveis e métricas
- Aprovações de entregáveis e links de compartilhamento
- Documentos e assinaturas
- Portal do creator (acesso por token)
- Telas e rotas
- Principais endpoints

---

## Módulo Financeiro

> A documentar. Cobre o controle financeiro: contas, lançamentos, categorias, conciliação bancária e pagamentos a creators.

Estrutura prevista para esta seção:

- Visão geral do módulo
- Contas financeiras e integração bancária (via IntegrationPlatform)
- Lançamentos, categorias e subcategorias
- Conciliação de transações bancárias
- Pagamentos a creators
- Relatórios financeiros
- Telas e rotas
- Principais endpoints
