using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202606120002)]
    public sealed class Migration_202606120002_SeedLinkedInPlatformLogo : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"
                UPDATE platform SET logourl = '/platforms/linkedin.svg', updatedat = NOW()
                WHERE identifier = 'linkedin' AND logourl IS NULL;
            ");
        }

        public override void Down()
        {
            Execute.Sql(@"
                UPDATE platform SET logourl = NULL, updatedat = NOW()
                WHERE identifier = 'linkedin' AND logourl = '/platforms/linkedin.svg';
            ");
        }
    }
}
