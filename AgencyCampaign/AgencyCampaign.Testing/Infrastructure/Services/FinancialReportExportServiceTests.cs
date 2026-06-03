using System.Text;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using AgencyCampaign.Infrastructure.Services;
using AgencyCampaign.Testing.TestSupport;

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
            service = new FinancialReportExportService(new FinancialReportService(db));
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
    }
}
