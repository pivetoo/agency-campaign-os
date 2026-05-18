using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605180007)]
    public sealed class Migration_202605180007_RemoveSeededFinancialAccount : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"
                DELETE FROM financialaccount
                WHERE name = 'Caixa'
                  AND type = 2
                  AND initialbalance = 0
                  AND color = '#6366f1'
                  AND NOT EXISTS (
                      SELECT 1 FROM financialentry
                      WHERE financialentry.accountid = financialaccount.id
                  )
                  AND NOT EXISTS (
                      SELECT 1 FROM banktransaction
                      WHERE banktransaction.accountid = financialaccount.id
                  );
            ");
        }

        public override void Down()
        {
        }
    }
}
