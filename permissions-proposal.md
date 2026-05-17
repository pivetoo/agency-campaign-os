# Proposta de Permissões — Kanvas (AgencyCampaign)

Mapa dos endpoints organizados por domínio funcional, com proposta de quais perfis devem ter acesso.

## Convenção de permissão

O `RequireAccessAttribute` do Archon gera a permission claim no formato `{controllerCamelCase}.{actionCamelCase}`.

Exemplos:
- `BrandsController.Create` → `brands.create`
- `OpportunitiesController.GetById` → `opportunities.getById`
- `FinancialEntriesController.MarkAsPaid` → `financialEntries.markAsPaid`

Cada linha da matriz abaixo representa uma capacidade funcional que abrange 1 ou mais endpoints relacionados.

## Perfis

| Perfil | Responsabilidade |
|---|---|
| **Comercial** | Prospecção, pipeline, propostas, follow-ups. Vende e fecha contratos com marcas. |
| **Operação** | Executa as campanhas. Gerencia creators, deliverables, aprovações com a marca. |
| **Financeiro** | Recebe e paga. Lançamentos, fluxo de caixa, repasses a creators. |
| **Gestor** | Visão completa do negócio. Pode tudo, configura tudo. |

Legenda das marcações:
- ✅ tem permissão
- ❌ não tem
- 👀 só consulta (sem criar/editar)

---

## Domínio: Comercial

### Pipeline de Oportunidades
**Endpoints**: `OpportunitiesController.*`, `CommercialPipelineStagesController.GetActive`, `OpportunitySourcesController.GetActive`

| Capacidade | Permissions | Comercial | Operação | Financeiro | Gestor |
|---|---|---|---|---|---|
| Listar/buscar oportunidades | `opportunities.get`, `opportunities.getById` | ✅ | 👀 | 👀 | ✅ |
| Criar oportunidade | `opportunities.create` | ✅ | ❌ | ❌ | ✅ |
| Atualizar oportunidade | `opportunities.update` | ✅ | ❌ | ❌ | ✅ |
| Mover no pipeline / mudar estágio | `opportunities.update` | ✅ | ❌ | ❌ | ✅ |
| Listar estágios disponíveis | `commercialPipelineStages.getActive` | ✅ | 👀 | 👀 | ✅ |
| Listar origens disponíveis | `opportunitySources.getActive` | ✅ | 👀 | 👀 | ✅ |

### Propostas
**Endpoints**: `ProposalsController.*`, `ProposalTemplatesController.*`, `ProposalBlocksController.*`

| Capacidade | Permissions | Comercial | Operação | Financeiro | Gestor |
|---|---|---|---|---|---|
| Listar propostas | `proposals.get`, `proposals.getById` | ✅ | 👀 | 👀 | ✅ |
| Criar/editar proposta | `proposals.create`, `proposals.update` | ✅ | ❌ | ❌ | ✅ |
| Gerar PDF | `proposals.generatePdf` | ✅ | 👀 | ❌ | ✅ |
| Enviar link público para marca | `proposals.send`, `proposals.regenerateLink` | ✅ | ❌ | ❌ | ✅ |
| Aprovação interna de proposta (desconto) | `proposals.approve`, `proposals.reject` | ❌ | ❌ | ❌ | ✅ |
| Converter proposta em campanha | `proposals.convertToCampaign` | ✅ | ✅ | ❌ | ✅ |
| Templates de proposta (listar) | `proposalTemplates.get` | ✅ | 👀 | ❌ | ✅ |
| Templates de proposta (criar/editar) | `proposalTemplates.create`, `proposalTemplates.update` | ❌ | ❌ | ❌ | ✅ |
| Blocos de proposta (CRUD) | `proposalBlocks.*` | ❌ | ❌ | ❌ | ✅ |

### Aprovações comerciais internas
**Endpoints**: `OpportunityApprovalsController.*`

