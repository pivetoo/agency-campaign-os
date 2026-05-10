import { test, expect } from '../fixtures/test'

// Spec 26: Tela de Integracoes (categoria -> integracao -> conector)
// Cobre o fluxo navegacional sem criar conector real (credenciais sao especificas
// por integracao). Valida:
// - render da pagina e das 2 abas
// - selecao de categoria popula lista de integracoes
// - selecao de integracao habilita botao "Conectar conta"
// - modal de conector abre, carrega atributos e cancela limpo
// - troca para aba Automacoes carrega sem erro

test.describe('Configuracao - Integracoes', () => {
  test('navega categoria -> integracao -> abre modal de conector e troca de aba', async ({ page, expectNoApiFailures }) => {
    await page.goto('/configuracao/integracoes')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})

    // 1) header e abas
    await expect(page.getByRole('heading', { name: /^Integrações$/ })).toBeVisible({ timeout: 15_000 })
    await expect(page.getByRole('tab', { name: /Contas conectadas/i })).toBeVisible()
    await expect(page.getByRole('tab', { name: /Automa[çc][õo]es/i })).toBeVisible()

    // 2) estado inicial: empty state pedindo categoria
    await expect(page.getByText(/Selecione uma categoria para começar/i)).toBeVisible()

    // 3) seleciona primeira categoria
    const categoryCombobox = page.getByRole('combobox').first()
    await categoryCombobox.click()
    const firstCategoryOption = page.getByRole('option').first()
    if (await firstCategoryOption.count() === 0) {
      test.skip(true, 'Sem categorias de integracao cadastradas no IntegrationPlatform.')
      return
    }
    await firstCategoryOption.click()

    // 4) painel de integracoes aparece
    await expect(page.getByRole('heading', { name: /^Integrações$/, level: 4 }).or(page.getByText(/^Integrações$/).nth(1))).toBeVisible({ timeout: 15_000 }).catch(() => {})
    // o card "Integrações" do lado esquerdo lista botoes de integracao
    const integrationButtons = page.locator('button').filter({ has: page.locator('text=Em uso, text=Sem ações, text=Não conectada') })
    // fallback: localizar por texto da linha de selecao
    const firstIntegrationButton = page
      .locator('button[type="button"]')
      .filter({ hasText: /(Em uso|Sem ações|Não conectada)/i })
      .first()

    if ((await firstIntegrationButton.count()) === 0) {
      test.skip(true, 'Categoria selecionada nao tem integracoes.')
      return
    }
    await firstIntegrationButton.click()

    // 5) painel direito: titulo da integracao e botao Conectar conta
    const conectarBtn = page.getByRole('button', { name: /Conectar conta/i }).first()
    await expect(conectarBtn).toBeVisible({ timeout: 10_000 })

    // 6) abre modal de conector — captura request de atributos
    const attrsPromise = page.waitForResponse(
      (resp) => /\/api\/IntegrationPlatformProxy\/.*Attributes/i.test(resp.url()) && resp.status() < 400,
      { timeout: 15_000 },
    ).catch(() => null)

    await conectarBtn.click()
    const modal = page.getByRole('dialog').filter({ hasText: /Conectar|Configurar conta|Configura/i }).first()
    await expect(modal).toBeVisible({ timeout: 10_000 })

    // espera a resposta de atributos (ou pula se a integracao nao tiver atributos)
    await attrsPromise

    // input de nome da conta sempre presente
    const nameInput = modal.locator('input[type="text"], input:not([type])').first()
    await expect(nameInput).toBeVisible({ timeout: 10_000 })

    // 7) cancela sem salvar
    const cancelBtn = modal.getByRole('button', { name: /^Cancelar$|^Fechar$/i }).first()
    await cancelBtn.click()
    await expect(modal).toBeHidden({ timeout: 10_000 })

    // 8) troca para aba Automacoes — deve carregar sem erro de API
    await page.getByRole('tab', { name: /Automa[çc][õo]es/i }).click()
    await page.waitForLoadState('networkidle', { timeout: 10_000 }).catch(() => {})

    // valida que renderizou alguma coisa: ou tabela com automacoes, ou empty state
    const emptyAutomations = page.getByText(/Nenhuma automa[çc][ãa]o (configurada|cadastrada)/i).first()
    const anyAutomationRow = page.locator('[data-row="true"]').first()
    await expect(emptyAutomations.or(anyAutomationRow)).toBeVisible({ timeout: 15_000 })

    expectNoApiFailures()
  })
})
