# Correcoes do Modulo Comercial - Kanvas (tracker)

Checklist vivo derivado de `analise-modulo-comercial.md` (2026-05-30). Os ~115 achados brutos foram consolidados em itens acionaveis e agrupados em fatias na ordem de execucao recomendada. Conforme cada item e resolvido, marcar `[x]` e atualizar o contador.

## Como usar

- `[ ]` pendente
- `[x]` concluido (com commit)
- `[~]` em andamento
- `[-]` descartado / decisao de nao fazer (registrar o porque na linha)
- `[?]` bloqueado por decisao de produto (ver secao "Decisoes de produto")

Cada item segue o formato: **[Severidade]** Titulo - problema -> correcao pretendida. _(area)_

## Progresso geral

- Total de itens: 57
- Concluidos: 22 / 57 (Fatia A + C3-C7 + D1i, D3i, D6i, D7i, D18i, D25i, D26i)
- Por fatia: A 10/10 - B 0/5 - C 5/7 - D 7/29 - E 0/6
- Fatia D: triagem paralela feita (29 itens, premissas validas). Lotes feitos: D7i, D25i, D26i, D3i, D1i, D18i (TDD) + D6i (logging). 3 jobs comerciais novos. D24i adiado (escopo global). Backend 888 testes verdes; Api builda.
- Fatia A verificada: backend 874 testes verdes; frontend `tsc -b` limpo. Build vite local bloqueado por binario nativo do rolldown (ambiente), CI builda normal.
- Fatia C: C3, C4, C5, C6, C7 feitos (backend 882 testes verdes; build do Api OK). C2 (rate limit) REMOVIDO a pedido do usuario - ele fara algo mais robusto. CORS nao mexido. C1 (multi-tenant do link) bloqueado por D4.

## Decisoes operacionais registradas (2026-05-30)

- Cadencia: commit + push por fatia, direto na main (cada fatia = 1 deploy).
- A1: o liquido vale para o recebivel E para o Budget da campanha gerada.
- D1 (margem): remover o campo inerte agora (ver A10).

---

## Decisoes de produto (resolver antes de implementar os itens marcados [?])

- [x] **D1 - Margem minima:** DECIDIDO - remover o campo inerte da politica por ora (reintroduzir quando a proposta carregar custo do creator). Implementado na Fatia A (A10).
- [ ] **D2 - Aceite digital:** botao Aceitar/Recusar com data/identidade/versao basta no MVP, OU ja queremos assinatura eletronica vinculante com trilha de auditoria? Bloqueia B1.
- [ ] **D3 - Valor fechado:** ao ganhar, sobrescrever o `EstimatedValue` da oportunidade pelo liquido da proposta aceita, OU manter estimado vs. fechado em campos separados? Bloqueia B2.
- [ ] **D4 - Deploy multi-tenant:** confirmar instancia unica compartilhada (entao corrigir resolvendo tenant pelo proprio token publico). Bloqueia C1.
- [ ] **D5 - Roadmap de nicho:** usage rights, modelos hibridos e rate cards entram no pre-lancamento ou ficam pos-MVP? Define se a Fatia E e agendada agora.

---

## Fatia A - Furos de dinheiro e governanca (Etapa 1 / quick wins)

Baixo risco, alto impacto. Estanca o "sangramento" financeiro e fecha os furos de governanca mais visiveis.

