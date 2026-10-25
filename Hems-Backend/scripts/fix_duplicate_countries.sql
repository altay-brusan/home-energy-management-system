-- =====================================================
-- Fix Duplicate Countries Script
-- Run this to remove duplicate country entries
-- =====================================================

-- First, check for duplicates
SELECT "Name", "Code", COUNT(*) as count
FROM "Countries"
GROUP BY "Name", "Code"
HAVING COUNT(*) > 1;

-- Keep the first entry of each duplicate and delete the rest
-- This preserves cities that reference the original country ID
DELETE FROM "Countries" c1
WHERE c1."Id" NOT IN (
    SELECT MIN(c2."Id")
    FROM "Countries" c2
    GROUP BY c2."Name", c2."Code"
);

-- Also clean up any orphaned cities (cities referencing deleted countries)
DELETE FROM "Cities"
WHERE "CountryId" NOT IN (SELECT "Id" FROM "Countries");

-- Verify no more duplicates
SELECT 'Remaining duplicates:' as info, COUNT(*) as count
FROM (
    SELECT "Name", "Code"
    FROM "Countries"
    GROUP BY "Name", "Code"
    HAVING COUNT(*) > 1
) duplicates;

-- Show final country list
SELECT "Id", "Name", "Code", "IsActive" FROM "Countries" ORDER BY "Name";
