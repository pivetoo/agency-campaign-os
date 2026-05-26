# Design: Aprovacao por aprovadores reais e conversao proposta -> campanha

- Data: 2026-05-25
- Modulo: Comercial (AgencyCampaign / Kanvas)
- Status: aprovado para detalhamento de plano
- Autor: Lead

## Contexto e problema

Auditoria do modulo Comercial apontou dois pontos a melhorar:

1. Conversao de proposta aprovada em campanha e parcial. `ProposalService.ConvertToCampaign`
   (`AgencyCampaign.Infrastructure/Services/ProposalService.cs:359`) exige um `campaignId` que ja
   exista (`:365`); o dominio (`Proposal.cs:137`) apenas seta `CampaignId`. Nao ha criacao
   automatica da campanha a partir da oportunidade/proposta, divergindo do fluxo documentado no
   README ("proposta aprovada vira campanha automaticamente").

2. Aprovadores (reviewers) sao decorativos e os alertas vazam para todos:
   - `OpportunityApprovalReviewer` tem `RecordDecision()` no dominio
     (`OpportunityApprovalReviewer.cs:45`), mas o `OpportunityApprovalReviewerService`
     so expoe `Add`/`Remove`/`GetByApprovalId` (sem voto). A decisao real e global:
     `OpportunityApprovalRequest.Approve/Reject` grava apenas quem decidiu, sem olhar reviewers.
     O status do reviewer nunca sai de `Pending`.
   - O banner "esperando sua decisao" no detalhe da oportunidade
     (`OpportunityDetail.tsx:380-400`) aparece sempre que `pendingApprovalsCount > 0`
     (`:205`), sem checar se o usuario logado e reviewer. Aparece para todos.
   - `canDecide` na tela Aprovacoes (`Approvals.tsx:331`) e `status === Pending || InReview`:
     qualquer usuario pode aprovar. Mesmo padrao no `ApprovalCard` do detalhe (`:1592`).
   - Reviewers nao aparecem de forma confiavel: a aba Aprovacoes do detalhe da oportunidade
     (`OpportunityDetail.tsx:617`) nao renderiza reviewers; o `ReviewersPanel` so existe no
     sidebar da tela standalone (`Approvals.tsx:430`,`:855`), e o GET tem `catch -> []`
     silencioso (`:869`), que esconde falhas (provavel normalizacao de rota Archon).

## Objetivos

- Aprovacao interna passa a depender dos votos dos aprovadores obrigatorios.
- Alerta e acoes de decidir ficam restritos a quem e aprovador pendente.
- Reviewers e seus status ficam visiveis onde a aprovacao aparece.
- Proposta aprovada vira campanha por um botao explicito que cria e vincula a campanha.

## Nao-objetivos

- Aprovacao sequencial ou "qualquer um aprova" (descartados na decisao de produto).
- Override de aprovacao por gestor/root (descartado).
- Geracao automatica da campanha no momento da aprovacao (sera por botao).
- Refatorar o workflow de diffs/impacts/comentarios de aprovacao alem do necessario.

## Decisoes confirmadas

- D1 Modelo de aprovacao: todos os aprovadores marcados como obrigatorios precisam aprovar
  (paralelo). Qualquer rejeicao de obrigatorio reprova. Nao-obrigatorios sao consultivos.
- D2 Escopo: apenas aprovadores pendentes decidem e veem o alerta "aguardando sua decisao".
  Os demais veem estado neutro ("aguardando aprovacao de Fulano").
- D3 Conversao: botao explicito "Converter em campanha" que cria a campanha e vincula.
- A1 Identidade do reviewer: selecionar usuario real do contrato via SearchableSelect,
  capturando `UserId`. Escopo e match passam a ser por `UserId` (nome apenas para exibir).
- A2 Sem decisao global: a aprovacao/rejeicao no nivel do request deixa de ser acionavel
  diretamente; o status do request passa a ser derivado dos votos.
