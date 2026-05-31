# Analise do Modulo Comercial - Kanvas

Data: 2026-05-30
Escopo: backend + frontend, fase MVP (pre-lancamento SaaS Mainstay)

---

## 1. Sumario executivo

O Modulo Comercial do Kanvas tem uma fundacao boa e ja entrega um funil comercial visivelmente acima de um CRUD: pipeline kanban com SLA, propostas centradas em itens com desconto, gate de aprovacao opt-in por politica, versionamento, link publico, PDF e conversao em campanha. A engenharia de dominio e disciplinada (fechamento por comportamento semantico do estagio, nao por nome; desconto guardado so como valor absoluto com derivados; maquinas de estado com guardas).

Apesar disso, **o modulo NAO esta pronto para uma agencia usar bem no dia a dia hoje**. Tres problemas, todos verificados no codigo, comprometem a confianca no produto exatamente onde ele mais importa:

1. **O dinheiro nao bate.** O recebivel financeiro gerado na conversao usa o total BRUTO da proposta, ignorando o desconto concedido. Toda venda com desconto entra inflada no financeiro. (Critico, verificado)
2. **O numero comercial e ficticio.** Metas, forecast e analytics somam o valor ESTIMADO inicial da oportunidade, nunca o valor liquido realmente negociado e fechado. A agencia bate meta com numeros que nao existem. (Alto, verificado)
3. **O cliente nao fecha nada dentro do sistema.** A pagina publica e somente leitura: nao ha aceite, recusa nem assinatura. Quem muda o status para "Aprovada"/"Visualizada" e o proprio operador, a mao. O fechamento vira informacao de boca, sem trilha nem prova. (Critico para a jornada)

Soma-se a isso um gate de aprovacao contornavel (o botao Aprovar nao respeita o bloqueio de politica, e o caminho direto de aprovacao nao valida quem aprova), margem minima configuravel que nao e avaliada, e o link publico que so funciona para a primeira agencia do deploy multi-tenant (bloqueador de go-to-market).

**Nota de maturidade geral: 4/10 — nivel Beta inicial.** Estrutura solida, mas com furos financeiros e de fechamento que precisam ser resolvidos antes de qualquer agencia operar com confianca.

| Sub-area | Maturidade | Leitura curta |
|---|---|---|
| Pipeline (funil/kanban) | 6/10 | Bem desenhado; faltam concorrencia, reabertura controlada e SLA robusto a reabertura |
| Propostas | 4/10 | Modelo bom, mas recebivel/versao usam bruto e PDF nao escala |
| Aprovacoes/Politica | 3/10 | Gate contornavel, sem alcada, margem inerte |
| Analytics/Metas | 4/10 | Bem modelado, mas alimentado por valor estimado, nao pelo negociado |
| Seguranca | 4/10 | Multi-tenant publico fragil, sem rate limit, IDOR intra-tenant, nome forjavel |
| Testes | 4/10 | Boa cobertura de status/probabilidade; zero nos pontos de dinheiro |

---

## 2. Pontos fortes

O que ja esta bem resolvido e merece ser preservado:

- **Fechamento guiado por comportamento semantico, nao por nome.** Ganho/perdido e classificacao de estagio dependem do FinalBehavior (None/Won/Lost), nunca de comparar o nome do estagio. A agencia pode renomear estagios livremente sem quebrar relatorios — exatamente a diretriz correta do projeto.

- **Modelo de desconto bem decidido.** O desconto e guardado apenas como valor monetario absoluto; percentual e liquido sao derivados. Isso elimina a inconsistencia classica de dois campos persistidos brigando entre si, e o vai-e-volta valor/percentual fica centralizado e unico.

- **Maquinas de estado com guardas explicitas.** Tanto a oportunidade quanto a proposta tem transicoes protegidas (ex.: aprovar/rejeitar so a partir de enviada/vista, converter so a partir de aprovada, bloqueio de refechamento), com mensagens internacionalizadas em pt-BR/en-US/es-AR.

- **Gate de aprovacao opt-in e bem posicionado.** A checagem de politica e centralizada e chamada por todos os canais de envio (e-mail, WhatsApp, marcar como enviada), sem furos por canal. E quando nao ha politica cadastrada, nao ha burocracia — alinhado a decisao de produto de aprovacao opcional.

- **Inbox de aprovacoes estilo pull request.** Revisores obrigatorios/opcionais com barra de progresso, diffs de politica, conversa com mencoes e estados ricos (aguardando/aprovou/rejeitou/comentou). E uma metafora madura, bem acima do botao "aprovar" tipico de CRM.

