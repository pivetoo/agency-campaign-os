import { test, expect } from '../fixtures/test'
import { createProposalForNewOpportunity, proposalDetail, sendProposalViaModal } from '../fixtures/helpers'

// Cobre o A4: depois de enviada, a proposta não é mais editável. As ações de item
// (adicionar/aplicar template/editar/excluir) desaparecem da tela de detalhe.

test.describe('Proposta - imutável após envio (A4)', () => {
  test('ações de item somem depois que a proposta é enviada', async ({ page }) => {
    const oppName = `E2E LockProp ${Date.now()}`
    await createProposalForNewOpportunity(page, oppName)

    // rascunho: ações de item disponíveis
    await expect(proposalDetail.itemsActions(page)).toBeVisible({ timeout: 15_000 })

    // enviar
    await sendProposalViaModal(page)

    // enviada: ações de item escondidas (edição bloqueada no backend e na UI)
    await expect(proposalDetail.itemsActions(page)).toBeHidden({ timeout: 15_000 })
  })
})
