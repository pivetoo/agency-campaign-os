using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605180009)]
    public sealed class Migration_202605180009_AddBankCreatedByUserName : Migration
    {
        public override void Up()
        {
            Execute.Sql("ALTER TABLE bank ADD COLUMN IF NOT EXISTS createdbyusername VARCHAR(160) NULL;");
        }

        public override void Down()
        {
            Execute.Sql("ALTER TABLE bank DROP COLUMN IF EXISTS createdbyusername;");
        }
    }
}
