using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605180013)]
    public sealed class Migration_202605180013_RemoveWalletAndCreditCardTypes : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"
                UPDATE financialaccount
                SET type = 1, updatedat = NOW()
                WHERE type IN (3, 4);
            ");
        }

        public override void Down()
        {
        }
    }
}