| Capacidade | Permissions | Comercial | Operação | Financeiro | Gestor |
|---|---|---|---|---|---|
| Listar aprovações pendentes | `opportunityApprovals.get` | 👀 | ❌ | ❌ | ✅ |
| Aprovar/rejeitar | `opportunityApprovals.approve`, `opportunityApprovals.reject` | ❌ | ❌ | ❌ | ✅ |

---

## Domínio: Operação

### Marcas (cadastro)
**Endpoints**: `BrandsController.*`

| Capacidade | Permissions | Comercial | Operação | Financeiro | Gestor |
|---|---|---|---|---|---|
| Listar/buscar marcas | `brands.get`, `brands.getById` | ✅ | ✅ | 👀 | ✅ |
| Cadastrar marca | `brands.create` | ✅ | ✅ | ❌ | ✅ |
| Editar marca | `brands.update` | ✅ | ✅ | ❌ | ✅ |
| Upload/remover logo | `brands.uploadLogo`, `brands.removeLogo` | ✅ | ✅ | ❌ | ✅ |
| Exportar marcas | `brands.export` | ✅ | ✅ | ✅ | ✅ |

### Creators (base)
**Endpoints**: `CreatorsController.*`, `CreatorSocialHandlesController.*`, `CreatorAccessTokensController.*`

| Capacidade | Permissions | Comercial | Operação | Financeiro | Gestor |
|---|---|---|---|---|---|
| Listar/buscar creators | `creators.get`, `creators.getById`, `creators.getSummary` | ✅ | ✅ | 👀 | ✅ |
| Cadastrar creator | `creators.create` | ❌ | ✅ | ❌ | ✅ |
| Editar creator | `creators.update` | ❌ | ✅ | ❌ | ✅ |
| Upload/remover foto | `creators.uploadPhoto`, `creators.removePhoto` | ❌ | ✅ | ❌ | ✅ |
| Listar campanhas do creator | `creators.getCampaigns` | ✅ | ✅ | ✅ | ✅ |
| Exportar creators | `creators.export` | ✅ | ✅ | ✅ | ✅ |
| Handles sociais (CRUD) | `creatorSocialHandles.*` | ❌ | ✅ | ❌ | ✅ |
| Gerar/revogar token do portal do creator | `creatorAccessTokens.issue`, `creatorAccessTokens.revoke` | ❌ | ✅ | ❌ | ✅ |
| Listar tokens do portal de um creator | `creatorAccessTokens.getByCreator` | ❌ | ✅ | ❌ | ✅ |

### Campanhas
**Endpoints**: `CampaignsController.*`

| Capacidade | Permissions | Comercial | Operação | Financeiro | Gestor |
|---|---|---|---|---|---|
| Listar/buscar campanhas | `campaigns.get`, `campaigns.getById`, `campaigns.getSummary` | ✅ | ✅ | ✅ | ✅ |
| Cadastrar campanha manualmente | `campaigns.create` | ❌ | ✅ | ❌ | ✅ |
| Editar campanha | `campaigns.update` | ❌ | ✅ | ❌ | ✅ |
| Consultar histórico de status | `campaigns.getStatusHistory` | ✅ | ✅ | ✅ | ✅ |

### Vínculo creator × campanha
**Endpoints**: `CampaignCreatorsController.*`, `CampaignCreatorStatusesController.GetActive`

| Capacidade | Permissions | Comercial | Operação | Financeiro | Gestor |
|---|---|---|---|---|---|
| Listar vínculos | `campaignCreators.get`, `campaignCreators.getById`, `campaignCreators.getByCampaign` | ✅ | ✅ | 👀 | ✅ |
| Vincular creator | `campaignCreators.create` | ❌ | ✅ | ❌ | ✅ |
| Atualizar vínculo (valor, status) | `campaignCreators.update` | ❌ | ✅ | ❌ | ✅ |
| Histórico de mudanças de status | `campaignCreators.getStatusHistory` | ❌ | ✅ | ✅ | ✅ |
| Listar status disponíveis | `campaignCreatorStatuses.getActive` | ✅ | ✅ | 👀 | ✅ |