- A3 Toda aprovacao precisa de ao menos um aprovador obrigatorio. O modal de criacao passa a
  selecionar aprovadores, opcionalmente pre-preenchidos pela `CommercialPolicy`
  (ja existe `PopulateFromPolicy` em `OpportunityApprovalRequestService.cs:238`).
  Sem aprovador obrigatorio, nao ha botoes de decisao e a UI orienta a adicionar.
- B1 Conversao para campanha existente: mantida como acao secundaria ("vincular a existente").

## Parte A: aprovacao por aprovadores reais e alerta escopado

### Dominio

- `OpportunityApprovalRequest` ganha navegacao para a colecao de `Reviewers` e um metodo:
  `RegisterReviewerDecision(reviewerId, OpportunityApprovalReviewerStatus status, long? decidedByUserId, string? notes)`.
  Regras:
  - valida que o reviewer pertence ao request e esta `Pending`;
  - chama `reviewer.RecordDecision(status, notes)`;
  - recomputa `Status` do request a partir do conjunto de reviewers:
    - existe reviewer `Required` com `Rejected` -> `Rejected`;
    - todos os `Required` com `Approved` -> `Approved`;
    - caso contrario, permanece `Pending`/`InReview`;
  - ao resolver, registra `DecidedAt` e o decisor: em `Approved`, o ultimo aprovador
    obrigatorio a votar; em `Rejected`, o reviewer que rejeitou.
- Invariante preservada: a recomposicao do status vive na agregada (request), nao no service.
- `OpportunityApprovalReviewer` ja possui `RecordDecision` (`:45`); `UserId` ja existe (nullable)
  e passa a ser obrigatorio no fluxo novo de criacao do reviewer.

### Servico e API

- `OpportunityApprovalReviewerService`:
  - novo `RecordDecision(approvalId, reviewerId, DecideOpportunityApprovalReviewerRequest, ct)`:
    carrega request + reviewers, valida via `ICurrentUser` que `currentUser.UserId` == `reviewer.UserId`
    (senao lanca `forbidden`), aplica a decisao pela agregada, salva.
  - quando o request resolver, dispara as notificacoes/automations ja existentes
    (mesmo ponto usado hoje no Approve/Reject do request).
  - `Add` passa a exigir `UserId` valido.
- `OpportunityApprovalRequestService`: `Approve`/`Reject` no nivel do request deixam de ser
  expostos como acao de usuario (status passa a ser derivado). `MarkInReview`, `RequestChanges`,
  `Resubmit`, `MarkMerged` permanecem.
- Endpoints (padrao Archon, atributos sem template redundante):
  - voto do reviewer no `OpportunityApprovalsController` (ex.: `reviewers/{reviewerId}/decision`);
  - manter `reviewers` (listar/adicionar/remover).
- Autorizacao continua via `[RequireAccess]`; a checagem fina de "sou o reviewer" e no service.

### Frontend

- Adicionar reviewer com `SearchableSelect` de usuarios do contrato (captura `UserId`),
  substituindo o input de nome livre (`Approvals.tsx:926-948`). Fonte de usuarios: reutilizar
  servico existente de usuarios do contrato; se inexistente, expor lista minima.
- `canDecide` deixa de ser por status e passa a ser "usuario atual e reviewer pendente
  (match por `UserId`) deste approval". Aplica em `Approvals.tsx:331` e no `ApprovalCard`
  do detalhe (`OpportunityDetail.tsx:1592`). Usuario atual via `useAuth()` (`Approvals.tsx:48`,
  `OpportunityDetail.tsx:36`).
- Banner do Resumo (`OpportunityDetail.tsx:380-400`): usa `pendingForMe`
  (aprovacoes onde sou reviewer pendente). Quem nao e reviewer ve estado neutro
  "Aguardando aprovacao de {nomes}". `pendingApprovalsCount` segue como contador informativo
  do badge da aba (`:205`,`:353`), mas nao dispara mais o CTA "sua decisao".