- [x] **A1 - [Critico] Liquido na conversao (recebivel + Budget)** - recebivel e Budget da campanha usavam o total BRUTO, ignorando o desconto -> agora usam `NetTotalValue` no recebivel (FinancialAutoGenerationService) e no Budget da campanha (ProposalService.ConvertToNewCampaign). Coberto por 2 testes novos (TDD: RED->GREEN). _(conversao proposta->campanha)_
- [x] **A2 - [Alto] Bloquear o botao Aprovar quando ha desvio de politica sem aprovacao concedida** - o banner avisa "aprovacao obrigatoria" mas o botao continua habilitado, tornando o gate contornavel -> desabilitar o Aprovar usando a flag de "precisa aprovacao" que ja existe. _(tela de propostas / detalhe)_
- [x] **A3 - [Alto] Guarda de reabertura no ChangeStage** - BACKEND PRONTO: dominio exige `allowReopen` explicito (lanca `opportunity.reopen.confirmationRequired`), e a reabertura confirmada limpa data/motivos/ids orfaos; DTO + service threadam o flag; 2 testes TDD. UI: confirmacao no kanban ao reabrir (com toast de erro no move); no detalhe a reabertura ja fica bloqueada pelo backend. _(oportunidade / kanban)_
- [x] **A4 - [Medio] Mascarar notas internas no snapshot publico** - BACKEND: o snapshot publico agora remove a chave `notes` (notas internas) na fronteira do servico publico; teste TDD. NOTA: a premissa de "custo por creator" NAO se confirmou - itens de proposta so tem preco de venda (unitPrice/total), que o cliente deve ver mesmo; proposta nao carrega custo. Verificar na UI publica se nao sobra bloco vazio de notas. _(link publico /p/:token)_
- [x] **A5 - [Medio] Toast de erro ao falhar o move no kanban** - em erro de rede/permissao o card "volta sozinho" sem feedback -> exibir toast de erro como ja ocorre no fechamento final. _(kanban)_
- [x] **A6 - [Medio] Validar coerencia da politica + avisar politica vazia** - BACKEND PRONTO: dominio lanca `commercialPolicy.paymentTerm.defaultExceedsMax` quando prazo padrao > maximo (3 testes TDD). UI: validacao inline de prazo (bloqueia salvar) + aviso de politica vazia na tela. _(config politica comercial)_
- [x] **A7 - [Baixo] Padronizar formatacao de moeda/data via helpers centralizados** - lista mostra "R$ 1500.00" em vez de "R$ 1.500,00" -> usar `src/lib/format`. _(lista de oportunidades)_
- [x] **A8 - [Alto] Sinal "fora da politica" vs "aguarda ok" no inbox de aprovacoes** - distincao so aparece dentro do painel de diffs -> mostrar badge de "fora da politica" na lista/cabecalho. _(inbox de aprovacoes)_
- [x] **A9 - [Alto] Testes nos pontos de dinheiro** - coberto pelos testes do A1 (recebivel liquido + Budget liquido) e do A4 (snapshot publico sem notas). +7 testes novos no total da fatia. _(testes)_
- [x] **A10 - [Alto] Remover margem minima inerte da politica** - BACKEND PRONTO: removida da entidade, EF config, model, request e service; migration 202605300001 dropa a coluna `minmarginpercent`; testes ajustados (teste "ignora margem" deletado por obsolescencia). UI: campo de margem removido da tela + type + service do front. _(politica comercial)_

---

## Fatia B - Fechamento real e inteligencia confiavel (Etapa 2)

Transforma "registro manual" em "funil rastreavel". Itens com decisao de produto.

- [ ] **B1 - [Critico] Aceite digital do cliente no link publico** `[?] D2` - a pagina publica e somente leitura; aceitar/recusar e definido a mao pelo operador -> permitir aceitar/recusar/comentar com captura de data, identidade e versao (idealmente assinatura com trilha). _(link publico + dominio proposta)_
- [ ] **B2 - [Alto] Reconciliacao do valor negociado** `[?] D3` - metas/forecast/analytics usam o valor estimado inicial, nunca o liquido negociado -> reconciliar o liquido da proposta aceita de volta na oportunidade e nas metricas. _(oportunidade / analytics / metas)_
- [ ] **B3 - [Alto] "Visualizada" automatica a partir do acesso real ao link** - hoje "visualizada" e clique manual do operador -> promover o status quando o cliente abre o link e notificar o operador ("cliente abriu"). _(link publico + proposta)_
- [ ] **B4 - [Alto] Clarear os dois modelos de "aprovar"** - aprovar a PROPOSTA (status) e aprovar o PEDIDO de excecao (revisores) se confundem -> diferenciar rotulos/fluxo na UI para o operador nao misturar. _(propostas / aprovacoes)_
- [ ] **B5 - [Medio] "Solicitar aprovacao" a partir da oportunidade** - o modal existe mas nenhum botao o aciona (codigo morto) -> ligar o botao na oportunidade OU remover o codigo morto. _(detalhe da oportunidade)_

---

## Fatia C - Go-to-market SaaS e seguranca (Etapa 3)

Pre-requisito para mais de uma agencia no mesmo deploy e para nao ficar exposto a exaustao de recursos.

