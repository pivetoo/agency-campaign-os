using AgencyCampaign.Infrastructure.Persistence.EF.Configurations;
using Archon.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AgencyCampaign.Testing.TestSupport
{
    public sealed class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
        {
        }

        public static TestDbContext CreateInMemory()
        {
            DbContextOptions<TestDbContext> options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(builder => builder.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            return new TestDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            foreach (Type entityType in DiscoverEntityTypes())
            {
                modelBuilder.Entity(entityType);
            }

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(CampaignConfiguration).Assembly);

            base.OnModelCreating(modelBuilder);
        }

        private static IEnumerable<Type> DiscoverEntityTypes()
        {
            return typeof(AgencyCampaign.Domain.Entities.Campaign).Assembly
                .GetTypes()
                .Where(type => type.IsClass && !type.IsAbstract && typeof(Entity).IsAssignableFrom(type));
        }
    }
}
