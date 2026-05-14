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

export const crud = {
  add: (page: Page) => page.getByTestId('crud-add-button'),
  edit: (page: Page) => page.getByTestId('crud-edit-button'),
  delete: (page: Page) => page.getByTestId('crud-delete-button'),
  view: (page: Page) => page.getByTestId('crud-view-button'),
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

export const campaign = {
  addCreatorButton: (p: Page) => p.getByTestId('campaign-add-creator-button'),
}

export const creator = {
  addHandleButton: (p: Page) => p.getByTestId('creator-add-handle-button'),
}
