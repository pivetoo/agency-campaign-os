using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202604270004)]
    public sealed class Migration_202604270004_AddIntegrationTables : Migration
    {
        public override void Up()
        {
            Create.Table("integration")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("identifier").AsString(120).NotNullable()
                .WithColumn("name").AsString(120).NotNullable()
                .WithColumn("description").AsString(500).Nullable()
                .WithColumn("category").AsInt32().NotNullable().WithDefaultValue(6)
                .WithColumn("isactive").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Create.Index("ixintegrationidentifier")
                .OnTable("integration")
                .OnColumn("identifier").Ascending()
                .WithOptions().Unique();

            Insert.IntoTable("integration").Row(new { identifier = "email-smtp", name = "E-mail SMTP", description = "Envio de e-mails via SMTP", category = 1, isactive = true, createdat = SystemMethods.CurrentUTCDateTime, updatedat = SystemMethods.CurrentUTCDateTime });
            Insert.IntoTable("integration").Row(new { identifier = "whatsapp-api", name = "WhatsApp Business API", description = "Envio de mensagens via WhatsApp", category = 2, isactive = true, createdat = SystemMethods.CurrentUTCDateTime, updatedat = SystemMethods.CurrentUTCDateTime });
            Insert.IntoTable("integration").Row(new { identifier = "sms-gateway", name = "SMS Gateway", description = "Envio de SMS", category = 3, isactive = true, createdat = SystemMethods.CurrentUTCDateTime, updatedat = SystemMethods.CurrentUTCDateTime });
            Insert.IntoTable("integration").Row(new { identifier = "stripe", name = "Stripe", description = "Pagamentos via Stripe", category = 4, isactive = true, createdat = SystemMethods.CurrentUTCDateTime, updatedat = SystemMethods.CurrentUTCDateTime });
            Insert.IntoTable("integration").Row(new { identifier = "push-notification", name = "Push Notification", description = "Notificacoes push", category = 5, isactive = true, createdat = SystemMethods.CurrentUTCDateTime, updatedat = SystemMethods.CurrentUTCDateTime });

            Create.Table("integrationpipeline")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("integrationid").AsInt64().NotNullable()
                .WithColumn("identifier").AsString(120).NotNullable()
                .WithColumn("name").AsString(120).NotNullable()
                .WithColumn("description").AsString(500).Nullable()
                .WithColumn("isactive").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Create.ForeignKey("fkintegrationpipelineintegration")
                .FromTable("integrationpipeline").ForeignColumn("integrationid")
                .ToTable("integration").PrimaryColumn("id");

            Create.Index("ixintegrationpipelineidentifier")
                .OnTable("integrationpipeline")
                .OnColumn("identifier").Ascending()
                .WithOptions().Unique();

            Create.Index("ixintegrationpipelineintegrationid")
                .OnTable("integrationpipeline")
                .OnColumn("integrationid").Ascending();

            Create.Table("integrationlog")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("integrationpipelineid").AsInt64().NotNullable()
                .WithColumn("status").AsInt32().NotNullable().WithDefaultValue(0)
                .WithColumn("payload").AsString(4000).Nullable()
                .WithColumn("response").AsString(4000).Nullable()
                .WithColumn("durationms").AsInt64().Nullable()
                .WithColumn("errormessage").AsString(2000).Nullable()
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Create.ForeignKey("fkintegrationlogintegrationpipeline")
                .FromTable("integrationlog").ForeignColumn("integrationpipelineid")
                .ToTable("integrationpipeline").PrimaryColumn("id");

            Create.Index("ixintegrationlogintegrationpipelineid")
                .OnTable("integrationlog")
                .OnColumn("integrationpipelineid").Ascending();

            Create.Index("ixintegrationlogcreatedat")
                .OnTable("integrationlog")
                .OnColumn("createdat").Descending();
        }

        public override void Down()
        {
            Delete.Table("integrationlog");
            Delete.Table("integrationpipeline");
            Delete.Table("integration");
        }
    }
}
