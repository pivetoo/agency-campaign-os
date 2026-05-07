using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605070007)]
    public sealed class Migration_202605070007_AddEmailTemplates : Migration
    {
        public override void Up()
        {
            Create.Table("emailtemplate")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("name").AsString(150).NotNullable()
                .WithColumn("eventtype").AsInt32().NotNullable()
                .WithColumn("subject").AsString(300).NotNullable()
                .WithColumn("htmlbody").AsCustom("text").NotNullable()
                .WithColumn("isactive").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("createdbyuserid").AsInt64().Nullable()
                .WithColumn("createdbyusername").AsString(255).Nullable()
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Create.Index("ixemailtemplateeventtypeisactive")
                .OnTable("emailtemplate")
                .OnColumn("eventtype").Ascending()
                .OnColumn("isactive").Ascending();
        }

        public override void Down()
        {
            Delete.Table("emailtemplate");
        }
    }
}
