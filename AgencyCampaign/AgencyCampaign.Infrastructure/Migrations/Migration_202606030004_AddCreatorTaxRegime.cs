using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    // Regime tributario do creator (PF/MEI/Simples/Lucro Presumido-Real) para registrar a retencao
    // aplicavel ao pagar o creator e alimentar o relatorio de retencoes (camada fiscal - fase 1).
    [Migration(202606030004)]
    public sealed class Migration_202606030004_AddCreatorTaxRegime : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"
                ALTER TABLE creator ADD COLUMN taxregime INTEGER NULL;
            ");
        }

        public override void Down()
        {
            Execute.Sql(@"
                ALTER TABLE creator DROP COLUMN IF EXISTS taxregime;
            ");
        }
    }
}
