import { test, expect } from '../fixtures/test'

// Spec 27: aba Automacoes (triggers/actions)
// Cobre o fluxo de visualizacao da lista, abertura do modal de Nova automacao,
// validacao do botao Salvar (disabled sem campos obrigatorios) e edicao de
// automacao existente (quando houver). Nao cria automacao real.

test.describe('Configuracao - Automacoes (triggers e acoes)', () => {
  test('lista, valida form de Nova automacao e edita existente quando houver', async ({ page, expectNoApiFailures }) => {
    await page.goto('/configuracao/integracoes')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})

    // 1) abre aba Automacoes
    await page.getByRole('tab', { name: /Automa[çc][õo]es/i }).click()
    await page.waitForLoadState('networkidle', { timeout: 10_000 }).catch(() => {})

    // 2) renderiza: empty state OU lista com badges/Nova automacao
    const emptyState = page.getByText(/Nenhuma automa[çc][ãa]o configurada/i).first()
    const novaBtn = page.getByRole('button', { name: /Nova automa[çc][ãa]o/i }).first()
    const criarPrimeiraBtn = page.getByRole('button', { name: /Criar primeira automa[çc][ãa]o/i }).first()

    // pelo menos um botao de criar deve estar visivel
    const criarBtn = (await novaBtn.count()) > 0 ? novaBtn : criarPrimeiraBtn
    await expect(criarBtn).toBeVisible({ timeout: 15_000 })

    // 3) abre modal de nova automacao
    await criarBtn.click()
    const modal = page.getByRole('dialog').filter({ hasText: /Nova automa[çc][ãa]o/i })
    await expect(modal).toBeVisible({ timeout: 10_000 })

    // 4) campos obrigatorios visiveis
    for (const label of ['Nome', 'Disparar quando', 'Categoria de integração', 'Integração', 'Conta conectada', 'Ação a executar']) {
      await expect(modal.getByText(label, { exact: false }).first()).toBeVisible()
    }

    // 5) Salvar fica disabled sem nome + connector + pipeline
    const salvarBtn = modal.getByRole('button', { name: /^Salvar$/ })
    await expect(salvarBtn).toBeDisabled()

    // 6) preencher nome nao habilita (ainda falta connector + pipeline)
    const nameInput = modal.locator('input').first()
    await nameInput.fill('E2E Automacao QA')
    await expect(salvarBtn).toBeDisabled()

    // 7) cancela
    await modal.getByRole('button', { name: /^Cancelar$/ }).click()
    await expect(modal).toBeHidden({ timeout: 10_000 })

    // 8) se houver automacao na lista, edita a primeira (so para abrir o modal em modo edit)
    const editButton = page.getByRole('button', { name: /^Editar$/ }).first()
    if ((await editButton.count()) > 0 && (await emptyState.count()) === 0) {
      await editButton.click()
      const editModal = page.getByRole('dialog').filter({ hasText: /Editar automa[çc][ãa]o/i })
      await expect(editModal).toBeVisible({ timeout: 10_000 })

      // ao editar, nome ja vem preenchido
      const editName = editModal.locator('input').first()
      await expect(editName).not.toHaveValue('')

      // botao Salvar fica habilitado em automacao valida ja existente
      await expect(editModal.getByRole('button', { name: /^Salvar$/ })).toBeEnabled()

      await editModal.getByRole('button', { name: /^Cancelar$/ }).click()
      await expect(editModal).toBeHidden({ timeout: 10_000 })
    }

    expectNoApiFailures()
  })
})