- **Historico de transicoes como base de inteligencia.** O registro de cada movimentacao alimenta conversao por estagio, tempo medio, ciclo de venda, SLA e itens em risco — bem alem de CRUD.

- **Token de compartilhamento forte.** Gerado com gerador criptografico (24 bytes, ~192 bits) e codificacao URL-safe; forca-bruta e inviavel na pratica.

- **UX de desconto e de envio bem cuidadas no frontend.** Os dois campos espelhados (R$ e %) com clamp correto e o modal de envio que detecta canal configurado/ativo (com fallback "marcar como enviada") sao pontos de usabilidade reais.

- **Pagina publica enxuta e profissional**, com estados de carregando/nao encontrado/vazio bem tratados, e snapshot versionado congelando os itens daquela versao.

---

## 3. Analise por area

### Backend

**[Critico] O recebivel financeiro cobra o valor cheio, ignorando o desconto.** Na conversao da proposta em campanha, o lancamento financeiro a receber e montado com o total BRUTO, nao com o liquido (apos desconto). O cliente ve o liquido no PDF e na tela publica, mas o financeiro interno registra o bruto. Toda proposta com desconto gera um recebivel inflado: a conciliacao bancaria nunca fecha e ha risco de cobranca indevida da marca. Verificado ponta a ponta. Numa agencia que usa desconto como alavanca comercial, isso contamina todo deal descontado.

**[Alto] A inteligencia comercial (metas, forecast, analytics) usa o valor estimado inicial, nunca o negociado.** Progresso de meta, forecast ponderado, total ganho, top performers e motivos por valor somam todos o valor estimado da oportunidade. O valor liquido real da proposta aceita existe, mas nunca e reconciliado de volta na oportunidade ao fechar. Resultado: a agencia bate (ou nao bate) meta com base em palpites. Um vendedor "atinge" a meta com oportunidades estimadas em 100k que fecharam por 70k apos desconto, e o forecast superestima receita de forma sistematica sempre que ha desconto — distorcendo decisoes de caixa, contratacao e comissionamento.

**[Alto] Oportunidade fechada pode ser reaberta silenciosamente, apagando os dados de fechamento.** O ChangeStage so impede mover para estagio inativo; nao verifica se a oportunidade ja esta fechada. Mover (inclusive por drag-and-drop) uma oportunidade ganha/perdida para um estagio aberto zera a data de fechamento e apaga as notas de ganho/motivo de perda, sem confirmacao e sem erro. Os identificadores de motivo de ganho/perda ficam orfaos, apontando para um fechamento que nao existe mais. Como o board nao filtra fechadas, elas continuam visiveis e arrastaveis. Isso distorce win-rate, motivos de perda e tempo de fechamento — justamente as metricas mais valiosas para a agencia.

**[Alto] Mudanca simultanea de estagio sem controle de concorrencia (last-write-wins).** Nao existe token de versao nas entidades; as transicoes gravam direto, sem checagem otimista nem retorno de conflito. Num board com varias pessoas, duas pessoas movendo o mesmo card (ou a mesma pessoa em duas abas) sobrepoem transicoes sem aviso: uma fecha como ganho, outra reabre. Combinado com a ausencia de guarda de reabertura, o efeito e historico contraditorio e forecast errado, sem que ninguem perceba.

**[Alto] PDF lanca um navegador Chromium completo por requisicao, sincrono, sem fila/pool/timeout.** Cada geracao de PDF sobe e descarta um navegador headless inteiro, dentro da requisicao HTTP, sem reuso, sem limite de concorrencia e sem propagar cancelamento. O endpoint publico de PDF e anonimo. Sob alguns acessos simultaneos (cliente abrindo, equipe baixando) o servidor pode saturar memoria e derrubar a API — afetando todas as agencias do deploy compartilhado. Como e o documento que o cliente recebe, lentidao aqui afeta diretamente a percepcao de qualidade.

**[Alto] Margem minima cadastrada mas nunca avaliada.** A politica permite configurar uma margem minima, o campo e persistido, mas o avaliador so olha desconto e prazo — ha ate um teste que garante que margem e ignorada. O operador configura uma trava de margem esperando barrar propostas abaixo dela, e nada acontece. Para uma agencia de influencia (onde margem importa mais que desconto nominal, por causa do cache variavel de creator), isso e uma falsa sensacao de controle perigosa. Agravante: a proposta nem sequer carrega custo, entao margem nao pode ser calculada hoje.

