using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605090003)]
    public sealed class Migration_202605090003_AddCreatorAccessToken : Migration
    {
        public override void Up()
        {
            Create.Table("creatoraccesstoken")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("creatorid").AsInt64().NotNullable()
                .WithColumn("token").AsString(64).NotNullable()
                .WithColumn("expiresat").AsDateTimeOffset().Nullable()
                .WithColumn("revokedat").AsDateTimeOffset().Nullable()
                .WithColumn("lastusedat").AsDateTimeOffset().Nullable()
                .WithColumn("usagecount").AsInt32().NotNullable().WithDefaultValue(0)
                .WithColumn("note").AsString(500).Nullable()
                .WithColumn("createdbyuserid").AsInt64().Nullable()
                .WithColumn("createdbyusername").AsString(150).Nullable()
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Create.ForeignKey("fkcreatoraccesstokencreatorcreatorid")
                .FromTable("creatoraccesstoken").ForeignColumn("creatorid")
                .ToTable("creator").PrimaryColumn("id");

            Create.Index("ixcreatoraccesstokencreatorid")
                .OnTable("creatoraccesstoken").OnColumn("creatorid").Ascending();

            Create.Index("ixcreatoraccesstokentoken")
                .OnTable("creatoraccesstoken").OnColumn("token").Unique();
        }

        public override void Down()
        {
            Delete.Index("ixcreatoraccesstokentoken").OnTable("creatoraccesstoken");
            Delete.Index("ixcreatoraccesstokencreatorid").OnTable("creatoraccesstoken");
            Delete.ForeignKey("fkcreatoraccesstokencreatorcreatorid").OnTable("creatoraccesstoken");
            Delete.Table("creatoraccesstoken");
        }
    }
}
