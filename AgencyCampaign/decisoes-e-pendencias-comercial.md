# Decisões e Próximos Passos — Módulo Comercial (Kanvas)

Estado atual: **45/57 itens resolvidos**. Tudo que não dependia de decisão sua está feito,
testado e na `main` (suíte 906 verde). Este documento lista o que falta, em checklist, para
você marcar `[x]` no que aprova / decidir.

Como usar: marque a **opção escolhida** em cada decisão e marque `[x]` nos itens de construção
que você quer que eu faça. O detalhe técnico do que já foi feito está em `correcoes-modulo-comercial.md`.

---

## 1. Decisões pendentes (cada uma destrava trabalho)

### D2 — Aceite digital do cliente  _(destrava B1)_
Como o cliente aceita/recusa a proposta direto no link público?
- [X] **A (MVP):** botão Aceitar / Recusar / Comentar, capturando data + identidade + versão
- [ ] **B:** assinatura eletrônica vinculante com trilha de auditoria completa
- [ ] Outro: ______________________________

### D3 — Valor fechado  _(destrava B2)_
Ao ganhar a oportunidade, qual valor vira a "fonte da verdade" para metas/forecast/analytics?
- [ ] **A:** sobrescrever o `EstimatedValue` da oportunidade pelo líquido da proposta aceita
- [X] **B (recomendado):** manter **estimado vs. fechado** em campos separados (preserva histórico)
- [ ] Outro: ______________________________

### D4 — Deploy multi-tenant  _(destrava C1)_
- [X] Confirmar **instância única compartilhada** (aí resolvo o tenant pelo próprio token público)
- [ ] Outro modelo de deploy: ______________________________

### D5 — Roadmap de nicho  _(define a Fatia E)_
As features de diferenciação (usage rights, rate cards, modelos híbridos) entram quando?
- [X] **No pré-lançamento** (agendar a Fatia E agora)
- [ ] **Pós-MVP** (deixar a Fatia E para depois do lançamento)

### D6 — Política de arredondamento financeiro  _(do nosso papo da auditoria)_
- [X] Manter **`AwayFromZero`** (arredondamento comercial — já é o padrão do código)
- [ ] Trocar para **`ToEven`** (bancário, reduz viés cumulativo)

### D7 — IP de visitantes / LGPD  _(do nosso papo)_
- [X] Manter **anonimizado** (como está hoje)
- [ ] **IP completo** + base legal (segurança) + nota + job de retenção/expurgo
- [ ] **Não logar IP**

---

## 2. O que falta construir (bloqueado pelas decisões acima)

### Fatia B — Fechamento real (o coração do MVP comercial)
- [x] **B1 — Aceite digital no link público** — FEITO (D2=A): cliente aceita/recusa no link com trilha (nome, e-mail, data, versão, hash do conteúdo); promove a proposta e notifica o operador. _(commit do B1)_
- [x] **B2 — Reconciliação do valor fechado** — FEITO (D3=B): novo campo `ClosedValue` na oportunidade, setado no ganho a partir da proposta aceita (sem tocar no estimado); metas/forecast/analytics/dashboard passam a usar o fechado nas ganhas. _(Fatia B FECHADA)_

### Fatia C — Go-to-market
- [x] **C1 — Link público multi-tenant** — FEITO (D4, abordagem #1): token embute o tenant; middleware próprio do AgencyCampaign resolve o banco certo a partir do token, só nas rotas públicas e só quando não há tenant. Sem mexer no framework Archon. _(Fatia C: só falta C2, removido a seu pedido)_

### Fatia E — Diferenciação de nicho _(depende de **D5**)_
- [x] **E1 — Usage rights / licenciamento** como linha precificável (item ganhou Kind/UsageDurationMonths/UsageScope)
- [x] **E2 — Modelos híbridos de remuneração** (base + comissão/afiliado/performance) — FEITO (modelo A, por item): ProposalItem ganhou PricingModel (Fixed/Commission/Performance) + taxa% + base estimada; total variável entra como estimativa. Integra com E5.
- [x] **E3 — Rate cards reutilizáveis** por creator/entregável (RateCardItem + picker na proposta)
- [x] **E4 — Tracking de engajamento da proposta** (abriu / tempo por seção) — FEITO: painel de engajamento com total de aberturas, primeiro/último acesso e timeline por device. Tempo por seção fica como evolução futura.
- [x] **E5 — Payout de creator ligado ao fechamento** (comercial ↔ financeiro) — FEITO (modelo A): conversão gera repasse de creator planejado (FinancialEntry CreatorPayout Pending) por item com creator; idempotente; execução por entrega suprimida quando há planejado. Forecast de margem desde o dia 1.
- [x] **E6 — Expiração automática + lembretes ao cliente** (cadência de lembrete) — FEITO: job avisa o responsável antes da proposta expirar (janela 3 dias); e-mail direto ao cliente fica como evolução futura.

---

## 3. Melhorias opcionais (sem depender de decisão — posso fazer quando quiser)

Dívidas pequenas que anotei durante o trabalho; nenhuma é bug, são reforços.

- [ ] **Isolamento por dono nos recursos-filho** da oportunidade (comentários, follow-ups, stageHistory ainda só por permissão; reforço do D17i)
- [ ] **Paginação server-side real nas Aprovações** (hoje teto 200 + aviso; filtro/busca são client-side)
- [ ] **Varredura de acessibilidade app-wide** (D28i cobriu só o módulo comercial)
- [ ] **Job de expurgo de `ProposalView` antigos** (retenção de dados de acesso)
- [ ] **Unificar arredondamento de métricas/projeção** via `Money.Round` (cosmético; não é dinheiro-de-registro)
- [ ] **Concorrência otimista na Proposta** (D2i cobriu só a Oportunidade; proposta e mudança de etapa inline no detalhe ainda last-write-wins)

---

## 4. Operacional (lembrar no deploy)

Não é desenvolvimento, mas precisa de ação no ambiente:
- [ ] **Conceder as permissões novas aos papéis** após o deploy (o access-sync as cria, mas é preciso atribuir): `opportunities.reopenFollowUp`. Além disso, a lógica de "só minhas oportunidades" (D17i) considera quem tem `opportunities.get` / `opportunities.board` como acesso amplo — conferir os papéis.

---

## 5. Adiados com justificativa (baixo valor — confirmar se concorda)

- [ ] **D19i** — endpoint público servir a versão exata do link (marginal: com link único reusado + desconto já congelado por versão, só importaria com múltiplos links manuais)
- [ ] **D24i** — `record.notFound` → 404 global (`NotFoundException`): hoje vira 400 em dezenas de services de TODO o codebase; o certo é uma passada cross-módulo dedicada, não piecemeal no comercial

---

### Resumo

Nada acima é dívida técnica travando o que já existe — o módulo está sólido (nota ~7,5/10).
O maior salto de valor é a **Fatia B (fechamento real)**: me responda **D2** e **D3** e eu construo.
**D4** destrava o multi-tenant; **D5** define se a diferenciação de nicho entra agora.
