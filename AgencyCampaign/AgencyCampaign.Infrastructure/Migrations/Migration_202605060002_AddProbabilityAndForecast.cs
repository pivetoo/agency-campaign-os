using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605060002)]
    public sealed class Migration_202605060002_AddProbabilityAndForecast : Migration
    {
        public override void Up()
        {
            Alter.Table("opportunity")
                .AddColumn("probability").AsDecimal(5, 2).NotNullable().WithDefaultValue(0)
                .AddColumn("probabilityismanual").AsBoolean().NotNullable().WithDefaultValue(false);

            Alter.Table("commercialpipelinestage")
                .AddColumn("defaultprobability").AsDecimal(5, 2).Nullable();

            Execute.Sql("UPDATE commercialpipelinestage SET defaultprobability = 10 WHERE name = 'Lead';");
            Execute.Sql("UPDATE commercialpipelinestage SET defaultprobability = 30 WHERE name = 'Qualificada';");
            Execute.Sql("UPDATE commercialpipelinestage SET defaultprobability = 60 WHERE name = 'Proposta';");
            Execute.Sql("UPDATE commercialpipelinestage SET defaultprobability = 80 WHERE name = 'Negociação';");
            Execute.Sql("UPDATE commercialpipelinestage SET defaultprobability = 100 WHERE finalbehavior = 1;");
            Execute.Sql("UPDATE commercialpipelinestage SET defaultprobability = 0 WHERE finalbehavior = 2;");

            Execute.Sql("""
                UPDATE opportunity
                SET probability = stage.defaultprobability
                FROM commercialpipelinestage stage
                WHERE opportunity.commercialpipelinestageid = stage.id
                  AND stage.defaultprobability IS NOT NULL;
                """);

            Execute.Sql("""
                UPDATE opportunity
                SET probability = 100
                FROM commercialpipelinestage stage
                WHERE opportunity.commercialpipelinestageid = stage.id
                  AND stage.finalbehavior = 1;
                """);

            Execute.Sql("""
                UPDATE opportunity
                SET probability = 0
                FROM commercialpipelinestage stage
                WHERE opportunity.commercialpipelinestageid = stage.id
                  AND stage.finalbehavior = 2;
                """);
        }

        public override void Down()
        {
            Delete.Column("defaultprobability").FromTable("commercialpipelinestage");
            Delete.Column("probabilityismanual").FromTable("opportunity");
            Delete.Column("probability").FromTable("opportunity");
        }
    }
}
