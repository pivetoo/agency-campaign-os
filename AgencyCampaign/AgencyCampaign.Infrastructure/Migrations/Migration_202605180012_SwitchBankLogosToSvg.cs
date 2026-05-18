using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605180012)]
    public sealed class Migration_202605180012_SwitchBankLogosToSvg : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"
                UPDATE bank
                SET logourl = REPLACE(logourl, '.png', '.svg'), updatedat = NOW()
                WHERE issystem = true AND logourl LIKE '/banks/%.png';
            ");
        }

        public override void Down()
        {
            Execute.Sql(@"
                UPDATE bank
                SET logourl = REPLACE(logourl, '.svg', '.png'), updatedat = NOW()
                WHERE issystem = true AND logourl LIKE '/banks/%.svg';
            ");
        }
    }
}
