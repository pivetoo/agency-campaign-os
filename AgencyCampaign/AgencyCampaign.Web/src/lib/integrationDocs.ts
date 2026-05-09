// Mapa de links de documentacao oficial dos provedores, indexado por
// (identifier da integracao, field do atributo). Usado pelo ConnectorConfigModal
// para mostrar um link "Onde encontrar?" ao lado dos campos sensiveis.
//
// Quando o IntegrationPlatform passar a guardar isso no banco, esse arquivo deixa de
// existir e os links sao servidos pela API.

type DocLink = {
  url: string
  label?: string
}

type IntegrationDocs = Record<string, Record<string, DocLink>>

export const integrationDocs: IntegrationDocs = {
  // ============ ASSINATURA DIGITAL ============
  zapsign: {
    api_token: { url: 'https://docs.zapsign.com.br/recursos/comecando-a-usar/api-tokens', label: 'Como obter o API Token' },
  },
  clicksign: {
    api_token: { url: 'https://developers.clicksign.com/docs/autenticando-as-requests', label: 'Como gerar o access_token' },
  },
  d4sign: {
    api_token: { url: 'https://ajuda.d4sign.com.br/portal/pt/kb/articles/onde-encontrar-token-de-uso-da-api-d4sign', label: 'Onde encontrar o tokenAPI' },
    crypt_key: { url: 'https://ajuda.d4sign.com.br/portal/pt/kb/articles/onde-encontrar-cryptkey-no-d4sign', label: 'Onde encontrar a cryptKey' },
  },
  autentique: {
    api_token: { url: 'https://docs.autentique.com.br/api/sobre/criando-um-token', label: 'Criando um token' },
  },
  docusign: {
    integration_key: { url: 'https://developers.docusign.com/platform/build-integration/', label: 'Como obter Integration Key' },
    rsa_private_key: { url: 'https://developers.docusign.com/platform/auth/jwt/jwt-get-token/', label: 'Como gerar RSA Private Key (JWT)' },
    user_id: { url: 'https://developers.docusign.com/platform/auth/jwt/jwt-get-token/', label: 'Como obter User ID' },
    account_id: { url: 'https://support.docusign.com/s/document-item?bundleId=pik1583277475390&topicId=lwf1583277376371', label: 'Como obter Account ID' },
  },

  // ============ EMAIL ============
  smtp: {
    password: { url: 'https://support.google.com/accounts/answer/185833', label: 'Gmail: como criar uma App Password' },
  },
  sendgrid: {
    api_key: { url: 'https://app.sendgrid.com/settings/api_keys', label: 'Onde gerar a API Key' },
  },
  mailgun: {
    api_key: { url: 'https://app.mailgun.com/settings/api_security', label: 'Onde gerar a Private API Key' },
    domain: { url: 'https://help.mailgun.com/hc/en-us/articles/202256730', label: 'Como configurar um domain' },
  },
  resend: {
    api_key: { url: 'https://resend.com/api-keys', label: 'Onde gerar a API Key' },
    target_secret: { url: 'https://resend.com/docs/dashboard/webhooks/verify-webhooks-requests', label: 'Como pegar o webhook signing secret' },
  },
  'aws-ses': {
    access_key_id: { url: 'https://docs.aws.amazon.com/IAM/latest/UserGuide/id_credentials_access-keys.html', label: 'Como criar Access Keys' },
    secret_access_key: { url: 'https://docs.aws.amazon.com/IAM/latest/UserGuide/id_credentials_access-keys.html', label: 'Como obter Secret Access Key' },
    configuration_set: { url: 'https://docs.aws.amazon.com/ses/latest/dg/using-configuration-sets.html', label: 'O que e Configuration Set' },
  },
  postmark: {
    server_token: { url: 'https://account.postmarkapp.com/servers', label: 'Onde encontrar o Server Token' },
  },
  'microsoft-graph': {
    tenant_id: { url: 'https://learn.microsoft.com/en-us/azure/active-directory/develop/howto-create-service-principal-portal', label: 'Onde encontrar Tenant ID' },
    client_id: { url: 'https://learn.microsoft.com/en-us/azure/active-directory/develop/howto-create-service-principal-portal', label: 'Onde encontrar Client ID' },
    client_secret: { url: 'https://learn.microsoft.com/en-us/azure/active-directory/develop/howto-create-service-principal-portal#option-3-create-a-new-client-secret', label: 'Como gerar Client Secret' },
  },

  // ============ WHATSAPP ============
  'whatsapp-cloud': {
    access_token: { url: 'https://developers.facebook.com/docs/whatsapp/business-management-api/get-started', label: 'Como gerar System User Token' },
    phone_number_id: { url: 'https://developers.facebook.com/docs/whatsapp/cloud-api/get-started', label: 'Onde achar o Phone Number ID' },
    business_account_id: { url: 'https://developers.facebook.com/docs/whatsapp/cloud-api/get-started', label: 'Onde achar o WABA ID' },
    app_secret: { url: 'https://developers.facebook.com/docs/development/create-an-app/', label: 'Onde encontrar o App Secret' },
  },
  'twilio-whatsapp': {
    account_sid: { url: 'https://www.twilio.com/console', label: 'Onde encontrar Account SID + Auth Token' },
    auth_token: { url: 'https://www.twilio.com/console', label: 'Onde encontrar Auth Token' },
    from_number: { url: 'https://www.twilio.com/docs/whatsapp/self-sign-up', label: 'Como ativar um numero WhatsApp' },
  },
  '360dialog': {
    api_key: { url: 'https://docs.360dialog.com/whatsapp-api/whatsapp-api/authentication', label: 'Como obter D360-API-KEY' },
  },
  'z-api': {
    instance_id: { url: 'https://developer.z-api.io/instancia/sobre', label: 'Onde achar o Instance ID' },
    instance_token: { url: 'https://developer.z-api.io/instancia/sobre', label: 'Onde achar o Instance Token' },
    client_token: { url: 'https://developer.z-api.io/instancia/seguranca-da-conta', label: 'Como gerar Client-Token' },
  },
  'evolution-api': {
    api_key: { url: 'https://doc.evolution-api.com/v2/pt/configuration/available-resources', label: 'Configurando API Keys' },
  },
}

