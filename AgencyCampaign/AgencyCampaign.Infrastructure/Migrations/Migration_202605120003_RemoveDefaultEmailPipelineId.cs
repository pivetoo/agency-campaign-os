using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605120003)]
    public sealed class Migration_202605120003_RemoveDefaultEmailPipelineId : Migration
    {
        public override void Up()
        {
            Delete.Column("defaultemailpipelineid").FromTable("agencysettings");
        }

        public override void Down()
        {
            Alter.Table("agencysettings")
                .AddColumn("defaultemailpipelineid").AsInt64().Nullable();
        }
    }
}
