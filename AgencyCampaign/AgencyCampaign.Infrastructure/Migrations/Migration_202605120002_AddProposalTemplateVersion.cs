using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605120002)]
    public sealed class Migration_202605120002_AddProposalTemplateVersion : Migration
    {
        public override void Up()
        {
            Create.Table("proposaltemplateversion")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("name").AsString(100).NotNullable()
                .WithColumn("template").AsCustom("text").NotNullable()
                .WithColumn("isactive").AsBoolean().NotNullable().WithDefaultValue(false)
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();
        }

        public override void Down()
        {
            Delete.Table("proposaltemplateversion");
        }
    }
}
