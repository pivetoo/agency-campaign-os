# Sistema para Agências de Marketing de Influência

O Kanvas é um sistema operacional completo para agências que trabalham com marketing de influência. Ele cobre o ciclo de ponta a ponta: prospecção comercial, gestão de propostas, produção de campanhas com creators, controle financeiro e auditoria.

A plataforma é organizada em três módulos de negócio:

- **Comercial** — prospecção, pipeline, propostas e aprovações até o fechamento.
- **Produção** — execução das campanhas com creators: entregáveis, aprovações de conteúdo e documentos.
- **Financeiro** — contas, lançamentos, conciliação bancária e pagamentos a creators.

> Documentação por módulo em evolução. Esta versão documenta o **Módulo Comercial** em detalhe; **Produção** e **Financeiro** estão com a estrutura preparada e serão detalhados na sequência.

## Sumário

- [Módulo Comercial](#módulo-comercial)
  - [Pipeline e oportunidades](#pipeline-e-oportunidades)
  - [Propostas](#propostas)
  - [Desconto, prazo e total líquido](#desconto-prazo-e-total-líquido)
  - [Aprovações (desconto e prazo)](#aprovações-desconto-e-prazo)
  - [Follow-ups](#follow-ups)
  - [Metas comerciais](#metas-comerciais)
  - [Painel, forecast e analytics](#painel-forecast-e-analytics)
  - [Configurações do comercial](#configurações-do-comercial)
  - [Telas e rotas](#telas-e-rotas)
  - [Principais endpoints](#principais-endpoints)
- [Módulo Produção](#módulo-produção)
- [Módulo Financeiro](#módulo-financeiro)

---

## Módulo Comercial

Cobre todo o funil comercial: da entrada do lead ao fechamento (ganho/perdido) e à conversão da proposta em campanha. Reúne pipeline visual, gestão de oportunidades, propostas com desconto/prazo e total líquido, versionamento, link público e PDF, gate de aprovação por política comercial, follow-ups, metas e analytics.

O modelo é **centrado na proposta**: as condições comerciais (desconto e prazo de pagamento) e a aprovação vivem na própria proposta. Não há entidade de negociação separada nem probabilidade/margem digitadas manualmente.

### Pipeline e oportunidades

**Funcionalidades**

- Pipeline visual (kanban) com as oportunidades em colunas por estágio; arrastar e soltar entre estágios no desktop. No mobile, vira um seletor de etapa com a lista vertical das oportunidades daquela etapa.
- Lista de oportunidades com paginação, busca e filtros (estágio, responsável, status aberta/ganha/perdida).
- Detalhe da oportunidade com abas: **Resumo**, **Aprovações**, **Propostas**, **Follow-ups** e **Atividade**.
- Fechamento como **ganha** ou **perdida** com registro de motivo. Cada oportunidade guarda marca, valor estimado, data prevista de fechamento, contato (nome/e-mail/telefone), origem e tags.
- Escopo "minhas" oportunidades (restritas ao usuário) além da visão geral, conforme permissão.

**Estágios do pipeline (`CommercialPipelineStage`)**

- Configuráveis: nome, cor, ordem de exibição, SLA em dias, probabilidade padrão (`DefaultProbability`), marcação de estágio inicial e final.
- `FinalBehavior`: `Won` (ao mover a oportunidade, fecha como ganha e probabilidade vai a 100%) ou `Lost` (fecha como perdida e probabilidade vai a 0%). Estágios intermediários (`None`) aplicam a probabilidade padrão do estágio.

**Probabilidade e regras de negócio**

- A probabilidade de cada oportunidade é **derivada do estágio** (a partir da `DefaultProbability`), nunca digitada. Ela é aplicada já na criação da oportunidade (a partir do estágio inicial) e reassumida a cada mudança de estágio. Serve exclusivamente para ponderar o forecast.
- Mover uma oportunidade para um estágio final dispara o fechamento automático (ganho/perdido) com nota padrão e ajuste da probabilidade (100% ganho, 0% perdido).
- O desfecho (ganho/perdido) é tratado pelo comportamento semântico do estágio (`FinalBehavior`), não por comparação de nome de status.
- O SLA por estágio gera indicadores de risco (estourado / em atenção) nos cards do pipeline.
- Histórico de mudanças de estágio é registrado e exibido na aba Atividade.

### Propostas

**Funcionalidades**

- Lista de propostas com paginação, busca e filtros (status, responsável). A lista exibe o **total líquido** (com o total bruto riscado quando há desconto).
- Detalhe da proposta com os itens, o cartão de **desconto/prazo** e o painel de **compartilhamento** (links públicos e versões).
- Itens da proposta com creator opcional, quantidade, valor unitário, prazo de entrega e total calculado; o total bruto da proposta é a soma dos itens.
- **Templates de proposta** reutilizáveis, aplicáveis a uma proposta via modal.
- **Versionamento**: cada envio registra uma versão com snapshot completo (JSON) para rastreabilidade.
- **Link público**: gera um link com token (sem login) para o cliente visualizar a proposta, com expiração opcional, revogação e contagem de visualizações (IP/user agent). Página pública em `/p/:token`.
- **PDF**: geração do PDF da proposta via navegador headless (PuppeteerSharp), com a **logo da agência** embutida e o layout do template ativo. Disponível tanto no detalhe autenticado quanto no link público (botão "Baixar PDF").
- **Envio** por e-mail ou WhatsApp (cria/garante o link público, marca a proposta como enviada e enfileira o envio no IntegrationPlatform).
- **Conversão**: proposta aprovada pode ser convertida em uma campanha existente ou gerar uma nova campanha.

**Ciclo de vida (`ProposalStatus`)**

```
Draft → Sent → Viewed → Approved → Converted
          │       │          │
       Expired  Rejected   Cancelled
```

- `Draft` (rascunho) → `Sent` (enviada) → `Viewed` (visualizada) → `Approved` (aprovada) → `Converted` (virou campanha).
- `Expired` (validade vencida), `Rejected` (rejeitada) e `Cancelled` (cancelada) são terminais.

### Desconto, prazo e total líquido

As condições comerciais vivem na própria proposta:

- **Desconto** pode ser informado em **R$ ou em %**, com conversão automática entre os dois no detalhe da proposta. É **armazenado como valor monetário** (`DiscountAmount`, decimal/`NUMERIC`), o que evita perda de precisão no vai-e-volta entre valor e percentual.
- O sistema deriva, sem persistir: `DiscountValue` (desconto efetivo, limitado ao total bruto), `DiscountPercent` (percentual equivalente) e `NetTotalValue` (**total líquido** = total bruto − desconto).
- **Prazo de pagamento** em dias (`PaymentTermDays`).
- Desconto e prazo alimentam o gate de aprovação (abaixo) quando há política comercial definida.

### Aprovações (desconto e prazo)

Quando o desconto ou o prazo de uma proposta extrapola a política comercial, entra um gate de aprovação interna ancorado na proposta.

**Política comercial (`CommercialPolicy`)**

- Define os limites: desconto máximo e prazo de pagamento máximo. (A entidade ainda guarda uma margem mínima por compatibilidade, mas a margem **não é avaliada** no gate — apenas desconto e prazo.)
- O `PolicyEvaluator` compara a proposta (desconto/prazo) com a política vigente e identifica as violações (`HasDeviations`).

**Como uma proposta é aprovada**

- **Sem política definida, ou proposta dentro dos limites**: aprovação direta pelo botão **Aprovar** no detalhe da proposta (status `Sent`/`Viewed` → `Approved`), sem revisão.
- **Gate automático (no envio)**: ao **enviar** uma proposta cujo desconto/prazo **estoura** a política, o envio é **bloqueado** e uma requisição de aprovação (`OpportunityApprovalRequest`, ancorada na proposta via `ProposalId`) é criada automaticamente.
- **Solicitação manual**: o usuário pode pedir aprovação a qualquer momento, mesmo sem desvio, pelo botão de solicitação na proposta.
- Enquanto existe uma aprovação **em aberto**, o envio permanece bloqueado (mensagem específica de "aprovação pendente", distinta de "excede a política").

**Conteúdo da requisição e revisão**

- **Diffs** (`OpportunityApprovalDiff`): o que diverge da política (valor da política vs. solicitado e o delta). **Impactos** (`OpportunityApprovalImpact`): o efeito da exceção.
- Tipos de aprovação: desconto, prazo ou exceção.
- A requisição pode ter múltiplos **revisores** (`OpportunityApprovalReviewer`), obrigatórios ou não, cada um com status próprio. Quando todos os revisores obrigatórios decidem, a requisição é aprovada (todos aprovaram) ou rejeitada (algum rejeitou).
- Estados da requisição: `Pending` → (opcional) `InReview` → `Approved`/`Rejected`/`ChangesRequested` → `Merged`. "Solicitar mudanças" devolve ao solicitante, que pode **resubmeter** (volta a `Pending`).
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
- **Forecast**: previsão de receita do período combinando o **ganho** (oportunidades já fechadas, valor cheio) com o **ponderado em aberto** (soma de `valor estimado × probabilidade/100` das oportunidades abertas), além da quebra por estágio (quantidade, valor, valor ponderado e probabilidade média).
- **Analytics**: oportunidades fechadas (ganhas/perdidas), taxa de ganho, ciclo médio, conversão e tempo médio por estágio, top performers e motivos de ganho/perda.
- **Insights**: próximos fechamentos e oportunidades em risco (aging), além de alertas comerciais.

### Configurações do comercial

- **Estágios do pipeline**: criar/editar (cor, ordem, SLA, probabilidade padrão, comportamento final). Apenas um estágio inicial por pipeline.
- **Política comercial**: limites de desconto e prazo de pagamento (alimentam o gate de aprovação). O campo de margem mínima existe no cadastro mas não é avaliado.
- **Fontes de oportunidade** (`OpportunitySource`): origem do lead (ex.: inbound, indicação), com cor e ordenação; podem ser desativadas preservando o histórico.
- **Motivos de ganho/perda**: catálogo usado no fechamento das oportunidades.
- **Templates de proposta**: modelos reutilizáveis de layout/itens.

### Telas e rotas

| Rota | Tela | Função |
|------|------|--------|
| `/comercial` | — | Redireciona para o pipeline |
| `/comercial/pipeline` | `Pipeline.tsx` | Kanban de oportunidades por estágio |
| `/comercial/oportunidades` | `Opportunities.tsx` | Lista de oportunidades |
| `/comercial/oportunidades/:id` | `OpportunityDetail.tsx` | Detalhe com abas (`?tab=`) |
| `/comercial/propostas` | `Proposals.tsx` | Lista de propostas |
| `/comercial/propostas/:id` | `ProposalDetail.tsx` | Detalhe da proposta (itens, desconto/prazo, compartilhamento) |
| `/comercial/aprovacoes` | `Approvals.tsx` | Caixa de solicitações de aprovação |
| `/comercial/followups` | `FollowUps.tsx` | Follow-ups por situação |
| `/comercial/metas` | `Goals.tsx` | Metas comerciais |
| `/comercial/analytics` | `Analytics.tsx` | Indicadores e análises do funil |
| `/p/:token` | `Public/Proposal.tsx` | Visualização pública de proposta (sem login) |

Principais modais: criar/editar oportunidade, follow-up, solicitação de aprovação (na proposta), proposta, item de proposta, envio de proposta, aplicação de template, meta comercial e configuração de estágio/motivos.

### Principais endpoints

Todos protegidos por `[RequireAccess]`, exceto os públicos de proposta (`[AllowAnonymous]`). Rotas relativas à API do AgencyCampaign.

- **Oportunidades** (`/opportunities`): listar, `mine`, detalhe, `board`, `forecast`, `analytics`, `insights`, `StageHistory`; criar, atualizar, excluir; `ChangeStage`, `CloseAsWon`, `CloseAsLost`. O mesmo controller agrupa, como sub-recursos da oportunidade, os comentários e os follow-ups. A probabilidade não tem endpoint nem campo de entrada: é derivada do estágio e usada só para ponderar o forecast.
- **Propostas** (`/proposals`): listar, detalhe, `pdf/{id}`; criar, atualizar (incluindo desconto/prazo); itens (criar/editar/remover); `SendByEmail`, `SendByWhatsapp`, `MarkAsSent`, `MarkAsViewed`, `Approve`, `Reject`, `ConvertToCampaign`, `ConvertToNewCampaign`, `Cancel`, `StatusHistory`; links de compartilhamento (criar/listar/revogar) e versões (listar). Não há exclusão de proposta pela API.
- **Proposta pública** (`/proposal-public/{token}` e `/proposal-public/{token}/pdf`): acesso anônimo por token; a consulta registra a visualização e o PDF é gerado sob demanda.
- **Aprovações** (`/opportunityApprovals`): listar, por proposta; criar; `Approve`, `Reject`, `MarkInReview`, `RequestChanges`, `Resubmit`, `MarkMerged`; decisão de revisor (`Reviewers/Decision`); `Comments` (CRUD); `Reviewers`, `Diffs` e `Impacts` (gerenciáveis); `evaluate-policy` e `PopulateFromPolicy` (avaliam/regeneram os desvios pela política).
- **Política comercial** (`/commercialPolicy`): obter e atualizar (upsert).
- **Estágios** (`/commercialPipelineStages`): listar, `active`, detalhe, criar/atualizar/excluir.
- **Metas** (`/commercialGoals`): listar, `progress`, criar/atualizar/excluir.
- **Fontes** (`/opportunitySources`): listar, criar/atualizar/excluir.
- **Templates de proposta** (`/proposalTemplates`): listar, detalhe, criar/atualizar, versões.

---

## Módulo Produção

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
