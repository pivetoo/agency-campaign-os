using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202604130005)]
    public sealed class Migration_202604130005_ParameterizePlatformsAndDeliverableKinds : Migration
    {
        public override void Up()
        {
            Create.Table("platform")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("name").AsString(120).NotNullable()
                .WithColumn("isactive").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("displayorder").AsInt32().NotNullable().WithDefaultValue(0)
                .WithColumn("createdat").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Create.Table("deliverablekind")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("name").AsString(120).NotNullable()
                .WithColumn("isactive").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("displayorder").AsInt32().NotNullable().WithDefaultValue(0)
                .WithColumn("createdat").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Execute.Sql(@"
                INSERT INTO platform (name, isactive, displayorder, createdat) VALUES
                ('Instagram', true, 1, NOW()),
                ('TikTok', true, 2, NOW()),
                ('YouTube', true, 3, NOW()),
                ('Kwai', true, 4, NOW()),
                ('X', true, 5, NOW()),
                ('Other', true, 6, NOW());
            ");

            Execute.Sql(@"
                INSERT INTO deliverablekind (name, isactive, displayorder, createdat) VALUES
                ('Reel', true, 1, NOW()),
                ('Story', true, 2, NOW()),
                ('Feed Post', true, 3, NOW()),
                ('Video', true, 4, NOW()),
                ('Live', true, 5, NOW()),
                ('Combo', true, 6, NOW()),
                ('Other', true, 7, NOW());
            ");

            Alter.Table("campaigndeliverable")
                .AddColumn("deliverablekindid").AsInt64().Nullable()
                .AddColumn("platformid").AsInt64().Nullable();

            Execute.Sql(@"
                UPDATE campaigndeliverable SET deliverablekindid = 1 WHERE type = 1;
                UPDATE campaigndeliverable SET deliverablekindid = 2 WHERE type = 2;
                UPDATE campaigndeliverable SET deliverablekindid = 3 WHERE type = 3;
                UPDATE campaigndeliverable SET deliverablekindid = 4 WHERE type = 4;
                UPDATE campaigndeliverable SET deliverablekindid = 5 WHERE type = 5;
                UPDATE campaigndeliverable SET deliverablekindid = 6 WHERE type = 6;
                UPDATE campaigndeliverable SET deliverablekindid = 7 WHERE type = 7;

                UPDATE campaigndeliverable SET platformid = 1 WHERE platform = 1;
                UPDATE campaigndeliverable SET platformid = 2 WHERE platform = 2;
                UPDATE campaigndeliverable SET platformid = 3 WHERE platform = 3;
                UPDATE campaigndeliverable SET platformid = 4 WHERE platform = 4;
                UPDATE campaigndeliverable SET platformid = 5 WHERE platform = 5;
                UPDATE campaigndeliverable SET platformid = 6 WHERE platform = 6;
            ");

            Alter.Column("deliverablekindid").OnTable("campaigndeliverable").AsInt64().NotNullable();
            Alter.Column("platformid").OnTable("campaigndeliverable").AsInt64().NotNullable();

            Create.ForeignKey("fkcampaigndeliverabledeliverablekinddeliverablekindid")
                .FromTable("campaigndeliverable").ForeignColumn("deliverablekindid")
                .ToTable("deliverablekind").PrimaryColumn("id");

            Create.ForeignKey("fkcampaigndeliverableplatformplatformid")
                .FromTable("campaigndeliverable").ForeignColumn("platformid")
                .ToTable("platform").PrimaryColumn("id");

            Create.Index("ixcampaigndeliverabledeliverablekindid")
                .OnTable("campaigndeliverable")
                .OnColumn("deliverablekindid").Ascending();

            Create.Index("ixcampaigndeliverableplatformid")
                .OnTable("campaigndeliverable")
                .OnColumn("platformid").Ascending();

            Delete.Column("type").FromTable("campaigndeliverable");
            Delete.Column("platform").FromTable("campaigndeliverable");
        }

        public override void Down()
        {
            Alter.Table("campaigndeliverable")
                .AddColumn("type").AsInt32().NotNullable().WithDefaultValue(7)
                .AddColumn("platform").AsInt32().NotNullable().WithDefaultValue(6);

            Execute.Sql(@"
                UPDATE campaigndeliverable SET type = 1 WHERE deliverablekindid = 1;
                UPDATE campaigndeliverable SET type = 2 WHERE deliverablekindid = 2;
                UPDATE campaigndeliverable SET type = 3 WHERE deliverablekindid = 3;
                UPDATE campaigndeliverable SET type = 4 WHERE deliverablekindid = 4;
                UPDATE campaigndeliverable SET type = 5 WHERE deliverablekindid = 5;
                UPDATE campaigndeliverable SET type = 6 WHERE deliverablekindid = 6;
                UPDATE campaigndeliverable SET type = 7 WHERE deliverablekindid = 7;

                UPDATE campaigndeliverable SET platform = 1 WHERE platformid = 1;
                UPDATE campaigndeliverable SET platform = 2 WHERE platformid = 2;
                UPDATE campaigndeliverable SET platform = 3 WHERE platformid = 3;
                UPDATE campaigndeliverable SET platform = 4 WHERE platformid = 4;
                UPDATE campaigndeliverable SET platform = 5 WHERE platformid = 5;
                UPDATE campaigndeliverable SET platform = 6 WHERE platformid = 6;
            ");

            Delete.Index("ixcampaigndeliverableplatformid").OnTable("campaigndeliverable");
            Delete.Index("ixcampaigndeliverabledeliverablekindid").OnTable("campaigndeliverable");
            Delete.ForeignKey("fkcampaigndeliverableplatformplatformid").OnTable("campaigndeliverable");
            Delete.ForeignKey("fkcampaigndeliverabledeliverablekinddeliverablekindid").OnTable("campaigndeliverable");
            Delete.Column("platformid").FromTable("campaigndeliverable");
            Delete.Column("deliverablekindid").FromTable("campaigndeliverable");

            Delete.Table("deliverablekind");
            Delete.Table("platform");
        }
    }
}
