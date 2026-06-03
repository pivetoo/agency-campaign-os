using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    // Teto de alcada (maker-checker) do repasse ao creator. Acima deste valor liquido o pagamento so e
    // agendado apos aprovacao por um usuario diferente de quem registrou. Nulo = sem teto.
    [Migration(202606030008)]
    public sealed class Migration_202606030008_AddAgencySettingsApprovalThreshold : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"ALTER TABLE agencysettings ADD COLUMN creatorpaymentapprovalthreshold NUMERIC(18,2) NULL;");
        }

        public override void Down()
        {
            Execute.Sql(@"ALTER TABLE agencysettings DROP COLUMN IF EXISTS creatorpaymentapprovalthreshold;");
        }
    }
}
