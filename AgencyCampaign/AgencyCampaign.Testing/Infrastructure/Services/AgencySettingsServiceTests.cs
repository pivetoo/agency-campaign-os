using AgencyCampaign.Application.Localization;
using AgencyCampaign.Application.Models;
using AgencyCampaign.Application.Requests.AgencySettings;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Infrastructure.Services;
using AgencyCampaign.Testing.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace AgencyCampaign.Testing.Infrastructure.Services
{
    [TestFixture]
    public sealed class AgencySettingsServiceTests
    {
        private TestDbContext db = null!;
        private AgencySettingsService service = null!;

        [SetUp]
        public void SetUp()
        {
            db = TestDbContext.CreateInMemory();
            service = new AgencySettingsService(db, LocalizerMock.Create<AgencyCampaignResource>());
        }

        [TearDown]
        public void TearDown() => db.Dispose();

        [Test]
        public async Task Get_should_create_default_settings_when_missing()
        {
            AgencySettingsModel result = await service.Get();

            result.Id.Should().BeGreaterThan(0);
            result.AgencyName.Should().Be("Minha agência");
            (await db.Set<AgencySettings>().CountAsync()).Should().Be(1);
        }

        [Test]
        public async Task Get_should_return_existing_settings_without_duplicating()
        {
            await service.Get();
            await service.Get();

            (await db.Set<AgencySettings>().CountAsync()).Should().Be(1);
        }

        [Test]
        public async Task Update_should_replace_state_creating_record_if_missing()
        {
            AgencySettingsModel result = await service.Update(new UpdateAgencySettingsRequest
            {
                AgencyName = "Acme Agency",
                TradeName = "Acme",
                PrimaryEmail = "agency@x",
                PrimaryColor = "#fff"
            });

            result.AgencyName.Should().Be("Acme Agency");
            result.TradeName.Should().Be("Acme");
            (await db.Set<AgencySettings>().CountAsync()).Should().Be(1);
        }
    }
}
