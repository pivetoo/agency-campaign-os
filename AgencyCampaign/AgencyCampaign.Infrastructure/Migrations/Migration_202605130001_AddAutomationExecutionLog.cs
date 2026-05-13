using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605130001)]
    public sealed class Migration_202605130001_AddAutomationExecutionLog : Migration
    {
        public override void Up()
        {
            Create.Table("automationexecutionlog")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("automationid").AsInt64().NotNullable()
                .WithColumn("automationname").AsString(150).NotNullable()
                .WithColumn("trigger").AsString(100).NotNullable()
                .WithColumn("succeeded").AsBoolean().NotNullable()
                .WithColumn("renderedpayload").AsCustom("text").Nullable()
                .WithColumn("errormessage").AsString(2000).Nullable()
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Create.ForeignKey("fkautomationexecutionlogautomationautomationid")
                .FromTable("automationexecutionlog").ForeignColumn("automationid")
                .ToTable("automation").PrimaryColumn("id");

            Create.Index("ixautomationexecutionlogautomationid")
                .OnTable("automationexecutionlog")
                .OnColumn("automationid");

            Create.Index("ixautomationexecutionlogtrigger")
                .OnTable("automationexecutionlog")
                .OnColumn("trigger");
        }

        public override void Down()
        {
            Delete.ForeignKey("fkautomationexecutionlogautomationautomationid").OnTable("automationexecutionlog");
            Delete.Index("ixautomationexecutionlogautomationid").OnTable("automationexecutionlog");
            Delete.Index("ixautomationexecutionlogtrigger").OnTable("automationexecutionlog");
            Delete.Table("automationexecutionlog");
        }
    }
}
