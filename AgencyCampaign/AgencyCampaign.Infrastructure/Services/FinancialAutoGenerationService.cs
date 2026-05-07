using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class FinancialAutoGenerationService : IFinancialAutoGeneration
    {
        private readonly DbContext dbContext;

        public FinancialAutoGenerationService(DbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task GenerateForConvertedProposal(Proposal proposal, long campaignId, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(proposal);

            bool alreadyExists = await dbContext.Set<FinancialEntry>()
                .AsNoTracking()
                .AnyAsync(item => item.SourceProposalId == proposal.Id, cancellationToken);

            if (alreadyExists)
            {
                return;
            }

            long? accountId = await ResolveDefaultAccountIdAsync(cancellationToken);
            if (!accountId.HasValue)
            {
                Console.WriteLine($"[FinancialAutoGeneration] No active account configured; skipping receivable for proposal {proposal.Id}.");
                return;
            }

            string brandName = await ResolveBrandNameAsync(campaignId, cancellationToken);
            DateTimeOffset dueAt = proposal.ValidityUntil ?? DateTimeOffset.UtcNow.AddDays(30);

            FinancialEntry entry = new(
                accountId.Value,
                FinancialEntryType.Receivable,
                FinancialEntryCategory.BrandReceivable,
                $"Recebível da proposta {proposal.Name}",
                proposal.TotalValue,
                dueAt,
                DateTimeOffset.UtcNow,
                paymentMethod: null,
                referenceCode: null,
                counterpartyName: brandName,
                notes: $"Gerado automaticamente a partir da conversão da proposta #{proposal.Id}.",
                campaignId: campaignId);

            entry.LinkToProposal(proposal.Id);

            dbContext.Set<FinancialEntry>().Add(entry);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task GenerateForPublishedDeliverable(CampaignDeliverable deliverable, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(deliverable);

            if (deliverable.CreatorAmount <= 0)
            {
                return;
            }

            bool alreadyExists = await dbContext.Set<FinancialEntry>()
                .AsNoTracking()
                .AnyAsync(item =>
                    item.CampaignDeliverableId == deliverable.Id &&
                    item.Type == FinancialEntryType.Payable &&
                    item.Category == FinancialEntryCategory.CreatorPayout,
                    cancellationToken);

            if (alreadyExists)
            {
                return;
            }

            long? accountId = await ResolveDefaultAccountIdAsync(cancellationToken);
            if (!accountId.HasValue)
            {
                Console.WriteLine($"[FinancialAutoGeneration] No active account configured; skipping payable for deliverable {deliverable.Id}.");
                return;
            }

            string? creatorName = await dbContext.Set<CampaignCreator>()
                .AsNoTracking()
                .Where(item => item.Id == deliverable.CampaignCreatorId)
                .Select(item => item.Creator!.StageName ?? item.Creator!.Name)
                .FirstOrDefaultAsync(cancellationToken);

            DateTimeOffset dueAt = (deliverable.PublishedAt ?? DateTimeOffset.UtcNow).AddDays(15);

            FinancialEntry entry = new(
                accountId.Value,
                FinancialEntryType.Payable,
                FinancialEntryCategory.CreatorPayout,
                $"Repasse creator: {deliverable.Title}",
                deliverable.CreatorAmount,
                dueAt,
                DateTimeOffset.UtcNow,
                paymentMethod: null,
                referenceCode: null,
                counterpartyName: creatorName,
                notes: $"Gerado automaticamente após publicação da entrega #{deliverable.Id}.",
                campaignId: deliverable.CampaignId,
                campaignDeliverableId: deliverable.Id);

            dbContext.Set<FinancialEntry>().Add(entry);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task<long?> ResolveDefaultAccountIdAsync(CancellationToken cancellationToken)
        {
            return await dbContext.Set<FinancialAccount>()
                .AsNoTracking()
                .Where(item => item.IsActive)
                .OrderBy(item => item.Id)
                .Select(item => (long?)item.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        private async Task<string> ResolveBrandNameAsync(long campaignId, CancellationToken cancellationToken)
        {
            string? brandName = await dbContext.Set<Campaign>()
                .AsNoTracking()
                .Where(item => item.Id == campaignId)
                .Select(item => item.Brand!.Name)
                .FirstOrDefaultAsync(cancellationToken);

            return brandName ?? "Marca";
        }
    }
}
