using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605030001)]
    public sealed class Migration_202605030001_DropLegacyIntegrationTables : Migration
    {
        public override void Up()
        {
            Delete.Table("integrationlog");
            Delete.Table("integrationpipeline");
            Delete.Table("integration");
        }

        public override void Down()
        {
            Create.Table("integration")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("identifier").AsString(100).NotNullable()
                .WithColumn("name").AsString(200).NotNullable()
                .WithColumn("description").AsString(500).Nullable()
                .WithColumn("categoryid").AsInt64().NotNullable()
                .WithColumn("isactive").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Create.Index("ixintegrationidentifier")
                .OnTable("integration")
                .OnColumn("identifier").Ascending()
                .WithOptions().Unique();

            Create.Index("ixintegrationcategoryid")
                .OnTable("integration")
                .OnColumn("categoryid").Ascending();

            Create.Table("integrationpipeline")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("integrationid").AsInt64().NotNullable()
                .WithColumn("identifier").AsString(100).NotNullable()
                .WithColumn("name").AsString(200).NotNullable()
                .WithColumn("description").AsString(500).Nullable()
                .WithColumn("configurationjson").AsString(int.MaxValue).NotNullable().WithDefaultValue("{}")
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
                .WithColumn("status").AsInt32().NotNullable()
                .WithColumn("payload").AsString(int.MaxValue).Nullable()
                .WithColumn("response").AsString(int.MaxValue).Nullable()
                .WithColumn("durationms").AsInt64().Nullable()
                .WithColumn("errormessage").AsString(int.MaxValue).Nullable()
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
    }
}
