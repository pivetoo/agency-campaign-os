namespace AgencyCampaign.Application.Services
{
    public interface IProposalPdfService
    {
        Task<byte[]> GenerateForProposalAsync(long proposalId, CancellationToken cancellationToken = default);

        Task<byte[]?> GenerateForShareTokenAsync(string token, CancellationToken cancellationToken = default);
    }
}
