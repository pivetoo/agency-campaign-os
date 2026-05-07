using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605070005)]
    public sealed class Migration_202605070005_AddPipelineStageSla : Migration
    {
        public override void Up()
        {
            Alter.Table("commercialpipelinestage")
                .AddColumn("slaindays").AsInt32().Nullable();

            // Defaults sensatos para os estágios padrão (podem ser editados depois)
            Execute.Sql("UPDATE commercialpipelinestage SET slaindays = 7 WHERE name = 'Lead' AND slaindays IS NULL;");
            Execute.Sql("UPDATE commercialpipelinestage SET slaindays = 14 WHERE name = 'Qualificada' AND slaindays IS NULL;");
            Execute.Sql("UPDATE commercialpipelinestage SET slaindays = 14 WHERE name = 'Proposta' AND slaindays IS NULL;");
            Execute.Sql("UPDATE commercialpipelinestage SET slaindays = 7 WHERE name = 'Negociação' AND slaindays IS NULL;");
        }

        public override void Down()
        {
            Delete.Column("slaindays").FromTable("commercialpipelinestage");
        }
    }
}