**[Alto] Aprovacao direta aceita qualquer nome do corpo e nao valida autoria nem alcada.** Os endpoints diretos de decisao recebem o nome do aprovador inteiramente do corpo da requisicao e nao verificam se o chamador e revisor designado ou tem alcada. Qualquer usuario com a permissao generica pode aprovar a propria proposta com desvio, assinando com o nome de outra pessoa. E justamente esse o caminho usado para resolver as aprovacoes automaticas de desvio (que nascem sem revisores obrigatorios). O gate de margem/teto que justifica ter politica fica contornavel por dentro, e a trilha de auditoria, falsificavel.

**[Medio] Apos "marcar como aplicada" (merge), o reenvio volta a ser bloqueado.** O fluxo de UI conduz o aprovador a marcar a excecao como aplicada apos aprovar. Mas, ao reenviar a mesma proposta (que ainda estoura a politica), o gate cria uma NOVA aprovacao e bloqueia de novo. O operador segue exatamente o que a tela manda e e barrado no momento de fechar, com uma segunda aprovacao pendente redundante. Nao ha caminho de reenvio a partir do estado merged.

**[Medio] Falhas silenciosas em pontos sensiveis.** A geracao do recebivel na conversao, a criacao automatica da aprovacao, o envio de notificacoes e a resolucao do nome do responsavel engolem excecoes escrevendo no console e seguem. Consequencias praticas: a campanha existe mas o recebivel some sem alerta; a proposta pode ficar travada (bloqueada para enviar e sem aprovacao para decidir) sem alarme; o card pode exibir responsavel em branco. Contraria a diretriz do projeto de nao adotar fallback silencioso.

**[Medio] Propostas nunca expiram, e o link automatico nunca expira.** Existe a regra de expiracao da proposta, mas nenhum job a invoca — o status "expirada" e codigo morto. O link gerado no envio nasce sem data de expiracao, sendo perpetuo ate revogacao manual (que ninguem faz). Propostas vencidas, rejeitadas ou canceladas continuam publicamente acessiveis com todos os valores. Risco de cliente reabrir uma proposta vencida e cobrar o preco antigo.

**[Medio] Outros pontos de qualidade de dados e funil:** reenvio sem guarda de estado cria versoes duplicadas e regride status de propostas ja aprovadas; o snapshot de versao nao congela desconto nem prazo (esvaziando o proposito do versionamento como prova); o desconto encolhe sozinho quando itens sao removidos, sem aviso; o endpoint publico sempre serve a ultima versao, nao a versao do link; ausencia de arredondamento monetario consistente no dominio; conversao por estagio assume funil estritamente linear; oportunidades sem data prevista somem do forecast sem contador. Cada um isolado e tolerave; juntos, minam a confianca nos numeros.

**[Baixo] Inconsistencias menores:** "nao encontrado" no ciclo de vida retorna 400 em vez de 404; resolucao de estagio final sem desempate deterministico; exclusao fisica de fontes/motivos quebra historico de analytics; meta duplicada estoura erro cru de banco; coleta de IP/user-agent sem politica de retencao (exposicao LGPD futura).

### Frontend

**[Critico/Alto] O cliente nao consegue aceitar nem recusar pelo link publico.** A pagina publica e estritamente somente leitura: itens, desconto, total e "baixar PDF". Nao ha aceitar, recusar, comentar ou assinar. Toda a maquina de status (aprovar/rejeitar) e acionada exclusivamente pelo operador interno. O "visualizada" tambem e um clique manual do operador, nao a abertura real do link. O estado da proposta reflete o que a agencia digitou, nao o que o cliente fez. Isso remove a parte mais valiosa de um fluxo de proposta digital (aceite registrado com data/hora e versao) e a maior fonte de conversao e de prova comercial.

**[Alto] O banner exige aprovacao por desvio, mas o botao Aprovar nao e bloqueado.** Quando a politica e estourada e nao ha aprovacao concedida, a tela mostra um aviso de que a aprovacao e obrigatoria — porem o botao Aprovar do operador permanece habilitado. O gate de excecao vira apenas um aviso amarelo contornavel. Combinado com a falta de alcada no backend, a governanca de desconto deixa de existir na pratica.

**[Alto] "Solicitar aprovacao" a partir da oportunidade e um elemento morto.** O detalhe da oportunidade renderiza o modal de pedido de aprovacao e mantem o estado, mas nenhum botao da tela o aciona. Na pratica, pedir aprovacao so e possivel de dentro da proposta. O operador, que esta na visao natural do negocio (a oportunidade), nao tem como disparar a aprovacao dali — codigo morto que sinaliza fluxo planejado e abandonado.

