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

- Total de itens: 56
- Concluidos: 0 / 56
- Por fatia: A 0/9 - B 0/5 - C 0/7 - D 0/29 - E 0/6

---

## Decisoes de produto (resolver antes de implementar os itens marcados [?])

- [ ] **D1 - Margem minima:** remover o campo inerte da politica OU implementar a avaliacao de verdade (exige adicionar custo do creator/proposta para calcular). Bloqueia A9.
- [ ] **D2 - Aceite digital:** botao Aceitar/Recusar com data/identidade/versao basta no MVP, OU ja queremos assinatura eletronica vinculante com trilha de auditoria? Bloqueia B1.
- [ ] **D3 - Valor fechado:** ao ganhar, sobrescrever o `EstimatedValue` da oportunidade pelo liquido da proposta aceita, OU manter estimado vs. fechado em campos separados? Bloqueia B2.
- [ ] **D4 - Deploy multi-tenant:** confirmar instancia unica compartilhada (entao corrigir resolvendo tenant pelo proprio token publico). Bloqueia C1.
- [ ] **D5 - Roadmap de nicho:** usage rights, modelos hibridos e rate cards entram no pre-lancamento ou ficam pos-MVP? Define se a Fatia E e agendada agora.

---

## Fatia A - Furos de dinheiro e governanca (Etapa 1 / quick wins)

Baixo risco, alto impacto. Estanca o "sangramento" financeiro e fecha os furos de governanca mais visiveis.

- [ ] **A1 - [Critico] Liquido no recebivel da conversao** - a geracao do recebivel usa o total BRUTO, ignorando o desconto; todo deal descontado entra inflado no financeiro -> usar o valor liquido (pos-desconto) na geracao do lancamento a receber. _(conversao proposta->campanha)_
- [ ] **A2 - [Alto] Bloquear o botao Aprovar quando ha desvio de politica sem aprovacao concedida** - o banner avisa "aprovacao obrigatoria" mas o botao continua habilitado, tornando o gate contornavel -> desabilitar o Aprovar usando a flag de "precisa aprovacao" que ja existe. _(tela de propostas / detalhe)_
- [ ] **A3 - [Alto] Guarda de reabertura no ChangeStage** - mover (inclusive por drag-and-drop) uma oportunidade fechada para estagio aberto zera data de fechamento e apaga motivos, sem aviso -> impedir ou exigir confirmacao explicita e nao apagar dados de fechamento silenciosamente. _(oportunidade / kanban)_
- [ ] **A4 - [Medio] Mascarar notas internas e custo por creator no snapshot publico** - o payload publico inclui notas internas (margem alvo, estrategia) e preco por creator -> remover/mascarar esses campos da projecao publica. _(link publico /p/:token)_
- [ ] **A5 - [Medio] Toast de erro ao falhar o move no kanban** - em erro de rede/permissao o card "volta sozinho" sem feedback -> exibir toast de erro como ja ocorre no fechamento final. _(kanban)_
- [ ] **A6 - [Medio] Validar coerencia da politica + avisar politica vazia** - politica salva sem checar prazo padrao <= maximo e sem avisar que politica vazia desliga o gate -> validar coerencia minima e exibir aviso. _(config politica comercial)_
- [ ] **A7 - [Baixo] Padronizar formatacao de moeda/data via helpers centralizados** - lista mostra "R$ 1500.00" em vez de "R$ 1.500,00" -> usar `src/lib/format`. _(lista de oportunidades)_
- [ ] **A8 - [Alto] Sinal "fora da politica" vs "aguarda ok" no inbox de aprovacoes** - distincao so aparece dentro do painel de diffs -> mostrar badge de "fora da politica" na lista/cabecalho. _(inbox de aprovacoes)_
- [ ] **A9 - [Alto] Testes nos pontos de dinheiro** - zero cobertura em liquido, recebivel na conversao e snapshot -> adicionar testes cobrindo o calculo liquido e a geracao do recebivel. _(testes)_ `[?] relacionado a D1`

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
- [ ] **C2 - [Alto] Rate limiting nos endpoints publicos + revisar CORS** - ausencia total de rate limit e CORS aberto ampliam a superficie -> aplicar rate limit nos endpoints anonimos e restringir CORS. _(API publica)_
- [ ] **C3 - [Alto] Pool/fila/timeout para o Chromium do PDF** - cada PDF sobe e descarta um navegador inteiro, sincrono, sem limite -> reusar instancia (pool), enfileirar, aplicar timeout e propagar cancelamento. _(geracao de PDF)_
- [ ] **C4 - [Alto] Reforcar o gate de aprovacao no backend** - aprovacao direta aceita nome do corpo e nao valida autoria/alcada; decisao direta ignora gating de revisores obrigatorios -> derivar aprovador do usuario autenticado, validar papel/alcada e impor o gating de revisores. _(aprovacoes)_
- [ ] **C5 - [Medio] Fechar reabertura pos-merge / reenvio bloqueado** - apos "marcar como aplicada", reenviar a mesma proposta cria nova aprovacao e bloqueia de novo -> permitir reenvio a partir do estado merged sem recriar aprovacao redundante. _(aprovacoes / envio)_
- [ ] **C6 - [Medio] IDOR intra-tenant na revogacao de share link** - revoga por id sem validar posse -> validar que o link pertence ao recurso/usuario antes de revogar. _(share link)_
- [ ] **C7 - [Medio] Expiracao de proposta e de link publico** - status "expirada" e codigo morto (nenhum job invoca) e o link nasce perpetuo -> implementar expiracao (job) e data de expiracao no link; bloquear acesso publico a propostas vencidas/rejeitadas/canceladas. _(proposta / link publico)_

