import { test as setup, expect } from '@playwright/test'
import * as path from 'node:path'
import * as fs from 'node:fs'
import { fileURLToPath } from 'node:url'
import { env, assertCredentials } from '../fixtures/env'

const here = path.dirname(fileURLToPath(import.meta.url))
const storageStatePath = path.join(here, '..', '.auth', 'user.json')

setup('autenticar via OIDC e salvar storageState', async ({ page }) => {
  assertCredentials()

  fs.mkdirSync(path.dirname(storageStatePath), { recursive: true })

  await page.goto('/')

  await page.waitForLoadState('domcontentloaded')

  await page.waitForURL(/auth\.mainstay\.com\.br/, { timeout: 20_000 })

  const emailField = page
    .getByRole('textbox', { name: /E-mail|usuário|username/i })
    .first()
  const passwordField = page
    .getByRole('textbox', { name: /Senha|password/i })
    .or(page.locator('input[type="password"]'))
    .first()

  await emailField.waitFor({ state: 'visible', timeout: 15_000 })
  await emailField.fill(env.user)
  await passwordField.fill(env.password)

  const submitButton = page.getByRole('button', { name: /Entrar|Login/i }).first()
  await submitButton.click()

  try {
    await page.waitForURL(/kanvas\.mainstay\.com\.br/, { timeout: 30_000 })
  } catch {
    // se nao redirecionou, provavelmente o IdM mostrou um erro no proprio form
    if (page.url().includes('auth.mainstay.com.br')) {
      const formError = await page
        .locator('form')
        .first()
        .innerText()
        .catch(() => '')
      throw new Error(
        `IdentityManagement nao redirecionou para o Kanvas. Conteudo visivel do form de login: "${formError.replace(/\s+/g, ' ').trim()}". ` +
          `Verifique se o usuario ${env.user} esta ativo e tem contrato associado.`
      )
    }
    throw new Error(`Login nao concluiu. URL atual: ${page.url()}`)
  }

  await expect(page.getByText(/Campanhas ativas|Marcas|Creators/i).first()).toBeVisible({ timeout: 30_000 })

  await page.context().storageState({ path: storageStatePath })
})
