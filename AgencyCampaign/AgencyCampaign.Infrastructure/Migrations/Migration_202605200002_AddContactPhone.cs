using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605200002)]
    public sealed class Migration_202605200002_AddContactPhone : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"
                ALTER TABLE brand ADD COLUMN IF NOT EXISTS contactphone VARCHAR(50);
                ALTER TABLE opportunity ADD COLUMN IF NOT EXISTS contactphone VARCHAR(50);
            ");
        }

        public override void Down()
        {
            Execute.Sql(@"
                ALTER TABLE opportunity DROP COLUMN IF EXISTS contactphone;
                ALTER TABLE brand DROP COLUMN IF EXISTS contactphone;
            ");
        }
    }
}