---

## Fatia D - Operacao diaria e robustez (Etapa 4)

Mantem o operador usando e o funil confiavel. Inclui performance, consistencia e polimento.

- [ ] **D1i - [Alto] Lembrete proativo de follow-up** - vencimento de follow-up nao dispara nada; so aparece se abrir a tela -> job + notificacao/e-mail ao responsavel (infra de jobs ja existe). _(follow-ups)_
- [ ] **D2i - [Medio] Controle de concorrencia otimista** - transicoes gravam direto (last-write-wins) -> token de versao retornando conflito em vez de sobrescrever. _(oportunidade / proposta)_
- [ ] **D3i - [Medio] Snapshot de versao completo** - snapshot nao congela desconto nem prazo; link sempre serve a ultima versao -> congelar desconto/prazo/liquido e amarrar o link a uma versao especifica. _(versionamento de proposta)_
- [ ] **D4i - [Medio] Paridade de motivos estruturados no fechamento via kanban** - arrastar para Ganho/Perdido nao coleta motivo estruturado (so texto livre), detalhe coleta -> oferecer os mesmos motivos cadastrados no fechamento pelo kanban. _(kanban)_
- [ ] **D5i - [Medio] i18n da pagina publica e do modal de envio** - strings hardcoded em pt-BR quebram para clientes es-AR/en-US -> internacionalizar. _(link publico / envio)_
- [ ] **D6i - [Medio] Tratar falhas silenciosas em pontos sensiveis** - recebivel, aprovacao automatica, notificacoes e resolucao do responsavel engolem excecoes no console -> logar/alertar em vez de seguir em silencio (diretriz do projeto). _(varios services)_
- [ ] **D7i - [Medio] Reenvio sem guarda de estado** - reenvio cria versoes duplicadas e regride status de propostas ja aprovadas -> guardar transicoes validas no reenvio. _(proposta)_
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
- [ ] **D18i - [Medio] Deal rotting (alerta de oportunidade parada)** - ha SLA mas sem alerta proativo -> alertar oportunidades estagnadas. _(pipeline)_
- [ ] **D19i - [Medio] Endpoint publico serve a versao do link** - hoje sempre serve a ultima versao -> servir a versao referenciada pelo link. _(link publico)_ (pode casar com D3i)
- [ ] **D20i - [Baixo] Arredondamento monetario consistente no dominio** - falta padrao de arredondamento -> centralizar regra de arredondamento. _(dominio comercial)_
- [ ] **D21i - [Baixo] Conversao por estagio nao assume funil linear** - calculo assume funil estritamente linear -> tornar robusto a funis nao lineares. _(analytics)_
- [ ] **D22i - [Baixo] Oportunidades sem data prevista somem do forecast sem contador** - desaparecem silenciosamente -> contabilizar/sinalizar. _(forecast)_
- [ ] **D23i - [Baixo] Conversao em nova campanha com modal de revisao** - usa confirm nativo e cria "as cegas" -> modal padrao do produto revisando nome/datas. _(conversao)_
- [ ] **D24i - [Baixo] "Nao encontrado" retornar 404 (nao 400)** - ciclo de vida retorna 400 -> corrigir status. _(API)_
- [ ] **D25i - [Baixo] Soft delete de fontes/motivos** - exclusao fisica quebra historico de analytics -> soft delete preservando historico. _(fontes / motivos)_
- [ ] **D26i - [Baixo] Meta duplicada com erro tratado** - estoura erro cru de banco -> validar e retornar erro amigavel. _(metas)_
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