### Entregas (Deliverables)
**Endpoints**: `CampaignDeliverablesController.*`, `DeliverableKindsController.GetActive`, `DeliverableShareLinksController.*`, `DeliverableApprovalsController.*`

| Capacidade | Permissions | Comercial | Operação | Financeiro | Gestor |
|---|---|---|---|---|---|
| Listar entregas | `campaignDeliverables.get`, `campaignDeliverables.getById`, `campaignDeliverables.getByCampaign` | ✅ | ✅ | 👀 | ✅ |
| Criar entrega | `campaignDeliverables.create` | ❌ | ✅ | ❌ | ✅ |
| Editar entrega | `campaignDeliverables.update` | ❌ | ✅ | ❌ | ✅ |
| Excluir entrega | `campaignDeliverables.delete` | ❌ | ✅ | ❌ | ✅ |
| Tipos de entrega disponíveis | `deliverableKinds.getActive` | ✅ | ✅ | 👀 | ✅ |
| Gerar share link para marca aprovar | `deliverableShareLinks.create` | ❌ | ✅ | ❌ | ✅ |
| Revogar share link | `deliverableShareLinks.revoke` | ❌ | ✅ | ❌ | ✅ |
| Listar share links de uma entrega | `deliverableShareLinks.getByDeliverable` | ❌ | ✅ | ❌ | ✅ |
| Lista de entregas com aprovação pendente | `deliverableShareLinks.getPending` | 👀 | ✅ | ❌ | ✅ |
| Histórico de aprovações de entrega | `deliverableApprovals.get`, `deliverableApprovals.getByDeliverable` | 👀 | ✅ | ❌ | ✅ |
| Registrar aprovação manual | `deliverableApprovals.create`, `deliverableApprovals.update` | ❌ | ✅ | ❌ | ✅ |

### Documentos da campanha (briefings, contratos)
**Endpoints**: `CampaignDocumentsController.*`, `CampaignDocumentTemplatesController.*`

| Capacidade | Permissions | Comercial | Operação | Financeiro | Gestor |
|---|---|---|---|---|---|
| Listar documentos | `campaignDocuments.get`, `campaignDocuments.getById`, `campaignDocuments.getByCampaign` | ✅ | ✅ | 👀 | ✅ |
| Criar documento | `campaignDocuments.create` | ❌ | ✅ | ❌ | ✅ |
| Atualizar documento | `campaignDocuments.update` | ❌ | ✅ | ❌ | ✅ |
| Gerar a partir de template | `campaignDocuments.generateFromTemplate` | ❌ | ✅ | ❌ | ✅ |
| Enviar por e-mail | `campaignDocuments.sendEmail` | ❌ | ✅ | ❌ | ✅ |
| Enviar para assinatura digital | `campaignDocuments.sendForSignature` | ❌ | ✅ | ❌ | ✅ |
| Marcar como assinado | `campaignDocuments.markSigned` | ❌ | ✅ | ❌ | ✅ |
| Templates (listar) | `campaignDocumentTemplates.get`, `getById`, `getActiveByDocumentType` | ❌ | ✅ | ❌ | ✅ |
| Templates (CRUD) | `campaignDocumentTemplates.create`, `update`, `delete` | ❌ | ❌ | ❌ | ✅ |

---

## Domínio: Financeiro

### Lançamentos financeiros (contas a pagar/receber)
**Endpoints**: `FinancialEntriesController.*`, `FinancialAccountsController.*`, `FinancialSubcategoriesController.*`