- [ ] **C1 - [Alto] Resolver link publico multi-tenant** `[?] D4` - endpoints publicos anonimos nao carregam tenant e caem no primeiro tenant configurado; link so funciona para a 1a agencia -> resolver tenant pelo proprio token. _(endpoints publicos)_
- [ ] **C2 - [Alto] Rate limiting nos endpoints publicos** - REMOVIDO (2026-05-31): a versao simples (fixed-window por IP, .NET nativo) foi removida a pedido do usuario, que vai implementar algo mais robusto. CAVEAT para a nova versao: atras de proxy/Docker, ler o IP real via `X-Forwarded-For` (`UseForwardedHeaders`) - senao o limite por IP fica inocuo (todos caem no IP do proxy). Considerar tambem sliding window/token bucket e limites em `appsettings`. CORS segue nao priorizado. _(API publica)_
- [x] **C3 - [Alto] Pool/fila/timeout para o Chromium do PDF** - FEITO: o `ProposalPdfService` agora reusa um unico navegador Chromium (estatico, lazy, com health-check e relancamento se cair) em vez de subir/descartar um por requisicao; concorrencia limitada por `SemaphoreSlim` (3 simultaneos) e timeout de 30s no render. Sem teste unitario (render real exige Chromium). _(geracao de PDF)_
- [x] **C4 - [Alto] Reforcar o gate de aprovacao no backend** - FEITO: Approve/Reject/RequestChanges agora derivam a autoria do usuario autenticado (`currentUser`), ignorando o nome do corpo (sem mais forja de aprovador); dominio bloqueia Approve/Reject direto quando ha revisores obrigatorios (forca a votacao); GetTrackedApproval passa a incluir Reviewers. 2 testes TDD. PARCIAL: alcada por papel/threshold (ex.: so gestor aprova acima de X) nao implementada - hoje o `[RequireAccess]` do controller gateia quem pode chamar; alcada granular fica como follow-up. _(aprovacoes)_
- [x] **C5 - [Medio] Fechar reabertura pos-merge / reenvio bloqueado** - FEITO: o gate de envio agora conta aprovacao `Merged` como valida (alem de `Approved`), entao reenviar apos "marcar como aplicada" passa sem recriar aprovacao. BONUS: descoberto e corrigido bug pre-existente - `GetTrackedApproval` exigia `Pending`, quebrando `MarkMerged`/`Resubmit` (nunca testados); guard de Pending movido para o dominio (Approve/Reject), liberando as transicoes corretas. 1 teste TDD (reenvio pos-merge). _(aprovacoes / envio)_
- [x] **C6 - [Medio] IDOR intra-tenant na revogacao de share link** - FEITO: revoke aninhado sob o proposal (rota `{id}/share-links/{shareLinkId}/Revoke`) e query valida que o link pertence ao proposal antes de revogar; service/interface/controller/front/ProposalShareTab atualizados. 1 teste TDD (IDOR). _(share link)_
- [x] **C7 - [Medio] Expiracao de proposta e de link publico** - FEITO (3 partes): (1) `GetByToken` bloqueia propostas Rejeitadas/Canceladas/Expiradas e com `ValidityUntil` vencido; (2) `ProposalService.ExpireOverdue` + `ProposalExpiryJob` (hosted service, tick 12h, por tenant) marcam propostas Sent vencidas como Expired; (3) link gerado sem expiracao explicita agora nasce com default de 30 dias. 5 testes TDD no total. _(proposta / link publico)_

---

## Fatia D - Operacao diaria e robustez (Etapa 4)

Mantem o operador usando e o funil confiavel. Inclui performance, consistencia e polimento.

