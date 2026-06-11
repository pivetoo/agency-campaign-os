using AgencyCampaign.Application.Models.Commercial;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using AgencyCampaign.Infrastructure.Services;
using AgencyCampaign.Testing.Builders;
using AgencyCampaign.Testing.TestSupport;

namespace AgencyCampaign.Testing.Infrastructure.Services
{
    [TestFixture]
    public sealed class CommercialReportServiceTests
    {
        private TestDbContext db = null!;
        private CommercialReportService service = null!;

        [SetUp]
        public void SetUp()
        {
            db = TestDbContext.CreateInMemory();
            service = new CommercialReportService(db);
        }

        [TearDown]
        public void TearDown() => db.Dispose();

        // --- ProposalsFunnel ---

        [Test]
        public async Task GetProposalsFunnel_should_count_emitted_distinct_proposals_in_window()
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            DateTimeOffset from = now.AddDays(-1);
            DateTimeOffset to = now.AddDays(1);

            Brand brand = new("Acme");
            db.Add(brand);
            await db.SaveChangesAsync();

            CommercialPipelineStage stage = CommercialPipelineStageBuilder.Default();
            db.Add(stage);
            await db.SaveChangesAsync();

            Opportunity opp = new(brand.Id, stage.Id, "Opp A", 500m);
            db.Add(opp);
            await db.SaveChangesAsync();

            Proposal proposal = new ProposalBuilder()
                .WithOpportunityId(opp.Id)
                .WithTotalValue(2000m)
                .Build();
            db.Add(proposal);
            await db.SaveChangesAsync();

            ProposalStatusHistory sentHistory = new(
                proposal.Id,
                ProposalStatus.Draft,
                ProposalStatus.Sent,
                null, null, "Enviada");
            db.Add(sentHistory);
            await db.SaveChangesAsync();

            ProposalsFunnelModel result = await service.GetProposalsFunnel(from, to);

            result.EmittedCount.Should().Be(1);
            result.EmittedValue.Should().Be(2000m);
        }

        [Test]
        public async Task GetProposalsFunnel_should_exclude_sent_outside_window()
        {
            DateTimeOffset baseDate = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);
            DateTimeOffset from = baseDate;
            DateTimeOffset to = baseDate.AddMonths(1);

            Brand brand = new("Acme");
            db.Add(brand);
            await db.SaveChangesAsync();

            CommercialPipelineStage stage = CommercialPipelineStageBuilder.Default();
            db.Add(stage);
            await db.SaveChangesAsync();

            Opportunity opp = new(brand.Id, stage.Id, "Opp B", 1000m);
            db.Add(opp);
            await db.SaveChangesAsync();

            Proposal proposal = new ProposalBuilder()
                .WithOpportunityId(opp.Id)
                .WithTotalValue(3000m)
                .Build();
            db.Add(proposal);
            await db.SaveChangesAsync();

            // Esta entrada fica FORA da janela (dois meses antes)
            ProposalStatusHistory outsideWindow = new(
                proposal.Id,
                ProposalStatus.Draft,
                ProposalStatus.Sent,
                null, null, "Enviada antes do período");
            // Forçar ChangedAt fora da janela via reflection para simular dado histórico
            typeof(ProposalStatusHistory)
                .GetProperty(nameof(ProposalStatusHistory.ChangedAt))!
                .SetValue(outsideWindow, baseDate.AddMonths(-2));
            db.Add(outsideWindow);
            await db.SaveChangesAsync();

            ProposalsFunnelModel result = await service.GetProposalsFunnel(from, to);

