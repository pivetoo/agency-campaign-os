using System.Text;
using AgencyCampaign.Application.Models.Reports;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using AgencyCampaign.Infrastructure.Services;
using AgencyCampaign.Testing.TestSupport;
using Moq;

namespace AgencyCampaign.Testing.Infrastructure.Services
{
    [TestFixture]
    public sealed class FinancialReportExportServiceTests
    {
        private TestDbContext db = null!;
        private FinancialReportExportService service = null!;

        [SetUp]
        public void SetUp()
        {
            db = TestDbContext.CreateInMemory();
            Mock<IReportPdfService> pdfMock = new();
            pdfMock.Setup(s => s.GenerateAsync(It.IsAny<ReportTable>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync([]);
            service = new FinancialReportExportService(new FinancialReportService(db), pdfMock.Object);
        }

        [TearDown]
        public void TearDown() => db.Dispose();

        [Test]
        public async Task ExportAging_should_emit_bom_and_header_even_with_no_data()
        {
            byte[] bytes = await service.ExportAging();

            bytes.Take(3).Should().Equal(0xEF, 0xBB, 0xBF);
            string csv = Encoding.UTF8.GetString(bytes);
            csv.Should().Contain("Faixa");
        }

        [Test]
        public async Task ExportAging_should_format_money_with_ptbr_comma()
        {
            FinancialEntry pending = new(1, FinancialEntryType.Receivable, FinancialEntryCategory.BrandReceivable,
                "a receber", 1500.50m, DateTimeOffset.UtcNow.AddDays(40), DateTimeOffset.UtcNow);
            db.Add(pending);
            await db.SaveChangesAsync();

            byte[] bytes = await service.ExportAging();
            string csv = Encoding.UTF8.GetString(bytes);

            csv.Should().Contain("1500,50");
        }

        [Test]
        public async Task ExportCashFlowProjection_should_emit_header_and_week_rows()
        {
            FinancialAccount account = new("Conta principal", FinancialAccountType.Bank, 0m);
            db.Add(account);
            await db.SaveChangesAsync();

            FinancialEntry receivable = new(account.Id, FinancialEntryType.Receivable, FinancialEntryCategory.BrandReceivable,
                "entrada futura", 2000m, DateTimeOffset.UtcNow.AddDays(7), DateTimeOffset.UtcNow);
            db.Add(receivable);
            await db.SaveChangesAsync();

            byte[] bytes = await service.ExportCashFlowProjection(4);
            string csv = Encoding.UTF8.GetString(bytes);

            bytes.Take(3).Should().Equal(0xEF, 0xBB, 0xBF);
            csv.Should().Contain("Semana");
            csv.Should().Contain("Saldo projetado");
            csv.Should().Contain("2000,00");
        }
    }
}