**[Alto] Distincao "aguarda ok" vs "estoura a politica" nao aparece no inbox de aprovacoes.** A lista e o cabecalho mostram so o tipo e o status. O sinal de "fora da politica" so aparece dentro do painel de diffs, depois de abrir e interpretar a tabela, e somente para diffs auto-gerados. O gestor nao consegue priorizar rapidamente um desconto que fura o teto versus um que so precisa de um ok formal, aumentando o risco de aprovar no automatico algo que compromete a rentabilidade.

**[Medio] Fechamento "ganho"/"perdido" no kanban nao coleta motivo estruturado; o detalhe coleta.** Arrastar para "Ganho" no kanban nunca registra o motivo estruturado (so texto livre ou nada), enquanto fechar pelo detalhe oferece motivos cadastrados. A qualidade do dado de "por que ganhamos/perdemos" depende de por onde o operador fechou — parte estruturada, parte texto livre, dados nao comparaveis. E na perda pelo kanban o texto e obrigatorio sem oferecer os motivos cadastrados, poluindo a base.

**[Medio] Falha ao mover card no kanban e silenciosa.** Em erro de rede/permissao, o card simplesmente "volta sozinho" para a coluna de origem, sem nenhum toast — diferente do fechamento final, que avisa. O operador acha que moveu, segue, e depois o pipeline mostra a etapa antiga sem explicacao. Gera retrabalho e desconfianca, especialmente em conexao instavel.

**[Medio] Lacunas de robustez e consistencia:** o kanban nao tem estado de carregamento proprio (tela em branco que "pisca" no primeiro load); no mobile nao ha como mover de etapa direto na lista (exige abrir o detalhe); montar aprovacoes/revisores no detalhe gera N+1 de chamadas com erros silenciados; o link publico pode ser gerado/enviado em rascunho por um caminho enquanto outro caminho o proibe; exclusao de item dispara sem confirmacao; a politica salva sem validar coerencia (prazo padrao acima do maximo) nem avisar que politica vazia desliga o gate; e widgets de forecast/insights falham de forma invisivel (lista vazia = erro = sem dado, indistinguiveis).

**[Baixo] Polimento e divida tecnica:** lista de oportunidades formata moeda/data fora dos helpers centralizados (mostra "R$ 1500.00" em vez de "R$ 1.500,00"); strings da pagina publica e do modal de envio hardcoded em pt-BR (problema para clientes es-AR/en-US); papeis de revisor em portugues embutidos no codigo; conclusao de follow-up irreversivel pela UI; lista de aprovacoes carrega 200 itens fixos sem paginacao nem aviso de truncamento; probabilidade derivada do estagio nao e exibida em nenhum card; e acessibilidade fraca em botoes apenas-icone e barras de progresso sem rotulo.

### Fluxo do operador

A jornada ponta a ponta tem boa espinha (pipeline kanban -> detalhe da oportunidade -> proposta -> aprovacao -> conversao em campanha), mas quebra em pontos previsiveis:

**[Critico] O fechamento depende de transcricao manual.** Sem aceite do cliente, o "sim" chega por WhatsApp/e-mail/telefone e o operador marca "Aprovada" a mao. O fechamento vira informacao de boca, sujeita a erro e sem prova de qual versao e qual valor o cliente aprovou.

**[Alto] Dois modelos mentais de "aprovar" se sobrepoem e confundem.** Aprovar a PROPOSTA (botao que muda o status) e aprovar o PEDIDO INTERNO de excecao (fluxo estilo pull request com revisores e diffs) sao coisas diferentes que o operador precisa distinguir sozinho. Como o gate e so visual, nada impede aprovar a proposta com desvio pendente.

**[Medio] Atrito no caminho de venda em volume.** Desconto so e definido no detalhe, depois de ter itens, e persiste no blur (sem botao salvar explicito) — gerando duvida de "ja salvou?" e risco de enviar proposta com desconto nao gravado. Vocabulario residual de "negociacao" (que foi aposentada) ainda aparece na UI de aprovacoes e em rotas, aumentando a curva de aprendizado.

**[Baixo] Inconsistencia de padrao no momento de maior valor:** a conversao em nova campanha usa um confirm nativo do navegador (fora do padrao de modal do produto) e cria "as cegas", sem deixar revisar nome/datas antes.

### Aptidao de produto

**[Alto] O produto registra a tarefa mas nao cobra a execucao.** Nao ha lembrete proativo de follow-up: o atraso so aparece se alguem abrir a tela. Diferente das mencoes em comentarios (que notificam), o vencimento de follow-up nao dispara nada — nem e-mail, nem push, nem notificacao interna. Para uma agencia, follow-up esquecido = oportunidade perdida; a ferramenta vira uma agenda passiva, perdendo o principal valor de um CRM (nao deixar o lead esfriar). Ha inclusive infraestrutura de jobs no sistema e um gatilho de automacao declarado para isso, mas nunca conectado.

