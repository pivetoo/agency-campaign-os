import { type Page, type Locator, expect } from '@playwright/test'

/**
 * Helpers compartilhados de Page Object para testes E2E do Kanvas.
 *
 * Premissas:
 * - Componentes-chave (PageLayout, ConfirmModal, Dashboard KPIs, etc) tem `data-testid` plantado.
 * - Sempre que possivel preferir `getByTestId` ao inves de seletores por texto/role+name.
 */

export const pageTitle = (page: Page): Locator => page.getByTestId('page-title')

export async function expectPageTitle(page: Page, matcher: string | RegExp, timeoutMs = 20_000) {
  await expect(pageTitle(page).first()).toBeVisible({ timeout: timeoutMs })
  if (typeof matcher === 'string') {
    await expect(pageTitle(page).first()).toContainText(matcher)
  } else {
    await expect(pageTitle(page).first()).toHaveText(matcher)
  }
}

// PageLayout renderiza as acoes em duas barras (desktop e mobile responsivo),
// ambas com o mesmo data-testid; filtra pela visivel para evitar strict mode.
export const crud = {
  add: (page: Page) => page.locator('[data-testid="crud-add-button"]:visible').first(),
  edit: (page: Page) => page.locator('[data-testid="crud-edit-button"]:visible').first(),
  delete: (page: Page) => page.locator('[data-testid="crud-delete-button"]:visible').first(),
  view: (page: Page) => page.locator('[data-testid="crud-view-button"]:visible').first(),
}

export const dialog = {
  confirm: (page: Page) => page.getByTestId('dialog-confirm'),
  confirmButton: (page: Page) => page.getByTestId('dialog-confirm-button'),
  cancelButton: (page: Page) => page.getByTestId('dialog-cancel-button'),
}

export async function confirmDelete(page: Page) {
  await expect(dialog.confirm(page)).toBeVisible({ timeout: 10_000 })
  await dialog.confirmButton(page).click()
  await expect(dialog.confirm(page)).toBeHidden({ timeout: 10_000 })
}

export function row(page: Page): Locator {
  return page.locator('[data-row="true"]')
}

export function rowWithText(page: Page, text: string): Locator {
  return row(page).filter({ hasText: text })
}

/**
 * Tenta achar e clicar em "Salvar" dentro de um modal/dialog ativo.
 * Aceita textos alternativos: Salvar | Criar | Confirmar.
 */
export async function clickSaveInDialog(dialogLocator: Locator) {
  const candidates = dialogLocator.getByRole('button', { name: /^Salvar$|^Criar$|^Confirmar$|^Criar e continuar$/i })
  await candidates.first().click()
}

/**
 * Envia a proposta pelo modal "Enviar proposta" (ProposalSendModal). Cobre os
 * dois caminhos: conector de envio configurado (preenche destinatario e envia)
 * ou nao configurado (botao "Marcar como enviada"). Deixa a proposta como Enviada.
 */
export async function sendProposalViaModal(page: Page) {
  await page.getByRole('button', { name: /^Enviar$/i }).first().click()
  const sendModal = page.getByRole('dialog').filter({ hasText: /Enviar proposta/i })
  await expect(sendModal).toBeVisible({ timeout: 10_000 })
  // o modal checa o conector de envio (estado de loading); espera estabilizar
  // entre o formulario de e-mail (conector ativo) e o aviso "Marcar como enviada"
  const email = sendModal.locator('input[type="email"]').first()
  const markSent = sendModal.getByRole('button', { name: /Marcar como enviada/i }).first()
  await Promise.race([
    email.waitFor({ state: 'visible', timeout: 10_000 }).catch(() => {}),
    markSent.waitFor({ state: 'visible', timeout: 10_000 }).catch(() => {}),
  ])
  await page.waitForTimeout(400)
  if (await email.isVisible().catch(() => false)) {
    await email.fill('e2e.qa@example.com')
    await sendModal.getByRole('button', { name: /Enviar email|Enviar por WhatsApp/i }).first().click()
  } else {
    await markSent.click()
  }
  await expect(sendModal).toBeHidden({ timeout: 15_000 })
}

