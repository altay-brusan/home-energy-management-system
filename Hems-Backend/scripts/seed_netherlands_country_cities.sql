-- =====================================================
-- Netherlands Country and Cities Seed Script
-- Run this in DBeaver or any PostgreSQL client
-- =====================================================

DO $$
DECLARE
    netherlands_id UUID;
BEGIN
    -- Generate UUID for Netherlands
    netherlands_id := gen_random_uuid();

    -- Insert Netherlands
    INSERT INTO "Countries" ("Id", "Name", "Code", "IsActive", "Created", "CreatedBy", "LastModified", "LastModifiedBy")
    VALUES (
        netherlands_id,
        'Netherlands',
        'NL',
        true,
        NOW() AT TIME ZONE 'UTC',
        'System',
        NOW() AT TIME ZONE 'UTC',
        'System'
    );

    -- Insert Major Dutch Cities (Top 25 by population)
    INSERT INTO "Cities" ("Id", "Name", "CountryId", "IsActive", "Created", "CreatedBy", "LastModified", "LastModifiedBy")
    VALUES
        (gen_random_uuid(), 'Amsterdam', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Rotterdam', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'The Hague', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Utrecht', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Eindhoven', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Groningen', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Tilburg', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Almere', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Breda', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Nijmegen', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Apeldoorn', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Haarlem', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Arnhem', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Enschede', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Amersfoort', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Zaanstad', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), '''s-Hertogenbosch', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Zwolle', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Maastricht', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Leiden', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Dordrecht', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Zoetermeer', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Delft', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Leeuwarden', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System'),
        (gen_random_uuid(), 'Alkmaar', netherlands_id, true, NOW() AT TIME ZONE 'UTC', 'System', NOW() AT TIME ZONE 'UTC', 'System');

    -- Output the generated Netherlands ID
    RAISE NOTICE 'Netherlands inserted with ID: %', netherlands_id;
END $$;

-- =====================================================
-- Verification Queries
-- =====================================================
SELECT 'Netherlands Country:' as info, "Id", "Name", "Code" FROM "Countries" WHERE "Code" = 'NL';
SELECT 'Total Dutch Cities:' as info, COUNT(*) as count FROM "Cities" c
    INNER JOIN "Countries" co ON c."CountryId" = co."Id"
    WHERE co."Code" = 'NL';
