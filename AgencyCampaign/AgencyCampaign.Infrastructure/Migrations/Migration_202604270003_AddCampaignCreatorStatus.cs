using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202604270003)]
    public sealed class Migration_202604270003_AddCampaignCreatorStatus : Migration
    {
        public override void Up()
        {
            Create.Table("campaigncreatorstatus")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("name").AsString(120).NotNullable()
                .WithColumn("description").AsString(500).Nullable()
                .WithColumn("displayorder").AsInt32().NotNullable().WithDefaultValue(0)
                .WithColumn("color").AsString(32).NotNullable().WithDefaultValue("#6366f1")
                .WithColumn("isinitial").AsBoolean().NotNullable().WithDefaultValue(false)
                .WithColumn("isfinal").AsBoolean().NotNullable().WithDefaultValue(false)
                .WithColumn("category").AsInt32().NotNullable().WithDefaultValue(0)
                .WithColumn("isactive").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Insert.IntoTable("campaigncreatorstatus").Row(new { name = "Convidado", description = "Creator foi convidado para participar da campanha", displayorder = 1, color = "#6b7280", isinitial = true, isfinal = false, category = 0, isactive = true, createdat = SystemMethods.CurrentUTCDateTime, updatedat = SystemMethods.CurrentUTCDateTime });
            Insert.IntoTable("campaigncreatorstatus").Row(new { name = "Pendente de aprovação", description = "Aguardando aprovação interna ou do cliente", displayorder = 2, color = "#0ea5e9", isinitial = false, isfinal = false, category = 0, isactive = true, createdat = SystemMethods.CurrentUTCDateTime, updatedat = SystemMethods.CurrentUTCDateTime });
            Insert.IntoTable("campaigncreatorstatus").Row(new { name = "Confirmado", description = "Creator aceitou participar da campanha", displayorder = 3, color = "#10b981", isinitial = false, isfinal = false, category = 0, isactive = true, createdat = SystemMethods.CurrentUTCDateTime, updatedat = SystemMethods.CurrentUTCDateTime });
            Insert.IntoTable("campaigncreatorstatus").Row(new { name = "Em execução", description = "Creator está produzindo o conteúdo", displayorder = 4, color = "#f59e0b", isinitial = false, isfinal = false, category = 0, isactive = true, createdat = SystemMethods.CurrentUTCDateTime, updatedat = SystemMethods.CurrentUTCDateTime });
            Insert.IntoTable("campaigncreatorstatus").Row(new { name = "Entregue", description = "Conteúdo foi entregue e publicado", displayorder = 5, color = "#8b5cf6", isinitial = false, isfinal = true, category = 1, isactive = true, createdat = SystemMethods.CurrentUTCDateTime, updatedat = SystemMethods.CurrentUTCDateTime });
            Insert.IntoTable("campaigncreatorstatus").Row(new { name = "Cancelado", description = "Creator foi removido da campanha", displayorder = 6, color = "#ef4444", isinitial = false, isfinal = true, category = 2, isactive = true, createdat = SystemMethods.CurrentUTCDateTime, updatedat = SystemMethods.CurrentUTCDateTime });

            Alter.Table("campaigncreator")
                .AddColumn("campaigncreatorstatusid").AsInt64().Nullable();

            Execute.Sql("""
                UPDATE campaigncreator
                SET campaigncreatorstatusid = status_map.id
                FROM (
                    SELECT 1 AS oldstatus, id FROM campaigncreatorstatus WHERE name = 'Convidado'
                    UNION ALL
                    SELECT 2 AS oldstatus, id FROM campaigncreatorstatus WHERE name = 'Pendente de aprovação'
                    UNION ALL
                    SELECT 3 AS oldstatus, id FROM campaigncreatorstatus WHERE name = 'Confirmado'
                    UNION ALL
                    SELECT 4 AS oldstatus, id FROM campaigncreatorstatus WHERE name = 'Em execução'
                    UNION ALL
                    SELECT 5 AS oldstatus, id FROM campaigncreatorstatus WHERE name = 'Entregue'
                    UNION ALL
                    SELECT 6 AS oldstatus, id FROM campaigncreatorstatus WHERE name = 'Cancelado'
                ) AS status_map
                WHERE campaigncreator.status = status_map.oldstatus;
                """);

            Execute.Sql("""
                UPDATE campaigncreator
                SET campaigncreatorstatusid = (SELECT id FROM campaigncreatorstatus WHERE isinitial = true ORDER BY displayorder LIMIT 1)
                WHERE campaigncreatorstatusid IS NULL;
                """);

            Alter.Column("campaigncreatorstatusid").OnTable("campaigncreator").AsInt64().NotNullable();

            Create.ForeignKey("fkcampaigncreatorcampaigncreatorstatus")
                .FromTable("campaigncreator").ForeignColumn("campaigncreatorstatusid")
                .ToTable("campaigncreatorstatus").PrimaryColumn("id");

            Create.Index("ixcampaigncreatorcampaigncreatorstatusid")
                .OnTable("campaigncreator")
                .OnColumn("campaigncreatorstatusid").Ascending();

            Delete.Column("status").FromTable("campaigncreator");
        }

        public override void Down()
        {
            Alter.Table("campaigncreator")
                .AddColumn("status").AsInt32().NotNullable().WithDefaultValue(1);

            Execute.Sql("""
                UPDATE campaigncreator
                SET status = mapped.status
                FROM (
                    SELECT id, 1 AS status FROM campaigncreatorstatus WHERE name = 'Convidado'
                    UNION ALL
                    SELECT id, 2 AS status FROM campaigncreatorstatus WHERE name = 'Pendente de aprovação'
                    UNION ALL
                    SELECT id, 3 AS status FROM campaigncreatorstatus WHERE name = 'Confirmado'
                    UNION ALL
                    SELECT id, 4 AS status FROM campaigncreatorstatus WHERE name = 'Em execução'
                    UNION ALL
                    SELECT id, 5 AS status FROM campaigncreatorstatus WHERE name = 'Entregue'
                    UNION ALL
                    SELECT id, 6 AS status FROM campaigncreatorstatus WHERE name = 'Cancelado'
                ) AS mapped
                WHERE campaigncreator.campaigncreatorstatusid = mapped.id;
                """);

            Delete.Index("ixcampaigncreatorcampaigncreatorstatusid").OnTable("campaigncreator");
            Delete.ForeignKey("fkcampaigncreatorcampaigncreatorstatus").OnTable("campaigncreator");
            Delete.Column("campaigncreatorstatusid").FromTable("campaigncreator");
            Delete.Table("campaigncreatorstatus");
        }
    }
}
