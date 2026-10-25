-- Drop Todo Tables
-- These tables are leftover from the Clean Architecture template and are not used by the application.
-- Run this script to remove unused tables from the database.

-- First drop TodoItems (has FK to TodoLists)
DROP TABLE IF EXISTS "TodoItems";

-- Then drop TodoLists
DROP TABLE IF EXISTS "TodoLists";

-- Verify tables are dropped
-- SELECT table_name FROM information_schema.tables WHERE table_name IN ('TodoItems', 'TodoLists');
