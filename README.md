# Sistema para Agências de Marketing de Influência

O Kanvas é um sistema operacional completo para agências que trabalham com marketing de influência. Ele cobre o ciclo de ponta a ponta: prospecção comercial, gestão de propostas, produção de campanhas com creators, controle financeiro e auditoria.

A plataforma é organizada em três módulos de negócio:

- **Comercial** — prospecção, pipeline, propostas e aprovações até o fechamento.
- **Produção** — execução das campanhas com creators: entregáveis, aprovações de conteúdo e documentos.
- **Financeiro** — contas, lançamentos, conciliação bancária e pagamentos a creators.

> Documentação por módulo em evolução. Esta versão documenta os **Módulos Comercial e de Produção** em detalhe; o **Financeiro** está com a estrutura preparada e será detalhado na sequência.

## Sumário

- [Módulo Comercial](#módulo-comercial)
  - [Pipeline e oportunidades](#pipeline-e-oportunidades)
  - [Propostas](#propostas)
  - [Itens: tipos, direitos de uso e precificação](#itens-tipos-direitos-de-uso-e-precificação)
  - [Desconto, prazo e total líquido](#desconto-prazo-e-total-líquido)
  - [Aprovações (desconto e prazo)](#aprovações-desconto-e-prazo)
  - [Aceite do cliente no link público](#aceite-do-cliente-no-link-público)
  - [Engajamento da proposta](#engajamento-da-proposta)
  - [Follow-ups](#follow-ups)
  - [Metas comerciais](#metas-comerciais)
  - [Painel, forecast e analytics](#painel-forecast-e-analytics)
  - [Rotinas automáticas](#rotinas-automáticas)
  - [Configurações do comercial](#configurações-do-comercial)
  - [Telas e rotas](#telas-e-rotas)
  - [Principais endpoints](#principais-endpoints)
- [Módulo de Produção](#módulo-de-produção)
  - [Visão geral](#visão-geral-da-produção)
  - [Campanhas e creators na campanha](#campanhas-e-creators-na-campanha)
  - [Entregáveis e métricas](#entregáveis-e-métricas)
  - [Aprovação de entregáveis e revisão de conteúdo](#aprovação-de-entregáveis-e-revisão-de-conteúdo)
  - [Documentos e assinaturas](#documentos-e-assinaturas)
  - [Mídia privada](#mídia-privada)
  - [Portal do creator (acesso por token)](#portal-do-creator-acesso-por-token)
  - [Rotinas automáticas da produção](#rotinas-automáticas-da-produção)
  - [Configurações da produção](#configurações-da-produção)
  - [Telas e rotas](#telas-e-rotas-1)
  - [Principais endpoints](#principais-endpoints-1)
- [Módulo Financeiro](#módulo-financeiro)

---

## Módulo Comercial

Cobre todo o funil comercial: da entrada do lead ao fechamento (ganho/perdido) e à conversão da proposta em campanha. Reúne pipeline visual, gestão de oportunidades, propostas com itens tipados (entregável/direitos de uso) e precificação fixa ou variável (comissão/performance), desconto/prazo e total líquido, versionamento, link público com PDF e **aceite eletrônico do cliente**, painel de engajamento, gate de aprovação por política comercial, follow-ups, metas, analytics e geração automática de lançamentos financeiros no fechamento.

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
- Detalhe da proposta com os itens, o cartão de **desconto/prazo** e o painel de **compartilhamento** (links públicos, engajamento e versões).
- Itens da proposta com creator opcional, prazo de entrega e total calculado; cada item tem um **tipo** (entregável ou direitos de uso) e um **modelo de preço** (fixo por quantidade × valor unitário, ou variável por comissão/performance). O total bruto da proposta é a soma dos itens (itens variáveis entram como estimativa). Detalhes em [Itens: tipos, direitos de uso e precificação](#itens-tipos-direitos-de-uso-e-precificação).
- **Templates de proposta** reutilizáveis, aplicáveis a uma proposta via modal.
- **Versionamento**: cada envio registra uma versão com snapshot completo (JSON) para rastreabilidade.
- **Link público**: gera um link com token (sem login) para o cliente visualizar a proposta, com expiração opcional e revogação. O token carrega o tenant de origem, permitindo que os endpoints públicos anônimos resolvam o banco correto em ambiente multi-tenant. A página pública (`/p/:token`) permite ao cliente **aceitar ou recusar** a proposta (ver [Aceite do cliente no link público](#aceite-do-cliente-no-link-público)) e alimenta o [painel de engajamento](#engajamento-da-proposta).
- **PDF**: geração do PDF da proposta via navegador headless (PuppeteerSharp), com a **logo da agência** embutida e o layout do template ativo. Disponível tanto no detalhe autenticado quanto no link público (botão "Baixar PDF").
- **Envio** por e-mail ou WhatsApp (cria/garante o link público, marca a proposta como enviada e enfileira o envio no IntegrationPlatform).
- **Conversão**: proposta aprovada pode ser convertida em uma campanha existente ou gerar uma nova campanha. A conversão também gera, no módulo financeiro, o **recebível da marca** (total líquido) e um **repasse de creator planejado** por item com creator associado (valor estimado), dando visibilidade de margem desde o início da campanha.

**Ciclo de vida (`ProposalStatus`)**

```
Draft → Sent → Viewed → Approved → Converted
          │       │          │
       Expired  Rejected   Cancelled
```

- `Draft` (rascunho) → `Sent` (enviada) → `Viewed` (visualizada) → `Approved` (aprovada) → `Converted` (virou campanha).
- `Expired` (validade vencida), `Rejected` (rejeitada) e `Cancelled` (cancelada) são terminais.
- A passagem para `Approved`/`Rejected` pode vir tanto de uma ação interna (botão Aprovar/Rejeitar no detalhe) quanto da **decisão do próprio cliente no link público** (ver [Aceite do cliente no link público](#aceite-do-cliente-no-link-público)).

### Itens: tipos, direitos de uso e precificação

Cada item da proposta tem um **tipo** (`ProposalItemKind`) e um **modelo de preço** (`ProposalItemPricingModel`), além do creator opcional, prazo de entrega e observações.

**Tipo do item (`ProposalItemKind`)**

- `Deliverable` (entregável): o item representa uma entrega (ex.: Reels, Stories).
- `UsageRights` (direitos de uso): o item representa licenciamento de uso do conteúdo, com **duração** em meses (`UsageDurationMonths`; vazio = perpétuo) e **escopo** (`UsageScope`, ex.: "paid social"). Exibido com selo próprio na proposta pública.

**Modelo de preço (`ProposalItemPricingModel`)**

- `Fixed` (fixo): total = `quantidade × valor unitário` (modelo padrão).
- `Commission` (comissão) e `Performance` (performance): remuneração **variável**, calculada como `taxa% × base estimada` (`VariableRate` × `VariableBasis`). O total do item é uma **estimativa**, marcada como tal na proposta pública (selo "Variável" com a taxa, e o total identificado como estimativa). Permite modelos híbridos (ex.: fee fixo de um item + comissão de outro) na mesma proposta.

**Rate cards**

- Cada creator pode ter um **rate card** reutilizável (`RateCardItem`: rótulo + valor unitário). No modal de item da proposta, ao selecionar o creator, seus itens de rate card aparecem como atalhos que preenchem descrição e valor — evitando redigitar preços recorrentes.

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

### Aceite do cliente no link público

Diferente da aprovação **interna** (gate de política, acima), o cliente final pode **aceitar ou recusar** a proposta diretamente no link público, sem login.

- Enquanto a proposta está `Sent` ou `Viewed`, a página pública (`/p/:token`) oferece as ações de **aceitar** e **recusar**.
- É um aceite eletrônico do tipo **clickwrap**: o cliente confirma informando o **nome** (e-mail e observação são opcionais).
- A decisão registra, na proposta: quem decidiu (`ClientDecisionByName`/`ClientDecisionByEmail`), quando (`ClientDecisionAt`), o **número da versão** decidida (`ClientDecisionVersionNumber`) e um **hash SHA-256 do snapshot enviado** (`ClientDecisionContentHash`) — prova de exatamente o que foi acordado, imune a edições posteriores na proposta viva.
- Aceitar leva a proposta a `Approved`; recusar leva a `Rejected`. Em ambos os casos o responsável é notificado.
- A operação é **idempotente**: uma proposta que já recebeu decisão (ou que esteja `Approved`/`Rejected`/`Cancelled`/`Expired`) não é mais decidível pelo link, e uma nova tentativa responde com conflito (HTTP 409).

### Engajamento da proposta

O painel de **compartilhamento** (aba do detalhe da proposta) consolida o engajamento de **todos** os links públicos da proposta.

- Métricas: total de aberturas, links ativos, primeiro e último acesso.
- **Linha do tempo** dos últimos acessos (até 50 eventos), cada um classificado por dispositivo (celular/tablet/computador) a partir do user agent. Os IPs são anonimizados.
- O **primeiro acesso** ao link marca a proposta como `Viewed` automaticamente e notifica o responsável (a marcação manual continua disponível).

### Follow-ups

- Agendamento de ações de acompanhamento por oportunidade: assunto, data de vencimento e notas; marcação de concluído.
- Tela dedicada com abas por situação (atrasados, hoje, próximos, concluídos) e acesso direto à oportunidade relacionada. Indicadores de atraso aparecem também no pipeline e no detalhe da oportunidade.

### Metas comerciais

- Metas por período (mensal, trimestral ou anual), globais (agência) ou por usuário, com valor-alvo.
- Cálculo de **progresso** comparando o realizado (oportunidades ganhas no período) com o alvo.
- Widget de metas no painel/insights com barra de progresso e cores por nível de atingimento.

### Painel, forecast e analytics

- **Board (kanban)**: oportunidades agrupadas por estágio, com contagem e valor por coluna.
- **Forecast**: previsão de receita do período combinando o **ganho** (oportunidades já fechadas, pelo **valor fechado real**) com o **ponderado em aberto** (soma de `valor estimado × probabilidade/100` das oportunidades abertas), além da quebra por estágio (quantidade, valor, valor ponderado e probabilidade média).
- **Valor fechado** (`ClosedValue`): ao fechar uma oportunidade como ganha, o sistema captura o **total líquido da proposta aceita** como valor fechado da oportunidade. As métricas (forecast, analytics, metas) usam o valor fechado quando disponível e caem para o valor estimado quando não há proposta aceita — o que reflete a receita real, e não só a expectativa inicial.
- **Analytics**: oportunidades fechadas (ganhas/perdidas), taxa de ganho, ciclo médio, conversão e tempo médio por estágio, top performers e motivos de ganho/perda.
- **Insights**: próximos fechamentos e oportunidades em risco (aging), além de alertas comerciais.

### Rotinas automáticas

Tarefas em segundo plano (hosted services) rodam por tenant e mantêm o funil em dia sem ação manual:

- **Expiração de propostas**: marca como `Expired` as propostas enviadas cuja validade venceu.
- **Lembrete de expiração**: avisa o responsável quando uma proposta enviada/visualizada está perto de expirar (janela de alguns dias), para fazer follow-up ou estender a validade. Um lembrete por ciclo de envio (reenviar a proposta rearma o lembrete).
- **Follow-up vencido**: notifica o responsável quando um follow-up agendado passa do vencimento.
- **Oportunidade parada**: notifica quando uma oportunidade ultrapassa o SLA do estágio (tempo parado além do previsto).

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
| `/comercial/propostas/:id` | `ProposalDetail.tsx` | Detalhe da proposta (itens, desconto/prazo, compartilhamento e engajamento) |
| `/comercial/aprovacoes` | `Approvals.tsx` | Caixa de solicitações de aprovação |
| `/comercial/followups` | `FollowUps.tsx` | Follow-ups por situação |
| `/comercial/metas` | `Goals.tsx` | Metas comerciais |
| `/comercial/analytics` | `Analytics.tsx` | Indicadores e análises do funil |
| `/p/:token` | `Public/Proposal.tsx` | Visualização, aceite e recusa pública da proposta (sem login) |

Principais modais: criar/editar oportunidade, follow-up, solicitação de aprovação (na proposta), proposta, item de proposta, envio de proposta, aplicação de template, meta comercial e configuração de estágio/motivos.

### Principais endpoints

Todos protegidos por `[RequireAccess]`, exceto os públicos de proposta (`[AllowAnonymous]`). Rotas relativas à API do AgencyCampaign.

- **Oportunidades** (`/opportunities`): listar, `mine`, detalhe, `board`, `forecast`, `analytics`, `insights`, `StageHistory`; criar, atualizar, excluir; `ChangeStage`, `CloseAsWon`, `CloseAsLost`. O mesmo controller agrupa, como sub-recursos da oportunidade, os comentários e os follow-ups. A probabilidade não tem endpoint nem campo de entrada: é derivada do estágio e usada só para ponderar o forecast.
- **Propostas** (`/proposals`): listar, detalhe, `pdf/{id}`; criar, atualizar (incluindo desconto/prazo); itens (criar/editar/remover, com tipo e modelo de preço); `SendByEmail`, `SendByWhatsapp`, `MarkAsSent`, `MarkAsViewed`, `Approve`, `Reject`, `ConvertToCampaign`, `ConvertToNewCampaign`, `Cancel`, `StatusHistory`; links de compartilhamento (criar/listar/revogar), engajamento (`engagement/Get`) e versões (listar). Não há exclusão de proposta pela API.
- **Proposta pública** (`/proposal-public/{token}`, `/proposal-public/{token}/pdf`, `/proposal-public/{token}/accept`, `/proposal-public/{token}/reject`): acesso anônimo por token (o token carrega o tenant, resolvendo o banco correto em multi-tenant). A consulta registra a visualização e o PDF é gerado sob demanda; `accept`/`reject` registram a decisão do cliente de forma idempotente.
- **Aprovações** (`/opportunityApprovals`): listar, por proposta; criar; `Approve`, `Reject`, `MarkInReview`, `RequestChanges`, `Resubmit`, `MarkMerged`; decisão de revisor (`Reviewers/Decision`); `Comments` (CRUD); `Reviewers`, `Diffs` e `Impacts` (gerenciáveis); `evaluate-policy` e `PopulateFromPolicy` (avaliam/regeneram os desvios pela política).
- **Política comercial** (`/commercialPolicy`): obter e atualizar (upsert).
- **Estágios** (`/commercialPipelineStages`): listar, `active`, detalhe, criar/atualizar/excluir.
- **Metas** (`/commercialGoals`): listar, `progress`, criar/atualizar/excluir.
- **Fontes** (`/opportunitySources`): listar, criar/atualizar/excluir.
- **Templates de proposta** (`/proposalTemplates`): listar, detalhe, criar/atualizar, versões.

---

## Módulo de Produção

### Visão geral da produção

Cobre a execução da campanha fechada: alocação e cachê dos creators, entregáveis (prazos, métricas, gate de aprovação, máquina de estados), revisão de conteúdo (rodadas e comentários, agência e marca), contratos com assinatura digital e lastro próprio, portal do creator e relatório público para a marca. O custo do creator concilia com o módulo financeiro (repasses por creator).

O modelo é **centrado no entregável**: a campanha nasce da proposta convertida (que já semeia os creators) e o entregável é a unidade de trabalho — com **gate de aprovação** antes de publicar/pagar, **conciliação financeira por creator** e **máquina de estados** que protege o que já gerou repasse. Não se publica conteúdo sem a aprovação exigida pela campanha.

Há três superfícies externas, sem login e por token (o token carrega o prefixo do tenant, resolvendo o banco correto em multi-tenant): o **portal do creator** (`/portal/:token`), a **aprovação do entregável pela marca** (`/d/:token`) e o **relatório da campanha para a marca** (`/r/:token`).

### Campanhas e creators na campanha

**Campanha (`Campaign`)**

- Marca, nome, orçamento, período, objetivo e **briefing estruturado** (`CampaignBriefing` — mensagem-chave, pode/não-pode, hashtags, menções, links de referência). O antigo campo de briefing livre foi **deprecado e consolidado** no estruturado (a aba de briefing é a entrada única; documentos e portal leem dele).
- Flag por campanha `RequiresDeliverableApproval` (default ligado) que controla o **gate de publicação**.
- Nasce da **conversão de uma proposta ganha** (que semeia um `CampaignCreator` por creator dos itens) ou de cadastro manual.

**Creator na campanha (`CampaignCreator`)**

- Liga creator e campanha com o **cachê** (`AgreedAmount`) e o **percentual de fee da agência** (`AgencyFeePercent`), **corrigível após a criação** — recalcula o valor do fee.
- **Status** configurável (`CampaignCreatorStatus`) por **flags semânticas** (inicial/confirmado/cancelado), nunca por comparação de nome, com histórico de mudanças. Transições para confirmado/cancelado geram notificação.
- **Atribuição de vendas**: cupom, URL de rastreio, pedidos e receita atribuídos.

### Entregáveis e métricas

**Funcionalidades**

- Entregável (`CampaignDeliverable`) com título, tipo (`DeliverableKind`), plataforma, prazo, e valores financeiros (bruto/creator/fee, com a **soma validada** — repasse + fee não pode exceder o bruto).
- **SLA** por prazo (indicadores atrasado / a vencer) e **lembrete automático** de prazo (job).
- A publicação exige `publishedUrl` e passa pelo **gate de aprovação** da campanha.

**Ciclo de vida e máquina de estados (`DeliverableStatus`)**

```
Pendente ⇄ Em revisão ⇄ Aprovado ──→ Publicado
   │           │            │             │
   └───────────┴────────────┴─────────────┴──→ Cancelado
```

- Os estados **ativos** (Pendente, Em revisão, Aprovado) transitam **livremente entre si** e para Publicado (sujeito ao gate) ou Cancelado.
- **Publicado** não volta para um estado ativo — lastro/repasse podem já ter ocorrido; só pode ser **Cancelado**.
- **Cancelado** é terminal (não reabre). Transição inválida é rejeitada (`deliverable.status.invalidTransition`).

**Métricas**

- Alcance, impressões, views, curtidas, comentários, compartilhamentos, salvamentos e taxa de engajamento, com a **origem rotulada** (`DeliverableMetricsSource`: manual vs insights do creator vs nenhuma).
- A edição faz **merge campo a campo** (editar uma métrica não zera as demais).
- Alcance/impressões vêm dos **insights do creator** (enviados pelo portal); a coleta social centralizada (Apify, bancada pela Mainstay) é planejada e **não** passa pelo IntegrationPlatform.

### Aprovação de entregáveis e revisão de conteúdo

**Aprovação da marca no link público**

- O link de compartilhamento do entregável (`/d/:token`) permite à marca **aprovar, reprovar, pedir alterações e comentar** sem login. A autoria vem do próprio link (anti-forja, como na proposta).

**Gate de publicação (`RequiresDeliverableApproval`)**

- Configurável por campanha (default obrigatório): exige uma **aprovação da marca** registrada antes de publicar/pagar o creator. Desligável por campanha quando o fluxo não precisa de revisão da marca.

**Revisão de conteúdo (`DeliverableContentVersion`)**

- Rodadas **versionadas** com comentários **internos** (só agência) ou **compartilhados** (com a marca).
- A agência pode **aprovar internamente** (sem enviar para a marca) para satisfazer o gate em conteúdo simples.
- Assets (imagem, **PDF**, **vídeo**) são privados e servidos por **URL assinada** (ver [Mídia privada](#mídia-privada)). A caixa de aprovações pendentes (`/operacao/aprovacoes`) lista o que aguarda decisão.

**Licença de conteúdo (`ContentLicense`)**

- Direitos e prazo de uso por entregável, com job de expiração.

### Documentos e assinaturas

**Funcionalidades**

- Documento (`CampaignDocument`) **gerado a partir de templates** (substituição de variáveis **validada** — placeholders desconhecidos são rejeitados, sem campos em branco silenciosos) ou anexado.
- Enviado para assinatura pelo **conector configurado** no IntegrationPlatform (categoria de assinatura digital).

**Ciclo de vida (`CampaignDocumentStatus`)**

```
Rascunho → Pronto p/ envio → Enviado → Visualizado → Assinado
                                 │            │
                             Rejeitado    Cancelado
```

- Guiado pelo **callback do provedor** (eventos `created`/`sent`/`viewed`/`signer.signed`/`completed`/`cancelled`), com assinatura por signatário (e-mail, id do provedor).

**Lastro próprio de não-adulteração**

- No envio, o corpo é **selado com hash SHA-256 imutável** (registrado num evento append-only `ContentSealed` com timestamp do servidor); `VerifyContentIntegrity` detecta adulteração posterior (`NotSealed`/`Intact`/`Tampered`).
- A **URL de assinatura por signatário** (quando o provedor a devolve) é capturada para o creator assinar pelo portal; o **IP do signatário** é guardado como evidência. (Espelha o aceite clickwrap + hash do snapshot do módulo Comercial.)

### Mídia privada

Mídia sensível (NF, peças de revisão, vídeo) **não** é servida como arquivo estático público. É gravada num diretório privado (fora do `wwwroot`) e servida apenas por um endpoint autorizado (`/api/media`) através de **URLs assinadas (HMAC) de curta duração** com o token na query — exibe em `<img>`/vídeo/PDF sem header de autenticação. A chave de armazenamento embute o tenant e o backend só assina chaves que o solicitante tem direito de ver. Logos/avatares públicos continuam no armazenamento de imagem separado.

### Portal do creator (acesso por token)

**Acesso e segurança (`CreatorAccessToken`)**

- Token por creator: **CSPRNG**, com **prefixo de tenant**, **expiração obrigatória** (default 30 dias, teto 90), **revogável** e à prova de IDOR.
- As páginas públicas usam um **cliente HTTP dedicado** que **não** redireciona para o login da agência no 401; um link expirado/inválido mostra a tela própria de "link inválido". Uma faixa avisa quando o acesso está perto de expirar.

**Superfícies**

- **Campanhas**: o fee da agência fica **oculto** (o creator vê só o cachê), com o briefing estruturado.
- **Contratos**: status de assinatura e botão **"Assinar"** quando há URL de assinatura do provedor.
- **Entregáveis**: enviar insights/métricas, versões de conteúdo e **upload de mídia** (incl. vídeo).
- **Pagamentos**: **upload do arquivo da NF** (PDF) e dados bancários (Pix).
- Layout responsivo (o público é majoritariamente mobile).

### Rotinas automáticas da produção

Tarefas em segundo plano (hosted services) rodam por tenant:

- **Lembrete de prazo de entregável**: avisa entregáveis vencidos ou a vencer (janela de dias), deduplicado por entregável e reposto quando o prazo é remarcado.
- **Expiração de licença de conteúdo**: sinaliza licenças vencidas.
- **Sync social**: coleta de seguidores/insights quando configurado.
- Notificações best-effort usam **log estruturado** (sem engolir falha em silêncio). O conceito de **Automações** (gatilhos → pipelines do IntegrationPlatform) liga eventos de negócio às integrações.

### Configurações da produção

- **Status de creator na campanha** (`CampaignCreatorStatus`): catálogo com cor, ordem e flags semânticas (inicial/confirmado/cancelado); podem ser desativados preservando o histórico.
- **Tipos de entregável** (`DeliverableKind`): catálogo de formatos (ex.: Reels, Stories).
- **Plataformas / redes sociais** (`Platform`): YouTube, Instagram, TikTok etc., usadas em entregáveis e creators.
- **Modelos de contrato** (`CampaignDocumentTemplate`): templates reutilizáveis com variáveis, editáveis em tela própria.

### Telas e rotas

A UI rotula este módulo como **Produção**; parte das rotas mantém, no código, o prefixo legado `operacao/`.

| Rota | Tela | Função |
|------|------|--------|
| `/campanhas` | `Campaigns.tsx` | Lista de campanhas |
| `/campanhas/:id` | `CampaignDetail.tsx` | Detalhe com abas (creators, entregáveis, documentos, briefing, conteúdo, relatório) |
| `/operacao/aprovacoes` | `Approvals.tsx` | Caixa de aprovações de entregáveis pendentes |
| `/operacao/calendario` | `Calendar.tsx` | Calendário de entregas |
| `/creators`, `/creators/:id` | `Creators.tsx` / `CreatorDetail.tsx` | Creators (rate card, redes sociais, audiência) |
| `/marcas` | `Brands.tsx` | Marcas e contatos |
| `/portal/:token` | `CreatorPortal/Layout.tsx` | Portal público do creator (abas: início, campanhas, resultados, contratos, pagamentos, conteúdo, perfil) |
| `/d/:token` | `Public/Deliverable.tsx` | Aprovação pública do entregável pela marca (sem login) |
| `/r/:token` | `Public/CampaignReport.tsx` | Relatório público da campanha para a marca (sem login) |

Principais modais/sheets: criar/editar campanha, alocar creator na campanha, criar/editar entregável, registrar métricas, sheet de revisão de conteúdo, licenças do entregável, gerar/enviar documento, aplicar template, criar link de compartilhamento e emitir token de acesso do creator.

### Principais endpoints

Todos protegidos por `[RequireAccess]`, exceto os públicos por token (`[AllowAnonymous]`, com o tenant resolvido pelo prefixo do token). Rotas relativas à API do AgencyCampaign.

- **Campanhas** (`/campaigns`): listar, detalhe, criar/atualizar/excluir; `/campaignBriefing` para o briefing estruturado.
- **Creators na campanha** (`/campaignCreators`): listar por campanha, detalhe, criar/atualizar (cachê e fee), `SetSalesAttribution`, histórico de status; `/campaignCreatorStatuses` para o catálogo de status.
- **Entregáveis** (`/campaignDeliverables`): listar por campanha, criar/atualizar, mudar status/publicar (com o gate), registrar métricas; `/deliverableMetrics` e `/deliverableKinds` (tipos).
- **Aprovação da marca** (`/deliverableShareLinks`): criar/listar/revogar link; `/deliverablePendingApprovals/pending`; público em `/deliverable-public/{token}` (consultar, `approve`, `reject`, `request-changes`, `comment`).
- **Revisão de conteúdo** (`/contentReview`): obter por entregável, `upload` de asset, adicionar versão, `request-changes`, `send-to-brand`, `agency-approve`, comentar; `/contentLicense` para licenças.
- **Documentos** (`/campaignDocuments`): listar por campanha, detalhe, criar, `GenerateFromTemplate`, `SendForSignature`, `SendEmail`, `SendWhatsapp`, `MarkSigned`, `verify-integrity/{id}`, `ProviderCallback` (anônimo); `/campaignDocumentTemplates` para os modelos.
- **Relatório** (`/campaignReports/campaign/{id}`): criar/obter link e `revoke`; público em `/campaign-report-public/{token}` (oculta custo/margem, mostra performance/EMV/ROI).
- **Portal do creator** (`/creatorPortal/{token}/...`, anônimo): `me`, `campaigns`, `documents`, `deliverables`, `payments`, `bank-info`, `invoice` e `invoice/upload`, `deliverables/{id}/review|version|comment|upload|insights`; `/creatorAccessTokens` (agência) emite e revoga tokens (`creator/{creatorId}/{id}/revoke`).
- **Mídia privada** (`/api/media?t=`, anônimo por assinatura): serve o arquivo privado validando o token HMAC e a expiração.
- **Creators e marcas** (`/creators`, `/creatorAudience`, `/creatorSocialHandles`, `/rateCardItems`, `/brands`, `/brandContact`): cadastro e dados de apoio.

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
