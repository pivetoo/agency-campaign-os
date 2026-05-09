import { test, expect } from '../fixtures/test'

test.describe('Permissoes - root vs nao-root', () => {
  test('user root acessa /usuarios sem ver Acesso restrito', async ({ page, expectNoApiFailures }) => {
    await page.goto('/usuarios')
    await page.waitForLoadState('networkidle', { timeout: 20_000 }).catch(() => {})

    // 1) tela NAO mostra "Acesso restrito"
    await expect(page.getByText(/Acesso restrito/i)).toHaveCount(0, { timeout: 5_000 })

    // 2) heading "Usuarios"
    await expect(page.getByRole('heading', { name: /Usuários/i }).first()).toBeVisible({ timeout: 10_000 })

    // 3) botao "Novo usuario"
    await expect(page.getByRole('button', { name: /Novo usuário/i }).first()).toBeVisible({ timeout: 10_000 })

    expectNoApiFailures()
  })

  test('JWT do storage state tem claim root=true', async () => {
    const fs = await import('node:fs')
    const path = await import('node:path')
    const url = await import('node:url')

    const here = path.dirname(url.fileURLToPath(import.meta.url))
    const stateFile = path.join(here, '..', '.auth', 'user.json')
    const state = JSON.parse(fs.readFileSync(stateFile, 'utf-8'))

    let token = ''
    for (const origin of state.origins ?? []) {
      for (const item of origin.localStorage ?? []) {
        if (item.name === '@Archon:accessToken') {
          token = item.value
        }
      }
    }
    expect(token).not.toBe('')

    // decodifica payload
    const payloadB64 = token.split('.')[1]
    const payload = JSON.parse(Buffer.from(payloadB64, 'base64').toString('utf-8'))

    // claim 'root' pode vir como string 'true' ou array ['true', 'true', ...] quando user tem multiplas roles isroot
    const rootClaim = payload.root
    const isRoot = Array.isArray(rootClaim) ? rootClaim.includes('true') : rootClaim === 'true'
    expect(isRoot).toBe(true)
    expect(payload.username).toBeTruthy()
    expect(payload.contract_id).toBeTruthy()
  })
})
