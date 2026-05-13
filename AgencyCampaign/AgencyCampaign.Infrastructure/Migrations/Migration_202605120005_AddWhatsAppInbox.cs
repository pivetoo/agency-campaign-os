using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605120005)]
    public sealed class Migration_202605120005_AddWhatsAppInbox : Migration
    {
        public override void Up()
        {
            Create.Table("whatsappconversation")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("connectorid").AsInt64().Nullable()
                .WithColumn("contactphone").AsString(50).NotNullable()
                .WithColumn("contactname").AsString(200).Nullable()
                .WithColumn("lastmessageat").AsDateTimeOffset().Nullable()
                .WithColumn("lastmessagepreview").AsString(200).Nullable()
                .WithColumn("unreadcount").AsInt32().NotNullable().WithDefaultValue(0)
                .WithColumn("isactive").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Create.Index("ixwhatsappconversationcontactphone")
                .OnTable("whatsappconversation")
                .OnColumn("contactphone");

            Create.Table("whatsappmessage")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("conversationid").AsInt64().NotNullable()
                .WithColumn("externalid").AsString(200).Nullable()
                .WithColumn("direction").AsInt32().NotNullable()
                .WithColumn("content").AsCustom("text").NotNullable()
                .WithColumn("sentat").AsDateTimeOffset().NotNullable()
                .WithColumn("isread").AsBoolean().NotNullable().WithDefaultValue(false)
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Create.ForeignKey("fkwhatsappmessagewhatsappconversationconversationid")
                .FromTable("whatsappmessage").ForeignColumn("conversationid")
                .ToTable("whatsappconversation").PrimaryColumn("id");

            Create.Index("ixwhatsappmessageconversationid")
                .OnTable("whatsappmessage")
                .OnColumn("conversationid");

            Alter.Table("agencysettings")
                .AddColumn("whatsappconnectorid").AsInt64().Nullable();
        }

        public override void Down()
        {
            Delete.Column("whatsappconnectorid").FromTable("agencysettings");
            Delete.ForeignKey("fkwhatsappmessagewhatsappconversationconversationid").OnTable("whatsappmessage");
            Delete.Table("whatsappmessage");
            Delete.Table("whatsappconversation");
        }
    }
}
