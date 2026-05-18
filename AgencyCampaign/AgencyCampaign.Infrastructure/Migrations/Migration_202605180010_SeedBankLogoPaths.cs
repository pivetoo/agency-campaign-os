using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605180010)]
    public sealed class Migration_202605180010_SeedBankLogoPaths : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"
                UPDATE bank
                SET logourl = '/banks/' || compe || '.png', updatedat = NOW()
                WHERE issystem = true AND logourl IS NULL;
            ");
        }

        public override void Down()
        {
            Execute.Sql(@"
                UPDATE bank
                SET logourl = NULL, updatedat = NOW()
                WHERE issystem = true AND logourl LIKE '/banks/%';
            ");
        }
    }
}