            result.EmittedCount.Should().Be(0);
            result.EmittedValue.Should().Be(0m);
        }

        [Test]
        public async Task GetProposalsFunnel_should_count_accepted_and_compute_acceptance_rate()
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            DateTimeOffset from = now.AddDays(-1);
            DateTimeOffset to = now.AddDays(1);

            Brand brand = new("Acme");
            db.Add(brand);
            await db.SaveChangesAsync();

            CommercialPipelineStage stage = CommercialPipelineStageBuilder.Default();
            db.Add(stage);
            await db.SaveChangesAsync();

            Opportunity opp = new(brand.Id, stage.Id, "Opp C", 500m);
            db.Add(opp);
            await db.SaveChangesAsync();

            Proposal proposal = new ProposalBuilder()
                .WithOpportunityId(opp.Id)
                .WithTotalValue(1500m)
                .Build();
            db.Add(proposal);
            await db.SaveChangesAsync();

            db.Add(new ProposalStatusHistory(proposal.Id, ProposalStatus.Draft, ProposalStatus.Sent, null, null, "Enviada"));
            db.Add(new ProposalStatusHistory(proposal.Id, ProposalStatus.Sent, ProposalStatus.Approved, null, null, "Aprovada"));
            await db.SaveChangesAsync();

            ProposalsFunnelModel result = await service.GetProposalsFunnel(from, to);

            result.EmittedCount.Should().Be(1);
            result.AcceptedCount.Should().Be(1);
            result.AcceptedValue.Should().Be(1500m);
            result.AcceptanceRate.Should().Be(100m);
        }

        [Test]
        public async Task GetProposalsFunnel_should_count_rejected_proposals()
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            DateTimeOffset from = now.AddDays(-1);
            DateTimeOffset to = now.AddDays(1);

            Brand brand = new("Acme");
            db.Add(brand);
            await db.SaveChangesAsync();

            CommercialPipelineStage stage = CommercialPipelineStageBuilder.Default();
            db.Add(stage);
            await db.SaveChangesAsync();

            Opportunity opp = new(brand.Id, stage.Id, "Opp D", 500m);
            db.Add(opp);
            await db.SaveChangesAsync();

            Proposal proposal = new ProposalBuilder()
                .WithOpportunityId(opp.Id)
                .WithTotalValue(800m)
                .Build();
            db.Add(proposal);
            await db.SaveChangesAsync();

            db.Add(new ProposalStatusHistory(proposal.Id, ProposalStatus.Draft, ProposalStatus.Sent, null, null, "Enviada"));
            db.Add(new ProposalStatusHistory(proposal.Id, ProposalStatus.Sent, ProposalStatus.Rejected, null, null, "Rejeitada"));
            await db.SaveChangesAsync();

            ProposalsFunnelModel result = await service.GetProposalsFunnel(from, to);

            result.RejectedCount.Should().Be(1);
            result.EmittedCount.Should().Be(1);
            result.AcceptedCount.Should().Be(0);
            result.AcceptanceRate.Should().Be(0m);
        }

        [Test]
        public async Task GetProposalsFunnel_should_return_zero_acceptance_rate_when_no_emitted()
        {
            DateTimeOffset baseDate = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);

            ProposalsFunnelModel result = await service.GetProposalsFunnel(baseDate, baseDate.AddMonths(1));

            result.EmittedCount.Should().Be(0);
            result.AcceptanceRate.Should().Be(0m);
        }

        // --- BrandRanking ---

        [Test]
        public async Task GetBrandRanking_should_order_brands_by_won_value_descending()
        {
            DateTimeOffset baseDate = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);
            DateTimeOffset from = baseDate;
            DateTimeOffset to = baseDate.AddMonths(1);

            Brand brandA = new("Alpha");
            Brand brandB = new("Beta");
            db.AddRange(brandA, brandB);
            await db.SaveChangesAsync();

            CommercialPipelineStage wonStage = new CommercialPipelineStageBuilder()
                .AsFinal(CommercialPipelineStageFinalBehavior.Won)
                .Build();
            db.Add(wonStage);
            await db.SaveChangesAsync();

            // Brand A: 1 won com 5000
            Opportunity oppA = new(brandA.Id, wonStage.Id, "A-1", 5000m);
            oppA.SetClosedValue(5000m);
            SetClosedAt(oppA, baseDate.AddDays(10));
            db.Add(oppA);

            // Brand B: 1 won com 3000
            Opportunity oppB = new(brandB.Id, wonStage.Id, "B-1", 3000m);
            oppB.SetClosedValue(3000m);
            SetClosedAt(oppB, baseDate.AddDays(5));
            db.Add(oppB);

            await db.SaveChangesAsync();

            BrandRankingModel result = await service.GetBrandRanking(from, to);

            result.Lines.Should().HaveCount(2);
            result.Lines.First().BrandName.Should().Be("Alpha");
            result.Lines.First().WonValue.Should().Be(5000m);
            result.Lines.Last().BrandName.Should().Be("Beta");
        }

        [Test]
        public async Task GetCreatorRevenue_should_aggregate_sold_value_per_creator_from_won_deals()
        {
            DateTimeOffset baseDate = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);
            DateTimeOffset from = baseDate;
            DateTimeOffset to = baseDate.AddMonths(1);

            Brand brand = new("Acme");
            db.Add(brand);
            CommercialPipelineStage wonStage = new CommercialPipelineStageBuilder().AsFinal(CommercialPipelineStageFinalBehavior.Won).Build();
            db.Add(wonStage);
            Creator jo = new("Jo");
            Creator ana = new("Ana");
            db.AddRange(jo, ana);
            await db.SaveChangesAsync();

            Opportunity opp = new(brand.Id, wonStage.Id, "Deal", 0m);
            opp.SetClosedValue(8000m);
            SetClosedAt(opp, baseDate.AddDays(10));
            db.Add(opp);
            await db.SaveChangesAsync();

            Proposal proposal = new(opp.Id, "P", 1);
            proposal.MarkAsSent();
            proposal.Approve();
            db.Add(proposal);
            await db.SaveChangesAsync();
            db.Add(new ProposalItem(proposal.Id, "Reels", 1, 5000m, creatorId: jo.Id));
            db.Add(new ProposalItem(proposal.Id, "Story", 2, 1000m, creatorId: jo.Id));
            db.Add(new ProposalItem(proposal.Id, "Post", 1, 1000m, creatorId: ana.Id));
            await db.SaveChangesAsync();

            CreatorRevenueModel result = await service.GetCreatorRevenue(from, to);

            result.Lines.Should().HaveCount(2);
            result.Lines.First().CreatorName.Should().Be("Jo");
            result.Lines.First().TotalValue.Should().Be(7000m);
            result.Lines.First().ItemCount.Should().Be(2);
            result.Lines.First().DealCount.Should().Be(1);
            result.Lines.Last().CreatorName.Should().Be("Ana");
        }

        [Test]
        public async Task GetBrandRanking_should_compute_win_rate_correctly()
        {
            DateTimeOffset baseDate = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);
            DateTimeOffset from = baseDate;
            DateTimeOffset to = baseDate.AddMonths(1);

            Brand brand = new("Gamma");
            db.Add(brand);
            await db.SaveChangesAsync();

            CommercialPipelineStage wonStage = new CommercialPipelineStageBuilder()
                .WithId(10)
                .AsFinal(CommercialPipelineStageFinalBehavior.Won)
                .Build();
            CommercialPipelineStage lostStage = new CommercialPipelineStageBuilder()
                .WithId(11)
                .AsFinal(CommercialPipelineStageFinalBehavior.Lost)
                .Build();
            db.AddRange(wonStage, lostStage);
            await db.SaveChangesAsync();

            // 2 won, 1 lost => WinRate = round(2/3*100, 2) = 66.67
            Opportunity oppWon1 = new(brand.Id, wonStage.Id, "G-1", 1000m);
            oppWon1.SetClosedValue(1000m);
            SetClosedAt(oppWon1, baseDate.AddDays(2));
            db.Add(oppWon1);

            Opportunity oppWon2 = new(brand.Id, wonStage.Id, "G-2", 2000m);
            oppWon2.SetClosedValue(2000m);
            SetClosedAt(oppWon2, baseDate.AddDays(3));
            db.Add(oppWon2);

            Opportunity oppLost = new(brand.Id, lostStage.Id, "G-3", 500m);
            SetClosedAt(oppLost, baseDate.AddDays(4));
            db.Add(oppLost);

            await db.SaveChangesAsync();

            BrandRankingModel result = await service.GetBrandRanking(from, to);

            result.Lines.Should().HaveCount(1);
            BrandRankingLineModel line = result.Lines.First();
            line.WonCount.Should().Be(2);
            line.LostCount.Should().Be(1);
            line.WonValue.Should().Be(3000m);
            line.WinRate.Should().Be(66.67m);
        }

        [Test]
        public async Task GetBrandRanking_should_exclude_opportunities_outside_window()
        {
            DateTimeOffset baseDate = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);
            DateTimeOffset from = baseDate;
            DateTimeOffset to = baseDate.AddMonths(1);

            Brand brand = new("Delta");
            db.Add(brand);
            await db.SaveChangesAsync();

            CommercialPipelineStage wonStage = new CommercialPipelineStageBuilder()
                .AsFinal(CommercialPipelineStageFinalBehavior.Won)
                .Build();
            db.Add(wonStage);
            await db.SaveChangesAsync();

            Opportunity oppOutside = new(brand.Id, wonStage.Id, "D-Outside", 9999m);
            oppOutside.SetClosedValue(9999m);
            SetClosedAt(oppOutside, baseDate.AddMonths(-2));
            db.Add(oppOutside);
            await db.SaveChangesAsync();

            BrandRankingModel result = await service.GetBrandRanking(from, to);

            result.Lines.Should().BeEmpty();
        }

        [Test]
        public async Task GetBrandRanking_should_use_estimated_value_when_closed_value_is_null()
        {
            DateTimeOffset baseDate = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);
            DateTimeOffset from = baseDate;
            DateTimeOffset to = baseDate.AddMonths(1);

            Brand brand = new("Epsilon");
            db.Add(brand);
            await db.SaveChangesAsync();

            CommercialPipelineStage wonStage = new CommercialPipelineStageBuilder()
                .AsFinal(CommercialPipelineStageFinalBehavior.Won)
                .Build();
            db.Add(wonStage);
            await db.SaveChangesAsync();

            // ClosedValue não definido; deve cair para EstimatedValue = 750
            Opportunity opp = new(brand.Id, wonStage.Id, "E-1", 750m);
            SetClosedAt(opp, baseDate.AddDays(5));
            db.Add(opp);
            await db.SaveChangesAsync();

            BrandRankingModel result = await service.GetBrandRanking(from, to);

            result.Lines.Should().HaveCount(1);
            result.Lines.First().WonValue.Should().Be(750m);
        }

        // Helper para forçar ClosedAt via reflection (a entidade não expõe setter público)
        private static void SetClosedAt(Opportunity opportunity, DateTimeOffset value)
        {
            typeof(Opportunity)
                .GetProperty(nameof(Opportunity.ClosedAt))!
                .SetValue(opportunity, value);
        }
    }
}
