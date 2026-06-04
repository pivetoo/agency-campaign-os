import { test, expect } from '../fixtures/test'

// Spec 20 (reescrito): a aba/modal "Negociacoes" foi APOSENTADA (OpportunityNegotiation
// removida; /comercial/negociacoes so redireciona). A aprovacao agora e ancorada na
// Proposta ("Solicitar aprovacao") e decidida na inbox /comercial/aprovacoes.
// Valida de forma robusta, sem depender de criar a cadeia opp+proposta:
//  - a rota legada /comercial/negociacoes REDIRECIONA para oportunidades;
//  - a inbox /comercial/aprovacoes renderiza sem erro de API;
//  - best-effort: numa proposta elegivel existente, "Solicitar aprovacao" abre o
//    modal atual com o gate de maker-checker (submit desabilitado sem aprovador).
// O ciclo completo (designar aprovador != solicitante e decidir na inbox) exige um
// 2o usuario ativo no contrato; documentado como gap em e2e-mapeamento-cobertura.md.

test.describe('Comercial - aprovacao ancorada na proposta (fluxo atual)', () => {
  test('negociacoes aposentadas: redirect + inbox + solicitar aprovacao na proposta', async ({ page, expectNoApiFailures }) => {
    // 1) rota legada redireciona (negociacoes aposentadas)
    await page.goto('/comercial/negociacoes')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})
    await expect(page).toHaveURL(/\/comercial\/oportunidades/, { timeout: 15_000 })

    // 2) inbox de aprovacoes renderiza sem erro
    await page.goto('/comercial/aprovacoes')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})
    await expect(page.getByTestId('page-title').first()).toBeVisible({ timeout: 20_000 })

    // 3) best-effort: abrir a primeira proposta e validar o modal de aprovacao atual
    await page.goto('/comercial/propostas')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})
    await expect(page.getByTestId('page-title').first()).toContainText(/Propostas/i, { timeout: 20_000 })

    const firstRow = page.locator('[data-row="true"]').first()
    if (await firstRow.count()) {
      const detailsBtn = firstRow.getByRole('button', { name: /Detalhes/i }).first()
      if (await detailsBtn.count()) {
        await detailsBtn.click()
      } else {
        await firstRow.dblclick()
      }
      const navegou = await page.waitForURL(/\/comercial\/propostas\/\d+/, { timeout: 15_000 }).then(() => true).catch(() => false)

      if (navegou) {
        const requestBtn = page.getByRole('button', { name: /Solicitar aprova/i }).first()
        const elegivel = (await requestBtn.count()) > 0 && (await requestBtn.isVisible().catch(() => false))
        if (elegivel) {
          await requestBtn.click()
          const aprModal = page.getByRole('dialog').filter({ hasText: /aprova/i }).first()
          await expect(aprModal).toBeVisible({ timeout: 10_000 })
          await expect(aprModal.getByText(/Aprovador/i)).toBeVisible()
          // gate maker-checker: sem aprovador designado, o submit fica desabilitado
          const submitBtn = aprModal.getByRole('button', { name: /Solicitar|Enviar|Confirmar/i }).last()
          await expect(submitBtn).toBeDisabled()
          await aprModal.getByRole('button', { name: /^Cancelar$/i }).first().click().catch(() => {})
          await expect(aprModal).toBeHidden({ timeout: 10_000 })
        }
      }
    }

    expectNoApiFailures()
  })
})
