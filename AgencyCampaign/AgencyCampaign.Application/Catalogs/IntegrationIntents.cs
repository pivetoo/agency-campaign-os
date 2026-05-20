namespace AgencyCampaign.Application.Catalogs
{
    public static class IntegrationIntents
    {
        public const string ProposalSendEmail = "proposal.send-email";

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
            new(ProposalSendEmail, "Enviar proposta por email", "email"),
            new(CampaignDocumentSendSignature, "Enviar documento para assinatura", "digital-signature"),
            new(CampaignDocumentSendEmail, "Enviar documento por email", "email"),
            new(CreatorPaymentSchedulePix, "Agendar pagamento PIX para creator", "payment"),
            new(NotificationSendTransactional, "Notificação transacional da plataforma", "email"),
            new(ReceivableIssueInvoice, "Emitir cobrança para cliente", "payment"),
            new(PayableTransfer, "Pagar fornecedor", "payment"),
            new(FinancialEntryIssueNf, "Emitir nota fiscal", "invoice"),
            new(BankAccountSync, "Sincronizar conta bancária", "banking"),
            new(CreatorPortalNotifyWhatsapp, "Notificar creator por WhatsApp", "whatsapp"),
        ];
    }

    public sealed record IntegrationIntentDescriptor(string Key, string Label, string CategoryIdentifier);
}
