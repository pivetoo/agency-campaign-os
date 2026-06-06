using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    // Tier de assinatura em cache no tenant. 1=Essencial, 2=Pro, 3=Scale, 99=Internal.
    // Default 99 (Internal) = fail-open: tenants existentes (incl. a Mainstay) ficam com acesso total
    // ate a sincronizacao com a assinatura do IdentityManagement atribuir o tier real.
    [Migration(202606060001)]
    public sealed class Migration_202606060001_AddAgencySettingsPlanTier : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"ALTER TABLE agencysettings ADD COLUMN plantier INTEGER NOT NULL DEFAULT 99;");
        }

        public override void Down()
        {
            Execute.Sql(@"ALTER TABLE agencysettings DROP COLUMN IF EXISTS plantier;");
        }
    }
}
