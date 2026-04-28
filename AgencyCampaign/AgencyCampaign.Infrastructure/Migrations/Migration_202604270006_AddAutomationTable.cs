using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202604270006)]
    public sealed class Migration_202604270006_AddAutomationTable : Migration
    {
        public override void Up()
        {
            Create.Table("automation")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("name").AsString(200).NotNullable()
                .WithColumn("trigger").AsString(100).NotNullable()
                .WithColumn("triggercondition").AsString(500).Nullable()
                .WithColumn("connectorid").AsInt64().NotNullable()
                .WithColumn("pipelineid").AsInt64().NotNullable()
                .WithColumn("variablemappingjson").AsString(4000).NotNullable().WithDefaultValue("{}")
                .WithColumn("isactive").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Create.Index("ixautomationtrigger")
                .OnTable("automation")
                .OnColumn("trigger").Ascending();

            Create.Index("ixautomationisactive")
                .OnTable("automation")
                .OnColumn("isactive").Ascending();
        }

        public override void Down()
        {
            Delete.Table("automation");
        }
    }
}
