using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class FinancialAutoGenerationService : IFinancialAutoGeneration
    {
        private readonly DbContext dbContext;
        private readonly ILogger<FinancialAutoGenerationService>? logger;

        public FinancialAutoGenerationService(DbContext dbContext, ILogger<FinancialAutoGenerationService>? logger = null)
        {
            this.dbContext = dbContext;
            this.logger = logger;
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
                logger?.LogWarning("No active financial account configured; skipping receivable for proposal {ProposalId}.", proposal.Id);
                return;
            }

            string brandName = await ResolveBrandNameAsync(campaignId, cancellationToken);
            DateTimeOffset dueAt = proposal.ValidityUntil ?? DateTimeOffset.UtcNow.AddDays(30);

            FinancialEntry entry = new(
                accountId.Value,
                FinancialEntryType.Receivable,
                FinancialEntryCategory.BrandReceivable,
                $"Recebível da proposta {proposal.Name}",
                proposal.NetTotalValue,
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

        // Gera os repasses PLANEJADOS de creator no fechamento: cada item da proposta com creator vira
        // um Payable (CreatorPayout) com vencimento previsto, dando visibilidade de margem desde o dia 1.
        // A execucao por entrega (GenerateForPublishedDeliverable) e suprimida quando ja existe planejado
        // na campanha, para nao duplicar o custo do mesmo creator.
        public async Task GenerateCreatorPayoutsForConvertedProposal(Proposal proposal, long campaignId, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(proposal);

            List<ProposalItem> items = await dbContext.Set<ProposalItem>()
                .AsNoTracking()
                .Include(item => item.Creator)
                .Where(item => item.ProposalId == proposal.Id && item.CreatorId != null)
                .ToListAsync(cancellationToken);

            if (items.Count == 0)
            {
                return;
            }

            long? accountId = await ResolveDefaultAccountIdAsync(cancellationToken);
            if (!accountId.HasValue)
            {
                logger?.LogWarning("No active financial account configured; skipping creator payouts for proposal {ProposalId}.", proposal.Id);
                return;
            }

            HashSet<long> alreadyLinked = (await dbContext.Set<FinancialEntry>()
                .AsNoTracking()
                .Where(item => item.SourceProposalItemId != null && item.SourceProposalId == proposal.Id)
                .Select(item => item.SourceProposalItemId!.Value)
                .ToListAsync(cancellationToken))
                .ToHashSet();

            DateTimeOffset now = DateTimeOffset.UtcNow;
            bool added = false;

            foreach (ProposalItem item in items)
            {
                if (alreadyLinked.Contains(item.Id) || item.Total <= 0)
                {
                    continue;
                }

                string? creatorName = item.Creator?.StageName ?? item.Creator?.Name;
                DateTimeOffset dueAt = item.DeliveryDeadline ?? now.AddDays(30);

                FinancialEntry entry = new(
                    accountId.Value,
                    FinancialEntryType.Payable,
                    FinancialEntryCategory.CreatorPayout,
                    $"Repasse creator (previsto): {item.Description}",
                    item.Total,
                    dueAt,
                    now,
                    paymentMethod: null,
                    referenceCode: null,
                    counterpartyName: creatorName,
                    notes: $"Previsto a partir da conversão da proposta #{proposal.Id}.",
                    campaignId: campaignId);

                entry.LinkToProposalItem(proposal.Id, item.Id);
                entry.LinkToCreator(item.CreatorId!.Value);

                dbContext.Set<FinancialEntry>().Add(entry);
                added = true;
            }

            if (added)
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
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

            var creatorInfo = await dbContext.Set<CampaignCreator>()
                .AsNoTracking()
                .Where(item => item.Id == deliverable.CampaignCreatorId)
                .Select(item => new { item.CreatorId, Name = item.Creator!.StageName ?? item.Creator!.Name })
                .FirstOrDefaultAsync(cancellationToken);

            // Nao duplicar: se ESTE creator ja tem repasse PLANEJADO (da proposta) na campanha, a execucao
            // por entrega e suprimida. Creators adicionados depois da proposta (sem planejado) geram repasse
            // normalmente - a supressao e por creator, nao pela campanha inteira.
            if (creatorInfo is not null)
            {
                bool hasPlannedForCreator = await dbContext.Set<FinancialEntry>()
                    .AsNoTracking()
                    .AnyAsync(item =>
                        item.CampaignId == deliverable.CampaignId &&
                        item.CreatorId == creatorInfo.CreatorId &&
                        item.Category == FinancialEntryCategory.CreatorPayout &&
                        item.SourceProposalItemId != null,
                        cancellationToken);

                if (hasPlannedForCreator)
                {
                    return;
                }
            }

            long? accountId = await ResolveDefaultAccountIdAsync(cancellationToken);
            if (!accountId.HasValue)
            {
                logger?.LogWarning("No active financial account configured; skipping payable for deliverable {DeliverableId}.", deliverable.Id);
                return;
            }

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
                counterpartyName: creatorInfo?.Name,
                notes: $"Gerado automaticamente após publicação da entrega #{deliverable.Id}.",
                campaignId: deliverable.CampaignId,
                campaignDeliverableId: deliverable.Id);

            if (creatorInfo is not null)
            {
                entry.LinkToCreator(creatorInfo.CreatorId);
            }

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
