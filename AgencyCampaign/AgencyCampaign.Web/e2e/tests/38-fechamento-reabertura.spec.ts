import { test, expect } from '../fixtures/test'
import { createOpportunityAndOpenDetail, opportunityDetail, dialog } from '../fixtures/helpers'

// Cobre o fluxo determinístico (sem drag) de ganhar/reabrir pelo detalhe da oportunidade.
// Exercita o A5 (reabertura pelo detalhe) e a lógica de fechamento por estágio final.

test.describe('Oportunidade - fechar como ganha e reabrir pelo detalhe', () => {
  test('fecha como ganha e reabre para etapa aberta', async ({ page, expectNoApiFailures }) => {
    const oppName = `E2E WinReopen ${Date.now()}`
    await createOpportunityAndOpenDetail(page, oppName)

    // estado inicial: aberta
    await expect(opportunityDetail.currentStage(page)).toBeVisible({ timeout: 15_000 })
    await expect(opportunityDetail.currentStage(page)).toHaveAttribute('data-closed', 'false')

    // menu de estágio -> primeira etapa Ganha (final=1) -> modal de fechamento -> confirmar
    await opportunityDetail.stageMenu(page).click()
    await opportunityDetail.moveStageByFinal(page, 1).click()
    const confirm = opportunityDetail.finalConfirm(page)
    await expect(confirm).toBeVisible({ timeout: 10_000 })
    await confirm.click()

    // oportunidade fechou
    await expect
      .poll(async () => opportunityDetail.currentStage(page).getAttribute('data-closed'), { timeout: 15_000 })
      .toBe('true')

    // reabrir: menu -> etapa aberta (final=0) -> ConfirmModal de reabertura
    await opportunityDetail.stageMenu(page).click()
    await opportunityDetail.moveStageByFinal(page, 0).click()
    await expect(dialog.confirm(page)).toBeVisible({ timeout: 10_000 })
    await dialog.confirmButton(page).click()
    await expect(dialog.confirm(page)).toBeHidden({ timeout: 10_000 })

    // voltou a ficar aberta
    await expect
      .poll(async () => opportunityDetail.currentStage(page).getAttribute('data-closed'), { timeout: 15_000 })
      .toBe('false')

    expectNoApiFailures()
  })
})
