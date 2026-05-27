using FluentMigrator;

namespace AgencyCampaign.Infrastructure.Migrations
{
    [Migration(202605270001)]
    public sealed class Migration_202605270001_AddPlatformIdentifier : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"
                ALTER TABLE platform ADD COLUMN IF NOT EXISTS identifier VARCHAR(60);
                ALTER TABLE platform ADD COLUMN IF NOT EXISTS issystem BOOLEAN NOT NULL DEFAULT false;
                CREATE UNIQUE INDEX IF NOT EXISTS uxplatformidentifier ON platform (identifier) WHERE identifier IS NOT NULL;

                DO $$
                DECLARE
                    rec record;
                    promoted_id bigint;
                BEGIN
                    FOR rec IN
                        SELECT * FROM (VALUES
                            ('instagram', 'Instagram', 1, ARRAY['instagram','insta','ig']),
                            ('tiktok',    'TikTok',    2, ARRAY['tiktok','tik tok']),
                            ('youtube',   'YouTube',   3, ARRAY['youtube','yt']),
                            ('facebook',  'Facebook',  4, ARRAY['facebook','fb']),
                            ('twitter',   'Twitter/X', 5, ARRAY['twitter','x','twitter/x']),
                            ('linkedin',  'LinkedIn',  6, ARRAY['linkedin']),
                            ('twitch',    'Twitch',    7, ARRAY['twitch']),
                            ('kwai',      'Kwai',      8, ARRAY['kwai']),
                            ('pinterest', 'Pinterest', 9, ARRAY['pinterest'])
                        ) AS t(identifier, name, displayorder, aliases)
                    LOOP
                        IF EXISTS (SELECT 1 FROM platform WHERE identifier = rec.identifier) THEN
                            CONTINUE;
                        END IF;

                        SELECT id INTO promoted_id FROM platform
                            WHERE identifier IS NULL AND lower(trim(name)) = ANY(rec.aliases)
                            ORDER BY id LIMIT 1;

                        IF promoted_id IS NOT NULL THEN
                            UPDATE platform SET identifier = rec.identifier, issystem = true WHERE id = promoted_id;
                        ELSE
                            INSERT INTO platform (name, isactive, displayorder, identifier, issystem, createdat)
                                VALUES (rec.name, true, rec.displayorder, rec.identifier, true, NOW());
                        END IF;
                    END LOOP;
                END $$;
            ");
        }

        public override void Down()
        {
            Execute.Sql(@"
                DROP INDEX IF EXISTS uxplatformidentifier;
                ALTER TABLE platform DROP COLUMN IF EXISTS identifier;
                ALTER TABLE platform DROP COLUMN IF EXISTS issystem;
            ");
        }
    }
}