- [x] **D1i - [Alto] Lembrete proativo de follow-up** - FEITO: `FollowUpReminderJob` (hosted service, tick 6h, por tenant) chama `OpportunityFollowUpService.RemindDue`, que notifica o responsavel (KanvasNotifications.FollowUpDue) dos follow-ups vencidos ou que vencem hoje, de oportunidades ABERTAS. Dedup via `ReminderSentAt` na entidade (migration 202605310002), resetado ao remarcar a data - um lembrete por vencimento, sem spam. 1 teste TDD. _(follow-ups)_
- [ ] **D2i - [Medio] Controle de concorrencia otimista** - transicoes gravam direto (last-write-wins) -> token de versao retornando conflito em vez de sobrescrever. _(oportunidade / proposta)_
- [x] **D3i - [Medio] Snapshot de versao completo (congelar desconto)** - FEITO: ProposalVersion ganhou colunas `DiscountAmount`/`NetTotalValue` (nullable, migration 202605310001); `CreateSentVersionAsync` congela o desconto da proposta no envio; `ProposalPublicService.GetByToken` le o desconto CONGELADO da versao (fallback ao vivo so para versoes legadas). Editar o desconto depois de enviado nao muda mais o que o cliente ve pelo link. 1 teste TDD. _(versionamento de proposta)_
- [ ] **D4i - [Medio] Paridade de motivos estruturados no fechamento via kanban** - arrastar para Ganho/Perdido nao coleta motivo estruturado (so texto livre), detalhe coleta -> oferecer os mesmos motivos cadastrados no fechamento pelo kanban. _(kanban)_
- [ ] **D5i - [Medio] i18n da pagina publica e do modal de envio** - strings hardcoded em pt-BR quebram para clientes es-AR/en-US -> internacionalizar. _(link publico / envio)_
- [x] **D6i - [Medio] Tratar falhas silenciosas em pontos sensiveis** - FEITO: os ~8 `Console.WriteLine` em catches best-effort dos services comerciais (ProposalService, ProposalPublicService, OpportunityApprovalRequestService, OpportunityFollowUpService, OpportunityService) + FinancialAutoGenerationService (skip de recebivel/repasse) viraram `logger?.LogWarning(exception, ...)` estruturado em ingles. `ILogger<T>` injetado como OPCIONAL (DI injeta em producao; testes passam null) -> zero churn de teste. Mantida a semantica best-effort (nao re-lanca), so melhora a observabilidade. _(varios services)_
- [x] **D7i - [Medio] Reenvio sem guarda de estado** - FEITO: dominio `Proposal.MarkAsSent` so permite enviar de Draft/Sent/Viewed (lanca `proposal.send.invalidStatus` para Approved/Convertida/Rejeitada/Cancelada/Expirada); `CreateSentVersionAsync` valida ANTES de criar a versao (sem versao orfa). 1 teste TDD. _(proposta)_
- [ ] **D8i - [Medio] Desconto encolhe sozinho ao remover itens** - sem aviso quando o desconto e reduzido por queda do bruto -> avisar o operador. _(proposta)_
- [ ] **D9i - [Medio] Kanban sem estado de carregamento proprio** - tela em branco que "pisca" no primeiro load -> adicionar loading. _(kanban)_
- [ ] **D10i - [Medio] Mover de etapa no mobile direto na lista** - hoje exige abrir o detalhe -> seletor de etapa inline no mobile. _(kanban mobile)_
- [ ] **D11i - [Medio] N+1 ao montar aprovacoes/revisores no detalhe** - varias chamadas com erros silenciados -> agregar em uma chamada / tratar erros. _(detalhe oportunidade / aprovacoes)_
- [ ] **D12i - [Medio] Confirmacao ao excluir item da proposta** - exclusao dispara sem confirmacao -> adicionar confirmacao. _(proposta)_
- [ ] **D13i - [Medio] Widgets de forecast/insights: distinguir vazio de erro** - lista vazia = erro = sem dado, indistinguiveis -> estados separados. _(widgets comerciais)_
- [ ] **D14i - [Medio] Desconto: salvar explicito** - persiste no blur, gerando duvida "ja salvou?" -> botao salvar explicito ou feedback claro de gravacao. _(proposta)_
- [ ] **D15i - [Medio] Remover vocabulario residual de "negociacao"** - termo aposentado ainda aparece na UI de aprovacoes e em rotas -> limpar UI/rotas. _(aprovacoes / rotas)_
- [ ] **D16i - [Medio] Probabilidade manual ajustavel** - dominio suporta mas nenhuma tela expoe; forecast preso a media do estagio -> permitir ajuste manual por oportunidade (opcional). _(oportunidade / forecast)_
- [ ] **D17i - [Medio] Reforcar escopo "minhas oportunidades" no dado** - isolamento so por permissao; erro de papel expoe a carteira inteira -> reforco por dado/owner alem da permissao. _(oportunidades)_
- [x] **D18i - [Medio] Deal rotting (alerta de oportunidade parada)** - FEITO: `OpportunityStalledJob` (hosted service, tick 12h, por tenant) chama `OpportunityService.AlertStalled`, que notifica o responsavel quando uma oportunidade ABERTA fica num estagio alem do `SlaInDays` (reusa a logica de stageEnteredAt do GetAlerts). Dedup via `StaleAlertedAt` na entidade (migration 202605310003), resetado ao mudar de estagio -> um alerta por entrada no estagio. 1 teste TDD. _(pipeline)_
- [-] **D19i - [Medio] Endpoint publico serve a versao do link** - ADIADO (valor marginal): com o D3i feito (desconto congelado por versao) e o modelo de UM link ativo reusado por proposta (EnsureActiveShareLinkAsync) que mostra a ultima versao enviada, servir "a versao do link" so importa no caso raro de MULTIPLOS links manuais apontando para versoes diferentes - e exigiria FK link->versao + lidar com ID pre-save. Reativar se surgir o cenario multi-link. _(link publico)_
- [ ] **D20i - [Baixo] Arredondamento monetario consistente no dominio** - falta padrao de arredondamento -> centralizar regra de arredondamento. _(dominio comercial)_
- [ ] **D21i - [Baixo] Conversao por estagio nao assume funil linear** - calculo assume funil estritamente linear -> tornar robusto a funis nao lineares. _(analytics)_
- [ ] **D22i - [Baixo] Oportunidades sem data prevista somem do forecast sem contador** - desaparecem silenciosamente -> contabilizar/sinalizar. _(forecast)_
- [ ] **D23i - [Baixo] Conversao em nova campanha com modal de revisao** - usa confirm nativo e cria "as cegas" -> modal padrao do produto revisando nome/datas. _(conversao)_
- [-] **D24i - [Baixo] "Nao encontrado" retornar 404 (nao 400)** - ADIADO (escopo global, nao comercial): `record.notFound` e lancado como `InvalidOperationException` (-> 400 no middleware do Archon) em dezenas de services de TODO o codebase, nao so no comercial. O Archon ja tem `NotFoundException` (-> 404); o fix correto e uma passada GLOBAL trocando o padrao (todos os `record.notFound` -> NotFoundException), nao piecemeal no comercial (criaria inconsistencia de status entre modulos). Fazer como tarefa dedicada cross-modulo. _(API / global)_
- [x] **D25i - [Baixo] Soft delete de fontes/motivos** - FEITO: Delete de OpportunitySource/WinReason/LossReason agora faz soft-delete (IsActive=false) em vez de Remove fisico, preservando o vinculo nas oportunidades historicas (GetAll ja filtra inativos). Tags ficaram de fora (join table, nao quebra analytics igual). 2 testes TDD. _(fontes / motivos)_
- [x] **D26i - [Baixo] Meta duplicada com erro tratado** - FEITO: `CommercialGoalService.Create` faz pre-check (mesmo responsavel/periodo, com COALESCE(userid,0) e PeriodStart normalizado) e lanca `ConflictException` (409 tratado) com chave i18n `commercialGoal.duplicate`, em vez do erro cru 500. 1 teste TDD. _(metas)_
- [ ] **D27i - [Baixo] Polimento diverso** - follow-up irreversivel pela UI; lista de aprovacoes 200 fixos sem paginacao; probabilidade do estagio nao exibida no card; papeis de revisor i18n -> ajustes pontuais. _(varios)_
- [ ] **D28i - [Baixo] Acessibilidade** - botoes apenas-icone sem rotulo, barras de progresso sem label -> rotular. _(varios)_
- [ ] **D29i - [Baixo] LGPD: IP/user-agent de visitantes** - coleta sem aviso/retencao/anonimizacao -> politica de retencao/anonimizacao. _(visualizacao publica)_