**[Alto] Falta o aceite digital do cliente como peca central** (detalhado acima), que e simultaneamente um gap de produto e de jornada.

**[Medio] Probabilidade presa a media do estagio.** O dominio suporta probabilidade manual, mas nenhum endpoint a expoe e nenhuma tela a exibe. Para agencias com poucos negocios grandes, o forecast fica preso a media do estagio, subestimando ou superestimando deals especificos, sem que o closer possa ajustar nem priorizar pelo "quao provavel e fechar isto".

**[Medio] Escopo "minhas oportunidades" depende inteiramente da matriz de permissoes.** O isolamento entre vendedores e por permissao, nao por dado: um erro de configuracao de papel expoe a carteira inteira. Funciona, mas e fragil para uma agencia que quer carteiras separadas por closer.

---

## 4. Verificacao: codigo vs. intencao documentada

| # | Regra documentada | Conforma | Observacao / divergencia |
|---|---|---|---|
| 1 | Probabilidade derivada do estagio, reassumida a cada mudanca, usada so no forecast (nunca digitada) | Sim | Conforme na pratica. Existe maquinario legado de probabilidade manual na entidade, mas nenhum fluxo de producao o aciona (so testes). Risco residual: codigo morto que contradiz textualmente o "nunca digitada" se um chamador futuro usar o metodo manual. |
| 2 | Mover para estagio final dispara fechamento (100% ganho / 0% perdido) via comportamento semantico, NAO por nome | Sim | Totalmente conforme. Fechamento e leitura de status agregado usam o comportamento final do estagio, nunca o nome. Comprovado por testes de dominio. |
| 3 | Desconto como valor monetario; percentual/liquido derivados e nao persistidos; liquido = bruto - desconto; vai-e-volta correto | Sim | Conforme. Nuance nao-contraditoria: o valor persistido e clampado apenas a nao-negativo (pode exceder o bruto no armazenamento), e o teto e aplicado so na derivacao para exibicao/calculo — coerente com a regra (o valor e a fonte de verdade). |
| 4 | Politica avalia APENAS desconto e prazo; margem minima existe no cadastro mas NAO e avaliada | Sim | Conforme, com teste explicito garantindo que margem e ignorada. **Importante:** isto e conformidade com a intencao documentada, mas e um gap funcional perigoso (campo configuravel inerte que sugere controle inexistente) — ver secao 5. |
| 5 | Gate no envio: estourar politica BLOQUEIA e cria aprovacao automatica; com aprovacao aberta, segue bloqueado com mensagem especifica | Sim | Conforme no backend, com testes cobrindo bloqueio, nao-duplicacao e destravar apos aprovado. **Divergencia relevante na pratica:** o frontend nao bloqueia o botao Aprovar e o caminho direto nao valida quem aprova, entao o gate e contornavel fora do envio. |
| 6 | Aprovacao multi-revisor (obrigatorios/opcionais) e transicoes Pending -> InReview -> Approved/Rejected/ChangesRequested -> Merged | Parcial | A maquina de estados nao impoe a ordem linear: apos entrar em revisao, os botoes de decisao unica nao podem mais ser usados por esse caminho (exigem Pending); decisoes a partir de "em revisao" so funcionam pela votacao de revisores. Pior: a decisao direta define o status final ignorando o gating de revisores obrigatorios, e a aprovacao automatica de desvio nasce SEM revisores obrigatorios — entao a votacao nunca resolve sozinha, restando so o caminho direto sem validacao de autoria. **Divergencia de severidade Media.** |

**Leitura geral:** o codigo conforma fielmente com as regras de dominio documentadas (1 a 5 majoritariamente Sim). As divergencias materiais nao estao no "o que o dominio faz", e sim em **lacunas de produto que a propria intencao documentada nao cobre** (margem inerte, gate apenas no envio e nao na decisao, multi-revisor inerte no caminho mais comum).

---

## 5. Lacunas e riscos

### Lacunas de produto

- **Aceite digital do cliente.** Sem aceitar/recusar/assinar online no link publico, o fechamento nao tem lastro nem trilha. E a maior lacuna funcional do modulo.
- **Reconciliacao do valor fechado.** O valor negociado (liquido) nunca volta para a oportunidade nem para meta/forecast/analytics. Toda a inteligencia comercial roda sobre estimativa.
- **Lembrete proativo de follow-up.** Sem job/notificacao de vencimento, o produto nao cobra a execucao — perde o nucleo de valor de um CRM.
- **Avaliacao de margem real.** O indicador mais relevante para a agencia (custo do creator vs. preco) nao e checado, e a proposta nem carrega custo para calcula-lo.
- **Versionamento como prova.** O snapshot nao congela desconto/prazo, e o link sempre serve a ultima versao — o versionamento nao consegue provar "o que o cliente aprovou na v1".
- **Modelagem de remuneracao alem de valor unitario fixo.** Sem comissao/afiliado/performance, sem linha de usage rights/licenciamento, sem deals recorrentes/embaixador — tudo o que o mercado de influencia ja trata como padrao (ver secao 6).