| Capacidade | Permissions | Comercial | Operação | Financeiro | Gestor |
|---|---|---|---|---|---|
| Listar lançamentos | `financialEntries.get`, `financialEntries.getById`, `financialEntries.getByCampaign` | ❌ | 👀 | ✅ | ✅ |
| Resumo a receber/pagar | `financialEntries.getSummary` | ❌ | ❌ | ✅ | ✅ |
| Criar lançamento | `financialEntries.create` | ❌ | ❌ | ✅ | ✅ |
| Atualizar lançamento | `financialEntries.update` | ❌ | ❌ | ✅ | ✅ |
| Marcar como pago/recebido | `financialEntries.markAsPaid` | ❌ | ❌ | ✅ | ✅ |
| Gerar parcelas | `financialEntries.createInstallments` | ❌ | ❌ | ✅ | ✅ |
| Contas financeiras (listar) | `financialAccounts.get`, `financialAccounts.getById` | ❌ | ❌ | ✅ | ✅ |
| Contas financeiras (CRUD) | `financialAccounts.create`, `update`, `delete` | ❌ | ❌ | ❌ | ✅ |
| Subcategorias (listar) | `financialSubcategories.get` | ❌ | ❌ | ✅ | ✅ |
| Subcategorias (CRUD) | `financialSubcategories.create`, `update`, `delete` | ❌ | ❌ | ❌ | ✅ |

### Repasses a creators
**Endpoints**: `CreatorPaymentsController.*`

| Capacidade | Permissions | Comercial | Operação | Financeiro | Gestor |
|---|---|---|---|---|---|
| Listar pagamentos | `creatorPayments.get`, `creatorPayments.getById`, `creatorPayments.getByCampaign`, `getByStatus` | ❌ | 👀 | ✅ | ✅ |
| Registrar pagamento | `creatorPayments.create` | ❌ | ❌ | ✅ | ✅ |
| Editar pagamento | `creatorPayments.update` | ❌ | ❌ | ✅ | ✅ |
| Anexar nota fiscal | `creatorPayments.attachInvoice` | ❌ | ❌ | ✅ | ✅ |
| Marcar como pago | `creatorPayments.markPaid` | ❌ | ❌ | ✅ | ✅ |
| Cancelar pagamento | `creatorPayments.cancel` | ❌ | ❌ | ✅ | ✅ |
| Agendar lote (gateway) | `creatorPayments.scheduleBatch` | ❌ | ❌ | ❌ | ✅ |

### Relatórios financeiros
**Endpoints**: `FinancialReportsController.*`

| Capacidade | Permissions | Comercial | Operação | Financeiro | Gestor |
|---|---|---|---|---|---|
| Fluxo de caixa | `financialReports.getCashFlow` | ❌ | ❌ | ✅ | ✅ |
| Aging | `financialReports.getAging` | ❌ | ❌ | ✅ | ✅ |

---

## Domínio: Geral

### Dashboard
**Endpoints**: `DashboardController.Overview`

| Capacidade | Permissions | Comercial | Operação | Financeiro | Gestor |
|---|---|---|---|---|---|
| Dashboard geral | `dashboard.overview` | ✅ | ✅ | ✅ | ✅ |

### Perfil do próprio usuário
**Endpoints**: `ProfileController.*` (upload de avatar)

| Capacidade | Permissions | Comercial | Operação | Financeiro | Gestor |
|---|---|---|---|---|---|
| Upload de avatar próprio | `profile.uploadAvatar` | ✅ | ✅ | ✅ | ✅ |

---

## Domínio: Configuração

> Em geral, **somente Gestor** mexe em configuração paramétrica. Os outros perfis só **consultam** o que precisam pra trabalhar (via endpoints `GetActive`).

### Agência (logo, dados, template de proposta)
| Capacidade | Permissions | Comercial | Operação | Financeiro | Gestor |
|---|---|---|---|---|---|
| Consultar config da agência | `agencySettings.get` | ✅ | ✅ | ✅ | ✅ |
| Atualizar config / logo / template | `agencySettings.update`, `uploadLogo`, `removeLogo`, `saveProposalTemplate`, `*ProposalTemplateVersion*` | ❌ | ❌ | ❌ | ✅ |

