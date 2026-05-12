using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605120004)]
    public sealed class Migration_202605120004_RemoveEmailTemplateAndDefaultConnector : Migration
    {
        public override void Up()
        {
            Delete.Table("emailtemplate");
            Delete.Column("defaultemailconnectorid").FromTable("agencysettings");
        }

        public override void Down()
        {
            Alter.Table("agencysettings")
                .AddColumn("defaultemailconnectorid").AsInt64().Nullable();

            Create.Table("emailtemplate")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("name").AsString(200).NotNullable()
                .WithColumn("eventtype").AsInt32().NotNullable()
                .WithColumn("subject").AsString(300).NotNullable()
                .WithColumn("htmlbody").AsCustom("text").NotNullable()
                .WithColumn("isactive").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("createdbyuserid").AsInt64().Nullable()
                .WithColumn("createdbyusername").AsString(200).Nullable()
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();
        }
    }
}
