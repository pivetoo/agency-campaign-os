using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202606120001)]
    public sealed class Migration_202606120001_AddPlatformLogoUrl : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"
                ALTER TABLE platform ADD COLUMN IF NOT EXISTS logourl VARCHAR(300);

                UPDATE platform SET logourl = data.url, updatedat = NOW()
                FROM (VALUES
                    ('instagram', 'https://cdn.simpleicons.org/instagram'),
                    ('tiktok',    'https://cdn.simpleicons.org/tiktok'),
                    ('youtube',   'https://cdn.simpleicons.org/youtube'),
                    ('facebook',  'https://cdn.simpleicons.org/facebook'),
                    ('twitter',   'https://cdn.simpleicons.org/x'),
                    ('twitch',    'https://cdn.simpleicons.org/twitch'),
                    ('pinterest', 'https://cdn.simpleicons.org/pinterest'),
                    ('kwai',      'https://cdn.simpleicons.org/kuaishou')
                ) AS data(identifier, url)
                WHERE platform.identifier = data.identifier AND platform.logourl IS NULL;
            ");
        }

        public override void Down()
        {
            Execute.Sql("ALTER TABLE platform DROP COLUMN IF EXISTS logourl;");
        }
    }
}
