using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605280007)]
    public sealed class Migration_202605280007_AddAgencySettingsEmvRate : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"ALTER TABLE agencysettings ADD COLUMN emvcpmrate NUMERIC(14,2) NULL;");
        }

        public override void Down()
        {
            Execute.Sql(@"ALTER TABLE agencysettings DROP COLUMN IF EXISTS emvcpmrate;");
        }
    }
}