### Riscos tecnicos / seguranca

- **[Critico] Recebivel inflado pelo uso do bruto** na conversao — quebra conciliacao e arrisca cobranca indevida.
- **[Alto] Link publico multi-tenant fragil.** Os endpoints publicos sao anonimos e nao carregam tenant; sem tenant no contexto, o sistema cai no primeiro tenant configurado. Em deploy SaaS de instancia unica, o link publico so funciona para a primeira agencia — as demais recebem "link invalido". E um bloqueador direto de go-to-market. (O risco de vazamento cross-tenant foi superdimensionado e nao se sustenta com banco-por-tenant; o impacto real e de disponibilidade.)
- **[Alto] Ausencia total de rate limiting nos endpoints publicos**, com o PDF subindo um Chromium por requisicao — vetor de exaustao de recursos barato que afeta todas as agencias do deploy compartilhado. CORS aberto amplia a superficie.
- **[Alto] Gate de aprovacao contornavel** (botao nao bloqueado + aprovacao direta sem alcada + nome forjavel) — anula o controle de margem e falsifica a auditoria.
- **[Alto] Concorrencia e reabertura sem controle** — last-write-wins e reabertura silenciosa corrompem historico e forecast sem erro visivel.
- **[Medio] Notas internas e custo por creator vazam no link publico.** O snapshot publico inclui o campo de notas internas (margem alvo, estrategia, observacoes sobre o cliente) e o detalhamento de preco por creator, sem mascaramento — a agencia pode vazar bastidores no proprio link que envia.
- **[Medio] IDOR intra-tenant** na revogacao de share link (revoga por id sem validar posse) e visibilidade ampla de analytics/metas por usuario arbitrario sem reforco no codigo.
- **[Medio] Falhas silenciosas** em recebivel, aprovacao automatica e notificacoes deixam estados quebrados sem alarme.
- **[Baixo] LGPD:** coleta de IP/user-agent de visitantes sem aviso, retencao ou anonimizacao.

**Risco de escala:** dashboard, alertas e analytics carregam todas as oportunidades/follow-ups em memoria; metas fazem N+1 de banco e chamada por usuario ao IdentityManagement; faltam indices em colunas de filtro (datas de follow-up, motivos, vinculo campanha-proposta). Irrelevante no pre-lancamento, mas estoura justamente quando o cliente cresce e mais usa os relatorios.

**Risco de testes:** a cobertura e boa em status, probabilidade e gate de envio, mas **zero nos pontos de dinheiro** (liquido, recebivel na conversao, snapshot de versao) e nos caminhos de risco do gate (reenvio pos-merge, autoaprovacao, multi-revisor automatico). A suite verde da confianca falsa exatamente onde os bugs mais caros vivem.

---

## 6. Comparacao com o mercado

O mercado de plataformas comerciais (CRMs gerais e ferramentas especificas de marketing de influencia) convergiu em um conjunto de recursos que o Kanvas ainda nao tem. Onde o Kanvas esta a frente, no nivel e atras:

| Funcionalidade | Mercado | Kanvas |
|---|---|---|
| Pipeline kanban configuravel com SLA por estagio | Padrao (Pipedrive, HubSpot) | **No nivel** — bem resolvido, com SLA e risco no card |
| Fechamento por comportamento semantico do estagio | Comum | **A frente** em robustez (nao depende de nome) |
| Gate de aprovacao de desconto multi-nivel | Maduro (Salesforce CPQ, PandaDoc) | **Atras** — gate contornavel, sem alcada, margem inerte |
| Aceite/assinatura digital do cliente na proposta | Padrao de mercado (PandaDoc, Proposify, Qwilr, DocuSign) | **Ausente** — link somente leitura |
| Tracking de engajamento da proposta (abriu/tempo por secao) | Padrao | **Ausente** — "visualizada" e clique manual |
| Expiracao automatica + lembretes da proposta | Padrao | **Ausente** — expiracao e codigo morto; link perpetuo |
| Probabilidade calibrada por dados + decay por estagnacao | Tendencia consolidada (HubSpot, Pipedrive) | **Atras** — probabilidade fixa por estagio, nem exibida |
| Deal rotting (alerta de oportunidade parada) | Padrao (Pipedrive) | **Parcial** — ha SLA, mas sem alerta proativo (so pull) |
| Pagamento/payout de creator ligado ao fechamento | Padrao no nicho (CreatorIQ, Upfluence, Lumanu) | **Ausente** no comercial |
| Usage rights/licenciamento como linha precificavel | Padrao no nicho (51% dos creators cobram a parte) | **Ausente** — so desconto global em R$/% |
| Modelos hibridos (base + comissao/afiliado) | Tendencia dominante 2026 | **Ausente** — so flat-fee por item |
| Rate cards reutilizaveis / pricing assistido por IA | Tendencia 2026 | **Ausente** — valor unitario digitado |
| Conversao de deal fechado em campanha executavel | Padrao | **No nivel** — converte em campanha (existente ou nova) |