> Performance/escala (resolver dentro de D quando tocar cada area): dashboard, alertas e analytics carregam todas as oportunidades/follow-ups em memoria; metas fazem N+1 e chamada por usuario ao IdentityManagement; faltam indices em colunas de filtro (datas de follow-up, motivos, vinculo campanha-proposta).

---

## Fatia E - Diferenciacao de nicho (pos-MVP) `[?] D5`

Coloca o Kanvas no nivel das ferramentas especificas de marketing de influencia. Relevante para vender, nao para o primeiro piloto.

- [ ] **E1 - Usage rights / licenciamento como linha precificavel** - hoje so desconto global; mercado trata como linha obrigatoria. _(proposta)_
- [ ] **E2 - Modelos hibridos de remuneracao** - base + comissao/afiliado/performance, alem do flat-fee por item. _(proposta)_
- [ ] **E3 - Rate cards reutilizaveis** - valor unitario digitado a cada vez; mercado usa rate card por creator/entregavel. _(catalogo / proposta)_
- [ ] **E4 - Tracking de engajamento da proposta** - abriu/tempo por secao; "visualizada" hoje e manual. _(link publico)_
- [ ] **E5 - Pagamento/payout de creator ligado ao fechamento** - padrao no nicho; hoje ausente no comercial. _(comercial <-> financeiro)_
- [ ] **E6 - Expiracao automatica + lembretes da proposta** - reforco do C7 com cadencia de lembrete ao cliente. _(proposta)_

---

## Notas

- Cada fatia e fechada com seu proprio commit (ou commits), seguindo o padrao pt-BR (`feat`/`fix`/`refactor`).
- Itens marcados `[?]` aguardam a decisao de produto correspondente na secao "Decisoes de produto".
- Severidades vem calibradas apos verificacao adversarial no relatorio de analise; ajustar aqui se a implementacao revelar algo diferente.
