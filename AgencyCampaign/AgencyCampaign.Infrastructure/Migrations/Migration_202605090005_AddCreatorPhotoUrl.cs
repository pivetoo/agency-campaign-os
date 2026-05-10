using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605090005)]
    public sealed class Migration_202605090005_AddCreatorPhotoUrl : Migration
    {
        public override void Up()
        {
            Alter.Table("creator")
                .AddColumn("photourl").AsString(255).Nullable();
        }

        public override void Down()
        {
            Delete.Column("photourl").FromTable("creator");
        }
    }
}
