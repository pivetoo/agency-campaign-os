using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    // Conta padrao da agencia: a auto-geracao de recebivel/repasse usa a conta marcada como padrao,
    // em vez da heuristica "primeira conta ativa por id".
    [Migration(202606030003)]
    public sealed class Migration_202606030003_AddFinancialAccountIsDefault : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"
                ALTER TABLE financialaccount ADD COLUMN isdefault BOOLEAN NOT NULL DEFAULT FALSE;
            ");
        }

        public override void Down()
        {
            Execute.Sql(@"
                ALTER TABLE financialaccount DROP COLUMN IF EXISTS isdefault;
            ");
        }
    }
}
