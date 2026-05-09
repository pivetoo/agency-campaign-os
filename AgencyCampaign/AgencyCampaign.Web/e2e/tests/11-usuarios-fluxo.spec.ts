import { test, expect } from '../fixtures/test'

test.describe('Usuarios - gestao do contrato ativo', () => {
  test('cria usuario novo no contrato ativo', async ({ page, expectNoApiFailures }) => {
    const stamp = Date.now()
    const username = `e2e.user${stamp}`
    const email = `e2e.user${stamp}@mainstay.com.br`
    const fullName = `E2E User ${stamp}`

    await page.goto('/usuarios')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})

    // o titulo da pagina pode ser "Usuarios" ou diferente; clicar no botao "Novo usuário"
    await page.getByRole('button', { name: /Novo usuário/i }).first().click()

    const modal = page.getByRole('dialog').filter({ hasText: /Novo usuário/i })
    await expect(modal).toBeVisible({ timeout: 10_000 })

    // preencher por placeholder
    await modal.getByPlaceholder('Maria Silva').fill(fullName)
    await modal.getByPlaceholder('maria.silva').fill(username)
    await modal.getByPlaceholder('maria@empresa.com').fill(email)
    await modal.getByPlaceholder(/Mínimo 6 caracteres/i).fill('senha123')

    // perfil: ja vem preenchido com o role default (Administrador) — pular se ja selecionado
    const perfilPlaceholder = modal.getByText('Selecione um perfil').first()
    if (await perfilPlaceholder.count()) {
      await perfilPlaceholder.click()
      const firstOption = page.locator('[role="option"]').first()
      await expect(firstOption).toBeVisible({ timeout: 10_000 })
      await firstOption.click()
    }

    // Salvar (botao "Criar usuário")
    await modal.getByRole('button', { name: /Criar usuário/i }).first().click()

    // modal fecha
    await expect(modal).toBeHidden({ timeout: 15_000 })

    // usuario aparece na listagem
    await expect(page.getByText(fullName).first()).toBeVisible({ timeout: 15_000 })

    expectNoApiFailures()
  })
})