**Onde o Kanvas esta a frente ou no nivel:** a modelagem de funil (comportamento semantico, historico como base de metricas) e a metafora de aprovacao estilo pull request sao mais ricas que o tipico "botao aprovar" de CRM. A conversao deal->campanha dentro da mesma plataforma e um diferencial real para agencias.

**Onde esta claramente atras:** o fechamento (sem aceite/assinatura/tracking, que o mercado trata como tabela-padrao) e a precificacao especifica de influencia (usage rights, modelos hibridos, rate cards). Propostas com e-signature fecham mais e mais rapido, e usage rights ja e linha comercial obrigatoria no nicho.

Fontes:
- Pipeline e probabilidade ponderada: https://www.salesforce.com/sales/pipeline/management/ ; https://forecastio.ai/blog/hubspot-sales-pipeline-stages ; https://prospeo.io/s/weighted-pipeline
- Deal rotting / atividade obrigatoria: https://support.pipedrive.com/en/article/the-rotting-feature
- Aceite/assinatura digital e tracking de proposta: https://www.pandadoc.com/proposal-software/ ; https://www.proposify.com/electronic-signature-software ; https://qwilr.com/ ; https://www.docusign.com/products/electronic-signature/learn/electronic-signature-legality
- Aprovacao de desconto multi-nivel: https://help.salesforce.com/s/articleView?id=sales.cpq_advanced_approvals.htm&language=en_US&type=5
- Especifico de influencia (CRM, pagamento, contratos): https://grin.co/product/influencer-crm-platform/ ; https://www.creatoriq.com/influencer-marketing-solution/creator-payments ; https://www.aspire.io/ ; https://www.upfluence.com/pricing
- Usage rights/licenciamento e modelos hibridos: https://www.lumanu.com/blog/how-influencers-charge-whitelisting-usage-rights ; https://impact.com/influencer/influencer-marketing-trends-performance/ ; https://digitalagencynetwork.com/influencer-marketing-agency-pricing-guide/
- Rate cards e pricing por IA: https://influenceflow.io/resources/rate-cards-and-pricing-structure-the-complete-2026-guide/

---

## 7. Recomendacoes priorizadas

### Quick wins (alto impacto, baixo esforco)

1. **Usar o liquido (nao o bruto) no recebivel da conversao.** Corrige o bug critico de dinheiro. Impacto: Alto. Esforco: Baixo. (Trocar o valor passado na geracao do recebivel.)
2. **Bloquear o botao Aprovar no frontend quando ha desvio de politica sem aprovacao concedida.** Fecha o furo mais visivel do gate. Impacto: Alto. Esforco: Baixo. (A flag de "precisa aprovacao" ja existe; falta liga-la ao desabilitar.)
3. **Adicionar guarda de reabertura no ChangeStage:** impedir (ou exigir confirmacao explicita) mover uma oportunidade ja fechada para um estagio aberto, e nao apagar dados de fechamento silenciosamente. Impacto: Alto. Esforco: Baixo/Medio.
4. **Mascarar/remover as notas internas e o custo por creator do snapshot publico.** Estanca o vazamento de bastidores no link. Impacto: Alto. Esforco: Baixo.
5. **Mostrar toast de erro quando o move do kanban falha** (em vez de o card "voltar sozinho"). Impacto: Medio. Esforco: Baixo.
6. **Validar coerencia minima da politica** (prazo padrao <= maximo) e avisar que politica vazia desliga o gate. Impacto: Medio. Esforco: Baixo.
7. **Padronizar formatacao de moeda/data na lista** usando os helpers centralizados. Impacto: Baixo (percepcao de qualidade). Esforco: Baixo.
8. **Decidir sobre a margem minima:** ou remover o campo inerte da politica, ou marca-lo claramente como "informativo (nao avaliado)" ate implementar. Impacto: Medio. Esforco: Baixo.

