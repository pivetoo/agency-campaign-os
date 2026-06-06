using AgencyCampaign.Application.Models.Commercial;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace AgencyCampaign.Infrastructure.Services
{
    public sealed class CommercialReportService : ICommercialReportService
    {
        private readonly DbContext dbContext;

        public CommercialReportService(DbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        // Funil de propostas: conta emissões (Sent), aceitações (Approved) e rejeições (Rejected)
        // dentro da janela pelo ChangedAt do ProposalStatusHistory. A taxa de aceite compara
        // aprovadas-no-período vs emitidas-no-período (coorte aproximada — uma proposta emitida em
        // dezembro e aprovada em janeiro só aparece nos aprovados de janeiro, não nos emitidos).
        public async Task<ProposalsFunnelModel> GetProposalsFunnel(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
        {
            DateTimeOffset normalizedFrom = from.ToUniversalTime();
            DateTimeOffset normalizedTo = to.ToUniversalTime();

            List<ProposalStatusHistory> historyInWindow = await dbContext.Set<ProposalStatusHistory>()
                .AsNoTracking()
                .Where(item => item.ChangedAt >= normalizedFrom && item.ChangedAt < normalizedTo)
                .ToListAsync(cancellationToken);

            long[] emittedIds = historyInWindow
                .Where(item => item.ToStatus == ProposalStatus.Sent)
                .Select(item => item.ProposalId)
                .Distinct()
                .ToArray();

            long[] acceptedIds = historyInWindow
                .Where(item => item.ToStatus == ProposalStatus.Approved)
                .Select(item => item.ProposalId)
                .Distinct()
                .ToArray();

            long[] rejectedIds = historyInWindow
                .Where(item => item.ToStatus == ProposalStatus.Rejected)
                .Select(item => item.ProposalId)
                .Distinct()
                .ToArray();

            decimal emittedValue = 0m;
            decimal acceptedValue = 0m;

            if (emittedIds.Length > 0)
            {
                List<Proposal> emittedProposals = await dbContext.Set<Proposal>()
                    .AsNoTracking()
                    .Where(item => emittedIds.Contains(item.Id))
                    .ToListAsync(cancellationToken);

                emittedValue = emittedProposals.Sum(item => item.NetTotalValue);
            }

            if (acceptedIds.Length > 0)
            {
                List<Proposal> acceptedProposals = await dbContext.Set<Proposal>()
                    .AsNoTracking()
                    .Where(item => acceptedIds.Contains(item.Id))
                    .ToListAsync(cancellationToken);

                acceptedValue = acceptedProposals.Sum(item => item.NetTotalValue);
            }

            int emittedCount = emittedIds.Length;
            int acceptedCount = acceptedIds.Length;
            int rejectedCount = rejectedIds.Length;
            decimal acceptanceRate = emittedCount > 0
                ? Math.Round((decimal)acceptedCount / emittedCount * 100m, 2)
                : 0m;

            return new ProposalsFunnelModel
            {
                From = normalizedFrom,
                To = normalizedTo,
                EmittedCount = emittedCount,
                EmittedValue = emittedValue,
                AcceptedCount = acceptedCount,
                AcceptedValue = acceptedValue,
                RejectedCount = rejectedCount,
                AcceptanceRate = acceptanceRate
            };
        }

        public async Task<BrandRankingModel> GetBrandRanking(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
        {
            DateTimeOffset normalizedFrom = from.ToUniversalTime();
            DateTimeOffset normalizedTo = to.ToUniversalTime();

            List<Opportunity> opportunities = await dbContext.Set<Opportunity>()
                .AsNoTracking()
                .Include(item => item.CommercialPipelineStage)
                .Include(item => item.Brand)
                .Where(item => item.ClosedAt.HasValue && item.ClosedAt.Value >= normalizedFrom && item.ClosedAt.Value < normalizedTo)
                .ToListAsync(cancellationToken);

            BrandRankingLineModel[] lines = opportunities
                .GroupBy(item => item.BrandId)
                .Select(group =>
                {
                    int wonCount = group.Count(item => item.CommercialPipelineStage?.FinalBehavior == CommercialPipelineStageFinalBehavior.Won);
                    int lostCount = group.Count(item => item.CommercialPipelineStage?.FinalBehavior == CommercialPipelineStageFinalBehavior.Lost);
                    decimal wonValue = Math.Round(
                        group
                            .Where(item => item.CommercialPipelineStage?.FinalBehavior == CommercialPipelineStageFinalBehavior.Won)
                            .Sum(item => item.ClosedValue ?? item.EstimatedValue),
                        2);
                    int total = wonCount + lostCount;
                    decimal winRate = total > 0 ? Math.Round((decimal)wonCount / total * 100m, 2) : 0m;

                    return new BrandRankingLineModel
                    {
                        BrandId = group.Key,
                        BrandName = group.First().Brand?.Name ?? string.Empty,
                        WonCount = wonCount,
                        LostCount = lostCount,
                        WonValue = wonValue,
                        WinRate = winRate
                    };
                })
                .OrderByDescending(item => item.WonValue)
                .Take(20)
                .ToArray();

            return new BrandRankingModel
            {
                GeneratedAt = DateTimeOffset.UtcNow,
                From = normalizedFrom,
                To = normalizedTo,
                Lines = lines
            };
        }
    }
}
