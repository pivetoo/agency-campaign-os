using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202604260001)]
    public sealed class Migration_202604260001_AddConfigurableCommercialPipelineStages : Migration
    {
        public override void Up()
        {
            Create.Table("commercialpipelinestage")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("name").AsString(120).NotNullable()
                .WithColumn("description").AsString(500).Nullable()
                .WithColumn("displayorder").AsInt32().NotNullable().WithDefaultValue(0)
                .WithColumn("color").AsString(32).NotNullable().WithDefaultValue("#6366f1")
                .WithColumn("isinitial").AsBoolean().NotNullable().WithDefaultValue(false)
                .WithColumn("isfinal").AsBoolean().NotNullable().WithDefaultValue(false)
                .WithColumn("finalbehavior").AsInt32().NotNullable().WithDefaultValue(0)
                .WithColumn("isactive").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Insert.IntoTable("commercialpipelinestage").Row(new { name = "Lead", description = "Novas marcas ou oportunidades ainda em triagem", displayorder = 1, color = "#6b7280", isinitial = true, isfinal = false, finalbehavior = 0, isactive = true, createdat = SystemMethods.CurrentUTCDateTime, updatedat = SystemMethods.CurrentUTCDateTime });
            Insert.IntoTable("commercialpipelinestage").Row(new { name = "Qualificada", description = "Leads com potencial validado para campanha", displayorder = 2, color = "#0ea5e9", isinitial = false, isfinal = false, finalbehavior = 0, isactive = true, createdat = SystemMethods.CurrentUTCDateTime, updatedat = SystemMethods.CurrentUTCDateTime });
            Insert.IntoTable("commercialpipelinestage").Row(new { name = "Proposta", description = "Oportunidades com proposta em elaboração ou enviada", displayorder = 3, color = "#8b5cf6", isinitial = false, isfinal = false, finalbehavior = 0, isactive = true, createdat = SystemMethods.CurrentUTCDateTime, updatedat = SystemMethods.CurrentUTCDateTime });
            Insert.IntoTable("commercialpipelinestage").Row(new { name = "Negociação", description = "Valores, escopo e condições em negociação", displayorder = 4, color = "#f59e0b", isinitial = false, isfinal = false, finalbehavior = 0, isactive = true, createdat = SystemMethods.CurrentUTCDateTime, updatedat = SystemMethods.CurrentUTCDateTime });
            Insert.IntoTable("commercialpipelinestage").Row(new { name = "Ganha", description = "Receita conquistada pronta para virar campanha", displayorder = 5, color = "#10b981", isinitial = false, isfinal = true, finalbehavior = 1, isactive = true, createdat = SystemMethods.CurrentUTCDateTime, updatedat = SystemMethods.CurrentUTCDateTime });
            Insert.IntoTable("commercialpipelinestage").Row(new { name = "Perdida", description = "Oportunidades encerradas sem fechamento", displayorder = 6, color = "#ef4444", isinitial = false, isfinal = true, finalbehavior = 2, isactive = true, createdat = SystemMethods.CurrentUTCDateTime, updatedat = SystemMethods.CurrentUTCDateTime });

            Alter.Table("opportunity")
                .AddColumn("commercialpipelinestageid").AsInt64().Nullable();

            Execute.Sql("""
                UPDATE opportunity
                SET commercialpipelinestageid = stage_map.id
                FROM (
                    SELECT 1 AS oldstage, id FROM commercialpipelinestage WHERE name = 'Lead'
                    UNION ALL
                    SELECT 2 AS oldstage, id FROM commercialpipelinestage WHERE name = 'Qualificada'
                    UNION ALL
                    SELECT 3 AS oldstage, id FROM commercialpipelinestage WHERE name = 'Proposta'
                    UNION ALL
                    SELECT 4 AS oldstage, id FROM commercialpipelinestage WHERE name = 'Negociação'
                    UNION ALL
                    SELECT 5 AS oldstage, id FROM commercialpipelinestage WHERE name = 'Ganha'
                    UNION ALL
                    SELECT 6 AS oldstage, id FROM commercialpipelinestage WHERE name = 'Perdida'
                ) AS stage_map
                WHERE opportunity.stage = stage_map.oldstage;
                """);

            Execute.Sql("""
                UPDATE opportunity
                SET commercialpipelinestageid = (SELECT id FROM commercialpipelinestage WHERE isinitial = true ORDER BY displayorder LIMIT 1)
                WHERE commercialpipelinestageid IS NULL;
                """);

            Alter.Column("commercialpipelinestageid").OnTable("opportunity").AsInt64().NotNullable();

            Create.ForeignKey("fkopportunitycommercialpipelinestage")
                .FromTable("opportunity").ForeignColumn("commercialpipelinestageid")
                .ToTable("commercialpipelinestage").PrimaryColumn("id");

            Create.Index("ixopportunitycommercialpipelinestageid")
                .OnTable("opportunity")
                .OnColumn("commercialpipelinestageid").Ascending();

            Delete.Index("ixopportunitystage").OnTable("opportunity");
            Delete.Column("stage").FromTable("opportunity");
        }

        public override void Down()
        {
            Alter.Table("opportunity")
                .AddColumn("stage").AsInt32().NotNullable().WithDefaultValue(1);

            Execute.Sql("""
                UPDATE opportunity
                SET stage = mapped.stage
                FROM (
                    SELECT id, 5 AS stage FROM commercialpipelinestage WHERE finalbehavior = 1
                    UNION ALL
                    SELECT id, 6 AS stage FROM commercialpipelinestage WHERE finalbehavior = 2
                    UNION ALL
                    SELECT id, 1 AS stage FROM commercialpipelinestage WHERE name = 'Lead'
                    UNION ALL
                    SELECT id, 2 AS stage FROM commercialpipelinestage WHERE name = 'Qualificada'
                    UNION ALL
                    SELECT id, 3 AS stage FROM commercialpipelinestage WHERE name = 'Proposta'
                    UNION ALL
                    SELECT id, 4 AS stage FROM commercialpipelinestage WHERE name = 'Negociação'
                ) AS mapped
                WHERE opportunity.commercialpipelinestageid = mapped.id;
                """);

            Create.Index("ixopportunitystage")
                .OnTable("opportunity")
                .OnColumn("stage").Ascending();

            Delete.Index("ixopportunitycommercialpipelinestageid").OnTable("opportunity");
            Delete.ForeignKey("fkopportunitycommercialpipelinestage").OnTable("opportunity");
            Delete.Column("commercialpipelinestageid").FromTable("opportunity");
            Delete.Table("commercialpipelinestage");
        }
    }
}
