using AgencyCampaign.Application.Requests.BrandContacts;
using AgencyCampaign.Application.Services;
using AgencyCampaign.Domain.Entities;
using AgencyCampaign.Domain.ValueObjects;
using AgencyCampaign.Infrastructure.Services;
using AgencyCampaign.Testing.TestSupport;
using FluentAssertions;
using NUnit.Framework;

namespace AgencyCampaign.Testing.Infrastructure.Services
{
    [TestFixture]
    public sealed class BrandContactServiceTests
    {
        private TestDbContext db = null!;
        private BrandContactService service = null!;

        [SetUp]
        public void SetUp()
        {
            db = TestDbContext.CreateInMemory();
            service = new BrandContactService(db);
        }

        [TearDown]
        public void TearDown() => db.Dispose();

        private async Task SeedBrandAsync()
        {
            db.Add(new Brand("Acme").WithId(1));
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
        }

        [Test]
        public async Task Add_first_of_type_becomes_primary_and_mirrors_to_brand()
        {
            await SeedBrandAsync();

            await service.Add(1, new AddBrandContactRequest { Type = BrandContactType.Email, Value = "a@x.com" });
            BrandContactModel second = await service.Add(1, new AddBrandContactRequest { Type = BrandContactType.Email, Value = "b@x.com" });

            IReadOnlyList<BrandContactModel> list = await service.GetByBrand(1);
            list.Should().HaveCount(2);
            list.Count(item => item.IsPrimary).Should().Be(1);
            second.IsPrimary.Should().BeFalse();

            db.Set<Brand>().Single(item => item.Id == 1).ContactEmail.Should().Be("a@x.com");
        }

        [Test]
        public async Task SetPrimary_moves_primary_and_updates_mirror()
        {
            await SeedBrandAsync();
            await service.Add(1, new AddBrandContactRequest { Type = BrandContactType.Email, Value = "a@x.com" });
            BrandContactModel second = await service.Add(1, new AddBrandContactRequest { Type = BrandContactType.Email, Value = "b@x.com" });

            await service.SetPrimary(second.Id);

            IReadOnlyList<BrandContactModel> list = await service.GetByBrand(1);
            list.Single(item => item.IsPrimary).Value.Should().Be("b@x.com");
            db.Set<Brand>().Single(item => item.Id == 1).ContactEmail.Should().Be("b@x.com");
        }

        [Test]
        public async Task Delete_primary_promotes_next_and_updates_mirror()
        {
            await SeedBrandAsync();
            BrandContactModel first = await service.Add(1, new AddBrandContactRequest { Type = BrandContactType.Phone, Value = "111" });
            await service.Add(1, new AddBrandContactRequest { Type = BrandContactType.Phone, Value = "222" });

            await service.Delete(first.Id);

            IReadOnlyList<BrandContactModel> list = await service.GetByBrand(1);
            list.Should().HaveCount(1);
            list.Single().IsPrimary.Should().BeTrue();
            db.Set<Brand>().Single(item => item.Id == 1).ContactPhone.Should().Be("222");
        }

        [Test]
        public async Task Add_rejects_invalid_email()
        {
            await SeedBrandAsync();

            Func<Task> act = async () => await service.Add(1, new AddBrandContactRequest { Type = BrandContactType.Email, Value = "nope" });

            await act.Should().ThrowAsync<InvalidOperationException>();
        }
    }
}