export const dashboardKpi = {
  campanhasAtivas: (page: Page) => page.getByTestId('dashboard-kpi-campanhas-ativas'),
  marcas: (page: Page) => page.getByTestId('dashboard-kpi-marcas'),
  influenciadores: (page: Page) => page.getByTestId('dashboard-kpi-influenciadores'),
  entregasPendentes: (page: Page) => page.getByTestId('dashboard-kpi-entregas-pendentes'),
}

export const portal = {
  page: (p: Page) => p.getByTestId('public-creator-portal-page'),
  tabCampanhas: (p: Page) => p.getByTestId('portal-tab-campanhas'),
  tabContratos: (p: Page) => p.getByTestId('portal-tab-contratos'),
  tabPagamentos: (p: Page) => p.getByTestId('portal-tab-pagamentos'),
  tabPerfil: (p: Page) => p.getByTestId('portal-tab-perfil'),
  pixInput: (p: Page) => p.getByTestId('portal-pix-input'),
  saveBankButton: (p: Page) => p.getByTestId('portal-save-bank-button'),
  uploadNfInput: (p: Page) => p.getByTestId('portal-upload-nf-input'),
}

export const publicProposal = {
  page: (p: Page) => p.getByTestId('public-proposal-page'),
}

export const opportunity = {
  stageColumn: (page: Page, stageName?: string) =>
    stageName
      ? page.locator(`[data-testid="opportunity-stage-column"][data-stage="${stageName}"]`)
      : page.getByTestId('opportunity-stage-column'),
  card: (page: Page, id?: string | number) =>
    id
      ? page.locator(`[data-testid="opportunity-card"][data-opportunity-id="${id}"]`)
      : page.getByTestId('opportunity-card'),
  cardByText: (page: Page, text: string) =>
    page.getByTestId('opportunity-card').filter({ hasText: text }),
}

export const proposal = {
  generateLinkButton: (p: Page) => p.getByTestId('proposal-generate-public-link-button'),
  shareLinkResult: (p: Page) => p.getByTestId('proposal-share-link-result'),
  shareLinkInput: (p: Page) => p.getByTestId('proposal-share-link-input'),
}

export const publicProposalDecision = {
  nameInput: (p: Page) => p.getByTestId('public-proposal-name'),
  acceptButton: (p: Page) => p.getByTestId('public-proposal-accept'),
  rejectButton: (p: Page) => p.getByTestId('public-proposal-reject'),
  accepted: (p: Page) => p.getByTestId('public-proposal-accepted'),
}

export const opportunityDetail = {
  currentStage: (p: Page) => p.getByTestId('opportunity-current-stage'),
  stageMenu: (p: Page) => p.getByTestId('opportunity-stage-menu'),
  // primeiro item do menu com o comportamento final desejado (1=Ganha, 2=Perdida, 0=aberto)
  moveStageByFinal: (p: Page, final: 0 | 1 | 2) =>
    p.locator(`[data-testid="opportunity-move-stage"][data-stage-final="${final}"]`).first(),
  finalConfirm: (p: Page) => p.getByTestId('opportunity-final-confirm'),
}

export const proposalDetail = {
  itemsActions: (p: Page) => p.getByTestId('proposal-items-actions'),
}

/**
 * Cria uma oportunidade pela lista (/comercial/oportunidades) com nome, marca e
 * (opcional) responsavel, e abre o detalhe. Retorna a URL do detalhe.
 * Segue o mesmo fluxo dos specs 03/19.
 */
