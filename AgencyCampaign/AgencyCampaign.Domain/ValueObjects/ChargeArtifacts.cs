namespace AgencyCampaign.Domain.ValueObjects
{
    // Artefatos bancarios de uma cobranca, devolvidos pelo provedor via conector do IntegrationPlatform.
    // Boleto: DigitableLine (linha digitavel), BarCode (codigo de barras), NossoNumero, BankSlipUrl (PDF).
    // PIX: PixCopyPaste (payload EMV "copia e cola"), PixQrCodeUrl (imagem/location do QR), TxId.
    // Todos opcionais: o conector preenche os que se aplicam ao metodo (boleto e/ou PIX).
    public sealed record ChargeArtifacts(
        string? DigitableLine,
        string? BarCode,
        string? NossoNumero,
        string? PixCopyPaste,
        string? PixQrCodeUrl,
        string? TxId,
        string? BankSlipUrl)
    {
        public bool HasAny =>
            !string.IsNullOrWhiteSpace(DigitableLine)
            || !string.IsNullOrWhiteSpace(BarCode)
            || !string.IsNullOrWhiteSpace(NossoNumero)
            || !string.IsNullOrWhiteSpace(PixCopyPaste)
            || !string.IsNullOrWhiteSpace(PixQrCodeUrl)
            || !string.IsNullOrWhiteSpace(TxId)
            || !string.IsNullOrWhiteSpace(BankSlipUrl);
    }
}
