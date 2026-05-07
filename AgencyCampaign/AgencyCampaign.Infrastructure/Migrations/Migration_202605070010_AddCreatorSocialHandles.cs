using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605070010)]
    public sealed class Migration_202605070010_AddCreatorSocialHandles : Migration
    {
        public override void Up()
        {
            Create.Table("creatorsocialhandle")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("creatorid").AsInt64().NotNullable()
                .WithColumn("platformid").AsInt64().NotNullable()
                .WithColumn("handle").AsString(120).NotNullable()
                .WithColumn("profileurl").AsString(500).Nullable()
                .WithColumn("followers").AsInt64().Nullable()
                .WithColumn("engagementrate").AsDecimal(5, 2).Nullable()
                .WithColumn("isprimary").AsBoolean().NotNullable().WithDefaultValue(false)
                .WithColumn("isactive").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Create.ForeignKey("fkcreatorsocialhandlecreator")
                .FromTable("creatorsocialhandle").ForeignColumn("creatorid")
                .ToTable("creator").PrimaryColumn("id")
                .OnDelete(System.Data.Rule.Cascade);

            Create.ForeignKey("fkcreatorsocialhandleplatform")
                .FromTable("creatorsocialhandle").ForeignColumn("platformid")
                .ToTable("platform").PrimaryColumn("id");

            Create.Index("uxcreatorsocialhandlecreatoridplatformid")
                .OnTable("creatorsocialhandle")
                .OnColumn("creatorid").Ascending()
                .OnColumn("platformid").Ascending()
                .WithOptions().Unique();
        }

        public override void Down()
        {
            Delete.Table("creatorsocialhandle");
        }
    }
}