- Lista de reviewers com status individual (Pendente/Aprovou/Rejeitou) e progresso
  ("2 de 3 aprovaram") nos dois lugares: tela Aprovacoes e aba Aprovacoes do detalhe
  (hoje a aba nao mostra reviewers).
- Corrigir o `catch -> []` silencioso de `getApprovalReviewers` (`Approvals.tsx:869`):
  investigar a causa raiz (rota/normalizacao) e exibir erro real, sem engolir.

### Casos de borda

- Approval sem aprovador obrigatorio: sem botoes de decisao; UI orienta a adicionar.
  Migracao de dados nao necessaria (aprovacoes antigas pendentes serao tratadas adicionando
  aprovadores; documentar no plano).
- Reviewer sem `UserId` (legado): nao casa com usuario atual; tratar como nao-acionavel
  e permitir recasar pela UI (re-selecionar usuario).

## Parte B: botao "Converter em campanha"

### Dominio e migration

- `Campaign` ganha `OpportunityId` (`long?`, nullable) e `SourceProposalId` (`long?`, nullable)
  para rastreabilidade bidirecional. `Proposal.CampaignId` ja existe (o outro lado).
- Ctor/metodo de `Campaign` para setar `OpportunityId`/`SourceProposalId` na criacao via conversao.
- Migration FluentMigrator adicionando as duas colunas (nullable, sem default).

### Servico e API

- Novo `ProposalService.ConvertToNewCampaign(proposalId, ct)`:
  - carrega proposta -> negociacao -> oportunidade -> marca;
  - valida `Status == Approved` e ausencia de `CampaignId` (guarda contra duplicidade);
  - monta a `Campaign` herdando os dados (mapa abaixo), cria e faz
    `proposal.ConvertToCampaign(novaCampanhaId)`;
  - reaproveita o pos-conversao ja existente (`FinancialAutoGeneration.GenerateForConvertedProposal`,
    automation `ProposalConverted`, notificacao) que hoje vive em `ProposalService.cs:378-386` —
    extrair em metodo privado compartilhado entre os dois caminhos de conversao.
- `ConvertToCampaign(id, campaignId)` atual permanece como acao secundaria
  ("vincular a campanha existente").
- Endpoint `POST proposals/{id}/ConvertToNewCampaign` retornando o id da campanha criada.

### Mapa de heranca proposta/oportunidade -> campanha

| Campo da Campanha          | Origem                                                        |
|----------------------------|---------------------------------------------------------------|
| BrandId                    | marca da oportunidade                                         |
| Name                       | nome da oportunidade (ou "Campanha - {oportunidade}")         |
| Budget                     | total da proposta (soma dos itens)                            |
| StartsAt                   | hoje (ou `ExpectedCloseAt` da oportunidade, se houver)        |
| EndsAt                     | vazio (definido depois)                                       |
| Objective / Description    | da proposta/oportunidade quando existir                       |
| InternalOwnerName          | responsavel comercial da oportunidade                         |
| Status                     | Draft                                                         |
| OpportunityId / SourceProposalId | vinculo de origem                                       |

### Frontend

- `ProposalDetail`: botao "Converter em campanha" visivel quando a proposta esta `Approved`
  e sem `CampaignId`. Dialogo de confirmacao exibindo o que sera herdado; ao confirmar,
  chama o endpoint, cria a campanha e navega para ela. Substitui o fluxo atual que exigia
  `campaignId` (`ProposalDetail.tsx:280-316`). A opcao "vincular a existente" fica como acao
  secundaria.

## Mudancas de dados / migrations

- `campaigns`: adicionar `opportunityid` (nullable) e `sourceproposalid` (nullable).
- Sem alteracao de schema na Parte A (reviewers ja persistem; apenas passa a haver voto).

## i18n

- Novas chaves no catalogo backend de Localization, em pt-BR, en-US e es-AR:
  estado neutro do alerta ("aguardando aprovacao de"), progresso de aprovadores,
  rotulos de status do reviewer, textos do dialogo de conversao e do botao.

