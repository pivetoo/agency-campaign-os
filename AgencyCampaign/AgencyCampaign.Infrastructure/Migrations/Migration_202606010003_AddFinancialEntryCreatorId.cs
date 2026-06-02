using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    // Repasse de creator (FinancialEntry CreatorPayout) ganha CreatorId para permitir a baixa
    // automatica quando o CreatorPayment executado e marcado como pago (conciliacao previsto x executado).
    [Migration(202606010003)]
    public sealed class Migration_202606010003_AddFinancialEntryCreatorId : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"
                ALTER TABLE financialentry ADD COLUMN creatorid BIGINT NULL;
                CREATE INDEX ixfinancialentrycampaignidcreatorid ON financialentry (campaignid, creatorid);
            ");
        }

        public override void Down()
        {
            Execute.Sql(@"
                DROP INDEX IF EXISTS ixfinancialentrycampaignidcreatorid;
                ALTER TABLE financialentry DROP COLUMN IF EXISTS creatorid;
            ");
        }
    }
}
