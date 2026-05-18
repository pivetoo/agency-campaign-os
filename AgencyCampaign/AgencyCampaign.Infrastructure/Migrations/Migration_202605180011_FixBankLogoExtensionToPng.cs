using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605180011)]
    public sealed class Migration_202605180011_FixBankLogoExtensionToPng : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"
                UPDATE bank
                SET logourl = REPLACE(logourl, '.svg', '.png'), updatedat = NOW()
                WHERE issystem = true AND logourl LIKE '/banks/%.svg';
            ");
        }

        public override void Down()
        {
            Execute.Sql(@"
                UPDATE bank
                SET logourl = REPLACE(logourl, '.png', '.svg'), updatedat = NOW()
                WHERE issystem = true AND logourl LIKE '/banks/%.png';
            ");
        }
    }
}