### Estruturais (maior esforco)

9. **Implementar aceite digital do cliente no link publico** (aceitar/recusar/comentar, com captura de data, identidade e versao; idealmente assinatura com trilha). Resolve o gap central da jornada e o item de mercado mais critico. Impacto: Alto. Esforco: Alto.
10. **Reconciliar o valor negociado de volta na oportunidade ao fechar** e fazer meta/forecast/analytics usarem o liquido. Torna a inteligencia comercial real. Impacto: Alto. Esforco: Medio/Alto.
11. **Promover "visualizada" automaticamente a partir do acesso real ao link** e notificar o operador ("cliente abriu, hora de ligar"). Impacto: Alto. Esforco: Medio.
12. **Lembrete proativo de follow-up** (job + notificacao/e-mail, com responsavel pela tarefa) — a infraestrutura de jobs ja existe no sistema. Impacto: Alto. Esforco: Medio.
13. **Reforcar o gate de aprovacao no backend:** derivar o aprovador do usuario autenticado, validar alcada/papel, e fechar o caminho de reabertura pos-merge. Impacto: Alto. Esforco: Medio/Alto.
14. **Pool/fila/timeout para o Chromium do PDF e rate limiting nos endpoints publicos.** Tira o vetor de exaustao e a lentidao. Impacto: Alto. Esforco: Medio.
15. **Resolver o link publico multi-tenant** (resolver tenant pelo proprio token, nao por fallback ao primeiro tenant). Bloqueador de go-to-market. Impacto: Alto. Esforco: Medio.
16. **Controle de concorrencia otimista** (token de versao) nas transicoes de estagio/fechamento, retornando conflito em vez de last-write-wins. Impacto: Medio/Alto. Esforco: Medio.
17. **Completar o snapshot de versao** (congelar desconto, prazo e liquido) e amarrar o link a uma versao especifica. Impacto: Medio. Esforco: Medio.
18. **Testes nos pontos de dinheiro e nos caminhos de risco do gate.** Impacto: Alto (preventivo). Esforco: Medio.
19. **(Roadmap de nicho)** linha de usage rights/licenciamento, modelos de remuneracao hibridos e rate cards reutilizaveis. Impacto: Alto (diferenciacao). Esforco: Alto.

---

## 8. Proximos passos sugeridos

Sequencia pragmatica para deixar o modulo pronto para agencias, sem tentar fazer tudo de uma vez:

**Etapa 1 — Estancar os furos de dinheiro e governanca (dias, nao semanas).**
Aplicar os quick wins 1 a 4 e 8: liquido no recebivel, bloquear o botao Aprovar, guarda de reabertura, mascarar dados internos no link publico, decidir sobre margem. Acompanhar de testes nos pontos de dinheiro (recomendacao 18). Sem isto, qualquer piloto com agencia real produz dados financeiros errados.

**Etapa 2 — Tornar o fechamento real e a inteligencia confiavel (semanas).**
Implementar aceite digital do cliente (9), reconciliacao do valor negociado (10) e a promocao automatica de "visualizada" + notificacao (11). Este e o bloco que transforma o modulo de "registro manual" em "funil rastreavel" e que faz meta/forecast pararem de mentir.

**Etapa 3 — Preparar para o go-to-market SaaS (em paralelo com a Etapa 2).**
Resolver o link publico multi-tenant (15), adicionar rate limiting e pool/fila para o PDF (14), e reforcar o gate no backend (13). Sem a Etapa 3, o produto nao funciona para a segunda agencia do deploy e fica exposto a exaustao de recursos.

**Etapa 4 — Operacao diaria e confianca de longo prazo.**
Lembrete proativo de follow-up (12), controle de concorrencia (16), snapshot de versao completo (17), e os ajustes de UX/consistencia (paridade de motivos no kanban, validacoes, i18n da pagina publica, deal rotting). E o que mantem o operador usando e o funil atualizado.

**Etapa 5 — Diferenciacao de nicho (roadmap pos-MVP).**
Usage rights, modelos hibridos de remuneracao, rate cards e tracking de engajamento da proposta. E o que coloca o Kanvas no nivel das ferramentas especificas de marketing de influencia — relevante para vender, nao para o primeiro piloto.

Criterio de saida para "pronto para agencias": ao final da Etapa 2 (com a Etapa 1 ja feita), uma agencia consegue rodar lead -> proposta -> aceite do cliente -> conversao em campanha com dados financeiros corretos e metas reais. A Etapa 3 e pre-requisito para mais de uma agencia no mesmo deploy.
