namespace AgencyCampaign.Application.Catalogs
{
    public static class IntegrationIntents
    {
        public const string ProposalSendEmail = "proposal.send-email";

        public const string ProposalSendWhatsapp = "proposal.send-whatsapp";

        public const string CampaignDocumentSendSignature = "campaign-document.send-signature";

        public const string CampaignDocumentSendEmail = "campaign-document.send-email";

        public const string CreatorPaymentSchedulePix = "creator-payment.schedule-pix";

        public const string NotificationSendTransactional = "notification.send-transactional";

        public const string ReceivableIssueInvoice = "receivable.issue-invoice";

        public const string PayableTransfer = "payable.transfer";

        public const string FinancialEntryIssueNf = "financial-entry.issue-nf";

        public const string BankAccountSync = "bank-account.sync";

        public const string CreatorPortalNotifyWhatsapp = "creator-portal.notify-whatsapp";

        public static readonly IReadOnlyList<IntegrationIntentDescriptor> All =
        [
            new(ProposalSendEmail, "Enviar proposta por email", "email", "email.send"),
            new(ProposalSendWhatsapp, "Enviar proposta por WhatsApp", "messaging", "messaging.send"),
            new(CampaignDocumentSendSignature, "Enviar documento para assinatura", "digital-signature", "signature.envelope.create"),
            new(CampaignDocumentSendEmail, "Enviar documento por email", "email", "email.send"),
            new(CreatorPaymentSchedulePix, "Agendar pagamento PIX para creator", "payment", "payment.charge.create"),
            new(NotificationSendTransactional, "Notificação transacional da plataforma", "email", "email.send"),
            new(ReceivableIssueInvoice, "Emitir cobrança para cliente", "payment", "payment.charge.create"),
            new(PayableTransfer, "Pagar fornecedor", "payment", "payment.charge.create"),
            new(FinancialEntryIssueNf, "Emitir nota fiscal", "invoice", "invoice.issue"),
            new(BankAccountSync, "Sincronizar conta bancária", "banking", "banking.account.sync"),
            new(CreatorPortalNotifyWhatsapp, "Notificar creator por WhatsApp", "messaging", "messaging.send"),
        ];

        public static IntegrationIntentDescriptor? Find(string intentKey)
        {
            if (string.IsNullOrWhiteSpace(intentKey))
            {
                return null;
            }

            string normalized = intentKey.Trim();
            return All.FirstOrDefault(item => string.Equals(item.Key, normalized, StringComparison.OrdinalIgnoreCase));
        }
    }

    public sealed record IntegrationIntentDescriptor(string Key, string Label, string CategoryIdentifier, string ServiceContractIdentifier);
}