## Testes

- Unit (dominio/service):
  - `RegisterReviewerDecision`: quorum de obrigatorios, rejeicao de obrigatorio reprova,
    nao-obrigatorio nao bloqueia, escopo por `UserId` (forbidden para nao-reviewer),
    reviewer ja decidido nao revota.
  - `ConvertToNewCampaign`: heranca correta dos campos, idempotencia (nao converte duas vezes),
    guardas (proposta nao aprovada, ja convertida), disparo do pos-conversao
    (financeiro + automation).
- Corrigir a causa raiz dos 5 testes vermelhos de
  `AgencyCampaign.Testing/Infrastructure/Services/OpportunityApprovalRequestServiceTests.cs`
  ("colecao vazia / entidade nao encontrada apos persistir") sem hackear testes.
- E2E: estender o fluxo comercial de aprovacao para votar como reviewer e validar o escopo;
  fluxo de conversao por botao criando campanha.

## Riscos e mitigacoes

- Aprovacoes pendentes legadas sem reviewers ficariam sem decisor: documentar passo de
  adicionar aprovadores; considerar seed via politica.
- Match por `UserId` exige que o reviewer tenha `UserId`: garantir na criacao (A1).
- Edicao concorrente em `OpportunityDetail.tsx`/`ProposalDetail.tsx`: reconciliar com WIP
  existente no momento da implementacao.

## Arquivos impactados (referencia, nao exaustivo)

Backend:
- `AgencyCampaign.Domain/Entities/OpportunityApprovalRequest.cs`
- `AgencyCampaign.Domain/Entities/OpportunityApprovalReviewer.cs`
- `AgencyCampaign.Infrastructure/Services/OpportunityApprovalReviewerService.cs`
- `AgencyCampaign.Infrastructure/Services/OpportunityApprovalRequestService.cs`
- `AgencyCampaign.Application/Services/IOpportunityApprovalReviewerService.cs`
- `AgencyCampaign.Api` controller de aprovacoes
- `AgencyCampaign.Domain/Entities/Campaign.cs`
- `AgencyCampaign.Domain/Entities/Proposal.cs`
- `AgencyCampaign.Infrastructure/Services/ProposalService.cs`
- `AgencyCampaign.Infrastructure/Services/CampaignService.cs`
- `AgencyCampaign.Infrastructure/Migrations/` (nova migration de `campaigns`)
- EF Configuration de `Campaign`

Frontend:
- `AgencyCampaign.Web/src/modules/Main/Commercial/Approvals.tsx`
- `AgencyCampaign.Web/src/modules/Main/Commercial/OpportunityDetail.tsx`
- `AgencyCampaign.Web/src/modules/Main/Commercial/ProposalDetail.tsx`
- `AgencyCampaign.Web/src/components/modals/OpportunityApprovalRequestFormModal.tsx`
- `AgencyCampaign.Web/src/services/opportunityService.ts`, `proposalService.ts`
- `AgencyCampaign.Web/src/types/opportunityApprovalReviewer.ts`

i18n:
- `AgencyCampaign.Application/Resources/Localization/AgencyCampaignResource.{pt-BR,en-US,es-AR}.resx`

Testes:
- `AgencyCampaign.Testing/Infrastructure/Services/` (aprovacao/reviewer e conversao)
- `AgencyCampaign.Web/e2e/` (aprovacao por reviewer e conversao)

## Sequenciamento sugerido

1. Parte A backend (dominio + service + endpoint de voto) + correcao dos 5 testes vermelhos.
2. Parte A frontend (escopo do alerta, canDecide, visibilidade de reviewers, SearchableSelect).
3. Parte B backend (migration + ConvertToNewCampaign + reuso do pos-conversao).
4. Parte B frontend (botao + dialogo + navegacao).
5. i18n e E2E.

Partes A e B sao independentes e podem virar planos/entregas separados.
