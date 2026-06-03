namespace AgencyCampaign.Domain.ValueObjects
{
    // Regime tributario do creator (Brasil): define o que a agencia retem ao pagar e qual documento
    // fiscal e exigido. O calculo automatico das retencoes e a emissao de RPA/NFS-e ficam para a fase 2;
    // por ora o campo serve para registrar e orientar a retencao manual e o relatorio para o contador.
    public enum TaxRegime
    {
        IndividualPF = 1,
        Mei = 2,
        SimplesNacional = 3,
        PresumedRealProfit = 4
    }
}