export async function createOpportunityAndOpenDetail(
  page: Page,
  name: string,
  options: { estimatedValue?: string; withResponsible?: boolean } = {},
): Promise<void> {
  await page.goto('/comercial/oportunidades')
  await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})
  await crud.add(page).click()
  const oppModal = page.getByRole('dialog').filter({ hasText: /Nova oportunidade/i })
  await expect(oppModal).toBeVisible({ timeout: 10_000 })
  await oppModal.getByLabel(/Nome da oportunidade/i).fill(name)
  await oppModal.locator(':text("Marca")').locator('..').locator('button, [role="combobox"]').first().click()
  await page.locator('[role="option"]').first().click()
  await oppModal.locator('#opportunity-estimated-value').fill(options.estimatedValue ?? '10000')
  if (options.withResponsible) {
    const respLabel = oppModal.locator('label', { hasText: /^Responsável comercial$/ }).first()
    await expect(respLabel).toBeVisible({ timeout: 5_000 })
    await respLabel.locator('xpath=following::*[self::button or @role="combobox"][1]').first().click()
    await page.locator('[role="option"]').filter({ hasNotText: /^Nenhum$/i }).first().click()
  }
  await oppModal.getByRole('button', { name: /^Salvar$/i }).first().click()
  await expect(oppModal).toBeHidden({ timeout: 15_000 })

  const pageSizeSelect = page.locator('select').filter({ hasText: /5|10|20|50/ }).first()
  if (await pageSizeSelect.count()) await pageSizeSelect.selectOption('50').catch(() => {})

  const targetRow = rowWithText(page, name).first()
  await expect(targetRow).toBeVisible({ timeout: 15_000 })
  const openBtn = targetRow.locator('button').filter({ hasText: /Abrir/i }).first()
  if (await openBtn.count()) {
    await openBtn.click()
  } else {
    await targetRow.dblclick()
  }
  await page.waitForURL(/\/comercial\/oportunidades\/\d+/, { timeout: 15_000 })
}

/**
 * Cria uma oportunidade com responsavel e uma proposta vinculada, terminando no
 * detalhe da proposta (/comercial/propostas/:id). Segue o fluxo do spec 03.
 */
export async function createProposalForNewOpportunity(page: Page, oppName: string): Promise<void> {
  await page.goto('/comercial/oportunidades')
  await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})
  await crud.add(page).click()
  const oppModal = page.getByRole('dialog').filter({ hasText: /Nova oportunidade/i })
  await expect(oppModal).toBeVisible({ timeout: 10_000 })
  await oppModal.getByLabel(/Nome da oportunidade/i).fill(oppName)
  await oppModal.locator(':text("Marca")').locator('..').locator('button, [role="combobox"]').first().click()
  await page.locator('[role="option"]').first().click()
  await oppModal.locator('#opportunity-estimated-value').fill('12000')
  const respLabel = oppModal.locator('label', { hasText: /^Responsável comercial$/ }).first()
  await expect(respLabel).toBeVisible({ timeout: 5_000 })
  await respLabel.locator('xpath=following::*[self::button or @role="combobox"][1]').first().click()
  await page.locator('[role="option"]').filter({ hasNotText: /^Nenhum$/i }).first().click()
  await oppModal.getByRole('button', { name: /^Salvar$/i }).first().click()
  await expect(oppModal).toBeHidden({ timeout: 15_000 })

  await page.goto('/comercial/propostas')
  await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})
  await crud.add(page).click()
  const propModal = page.getByRole('dialog').filter({ hasText: /Criar proposta comercial/i })
  await expect(propModal).toBeVisible({ timeout: 10_000 })
  await propModal.locator(':text("Oportunidade")').locator('..').locator('button, [role="combobox"]').first().click()
  const search = page.locator('input[placeholder*="Buscar oportunidade" i]').first()
  if (await search.count()) await search.fill(oppName)
  await page.locator('[role="option"]', { hasText: oppName }).first().click()
  await clickSaveInDialog(propModal)
  await expect(propModal).toBeHidden({ timeout: 15_000 })
  await page.waitForURL(/\/comercial\/propostas\/\d+/, { timeout: 15_000 })
}

/**
 * Gera o share link publico da proposta (ja enviada) e retorna o token.
 */
export async function generateShareLinkToken(page: Page): Promise<string> {
  const generateBtn = proposal.generateLinkButton(page)
  await expect(generateBtn).toBeVisible({ timeout: 15_000 })
  const sharePromise = page.waitForResponse(
    (response) => /\/share-links\/Create/i.test(response.url()) && response.request().method() === 'POST',
    { timeout: 15_000 },
  )
  await generateBtn.click()
  const shareResponse = await sharePromise
  expect(shareResponse.ok(), `share-links/Create retornou ${shareResponse.status()}`).toBeTruthy()
  const body = await shareResponse.json()
  const token: string | undefined = body?.data?.token ?? body?.token
  expect(token, 'esperava token na resposta de share-links/Create').toBeTruthy()
  return token as string
}

export const campaign = {
  addCreatorButton: (p: Page) => p.getByTestId('campaign-add-creator-button'),
}

export const creator = {
  addHandleButton: (p: Page) => p.getByTestId('creator-add-handle-button'),
}
