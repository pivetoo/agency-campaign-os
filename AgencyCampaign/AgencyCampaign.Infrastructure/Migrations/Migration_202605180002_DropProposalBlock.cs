using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605180002)]
    public sealed class Migration_202605180002_DropProposalBlock : Migration
    {
        public override void Up()
        {
            Execute.Sql("DROP TABLE IF EXISTS proposalblock CASCADE;");
        }

        public override void Down()
        {
        }
    }
}