export function getDocLink(integrationIdentifier: string | undefined, field: string): DocLink | null {
  if (!integrationIdentifier) return null
  return integrationDocs[integrationIdentifier]?.[field] ?? null
}

// Presets pra SMTP. Click no chip preenche os campos correspondentes.
export interface SmtpPreset {
  id: string
  label: string
  description: string
  values: Record<string, string>
}

export const smtpPresets: SmtpPreset[] = [
  {
    id: 'gmail',
    label: 'Gmail',
    description: 'Use App Password (nao a senha da conta).',
    values: {
      host: 'smtp.gmail.com',
      port: '587',
      enable_ssl: 'true',
    },
  },
  {
    id: 'outlook365',
    label: 'Outlook / Microsoft 365',
    description: 'Conta Microsoft 365 com SMTP AUTH habilitado.',
    values: {
      host: 'smtp.office365.com',
      port: '587',
      enable_ssl: 'true',
    },
  },
  {
    id: 'aws-ses-smtp',
    label: 'AWS SES (SMTP)',
    description: 'Use credenciais SMTP do SES (nao Access Key da AWS).',
    values: {
      host: 'email-smtp.us-east-1.amazonaws.com',
      port: '587',
      enable_ssl: 'true',
    },
  },
  {
    id: 'mailgun-smtp',
    label: 'Mailgun (SMTP)',
    description: 'Credenciais SMTP do dominio Mailgun.',
    values: {
      host: 'smtp.mailgun.org',
      port: '587',
      enable_ssl: 'true',
    },
  },
]