### Pipeline comercial, origens, tags
| Capacidade | Permissions | Comercial | Operação | Financeiro | Gestor |
|---|---|---|---|---|---|
| Configurar estágios do pipeline | `commercialPipelineStages.create`, `update`, `get`, `getById` | ❌ | ❌ | ❌ | ✅ |
| Configurar origens de oportunidade | `opportunitySources.create`, `update`, `get`, `getById` | ❌ | ❌ | ❌ | ✅ |

### Redes sociais (plataformas)
| Capacidade | Permissions | Comercial | Operação | Financeiro | Gestor |
|---|---|---|---|---|---|
| Listar plataformas | `platforms.getActive` | ✅ | ✅ | 👀 | ✅ |
| Configurar plataformas | `platforms.create`, `update`, `get`, `getById` | ❌ | ❌ | ❌ | ✅ |

### Status de creator em campanha
| Capacidade | Permissions | Comercial | Operação | Financeiro | Gestor |
|---|---|---|---|---|---|
| Configurar status | `campaignCreatorStatuses.create`, `update`, `get`, `getById` | ❌ | ❌ | ❌ | ✅ |

### Tipos de entrega
| Capacidade | Permissions | Comercial | Operação | Financeiro | Gestor |
|---|---|---|---|---|---|
| Configurar tipos | `deliverableKinds.create`, `update`, `get`, `getById` | ❌ | ❌ | ❌ | ✅ |

### Integrações e Automações
| Capacidade | Permissions | Comercial | Operação | Financeiro | Gestor |
|---|---|---|---|---|---|
| Listar categorias de integração | `integrationCategories.*` | ❌ | ❌ | ❌ | ✅ |
| Operar conectores e pipelines (CRUD via proxy) | `integrationPlatformProxy.*` | ❌ | ❌ | ❌ | ✅ |
| Listar automações | `automations.get`, `automations.getById`, `automations.logs` | ❌ | ❌ | ❌ | ✅ |
| Configurar automações | `automations.create`, `automations.update` | ❌ | ❌ | ❌ | ✅ |

---

## Endpoints públicos (sem permissão)

Não exigem permission claim — são `[AllowAnonymous]` ou validados por token de URL:

| Endpoint | Quem chama |
|---|---|
| `ProposalPublicController.*` (`/p/{token}`) | Marca abrindo o link público de proposta |
| `DeliverablePublicController.*` (`/d/{token}`) | Marca aprovando entrega via link público |
| `CreatorPortalController.*` (`/portal/{token}`) | Creator acessando seu portal |
| `*ProviderCallback` | Webhooks de IntegrationPlataform |
| `WhatsAppWebhookController.Receive` | _removido_ |

---

## Recomendações finais

1. **Gestor** é praticamente "root operacional" — recomendo manter como **role com `root=true`** no IdM pra ele bypassar a verificação de permission. Simplifica e é o padrão de sistemas multi-perfil.

2. **Comercial** e **Operação** têm **alta sobreposição em consultas** (ambos veem marcas, creators, campanhas) mas **escrita disjunta**: Comercial escreve em propostas/oportunidades; Operação escreve em creators/deliverables/documentos. Marcas é compartilhado (ambos editam) — alinha com a prática de mercado.

3. **Financeiro** é praticamente isolado — só consulta operação pra contextualizar (ver campanha vinculada ao lançamento), mas não escreve.

4. **Dashboard** liberado pra todos pra cada um ver seus indicadores.

5. **Aprovações internas** (de desconto em proposta) sempre só Gestor — é o controle de exceção.

6. **Configuração de integrações e automações** sempre só Gestor — risco alto e exige conhecimento técnico.

7. Considerar criar um **5º perfil "Atendimento"** no futuro pra customer-success se a operação ficar grande: mais leitura ampla, escrita só em comentários/anotações. Por enquanto, "Operação" cobre.
