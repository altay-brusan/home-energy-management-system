-- =====================================================================
-- Add Battery Configuration Parameters
-- Parameters: BatteryCapacity, MaxBatteryChargeCurrent, MaxBatteryDischargeCurrent
-- Created: 2025-01-13
-- =====================================================================

-- =====================================================================
-- PART 1: CREATE SUBGROUP (if not exists)
-- =====================================================================
-- Add "BatteryConfiguration" subgroup under "BatterySettings"

INSERT INTO "ParameterGroups" ("Id", "ParentId", "Name", "Description", "Version", "SortOrder", "Created", "CreatedBy", "LastModified", "LastModifiedBy")
SELECT
    gen_random_uuid(),
    pg."Id",
    'BatteryConfiguration',
    'Battery Configuration - Capacity and current limits',
    '1.0',
    0,  -- SortOrder 0 to appear before ChargeSources (1) and ProtectionThresholds (2)
    NOW(),
    'system',
    NOW(),
    'system'
FROM "ParameterGroups" pg
WHERE pg."Name" = 'BatterySettings'
  AND pg."ParentId" IS NULL
  AND NOT EXISTS (
      SELECT 1 FROM "ParameterGroups"
      WHERE "Name" = 'BatteryConfiguration'
        AND "ParentId" = pg."Id"
  );

-- =====================================================================
-- PART 2: ADD PARAMETERS (if not exists)
-- =====================================================================

-- Parameter 1: BatteryCapacity
INSERT INTO "Parameters" ("Id", "ParameterGroupId", "Name", "Description", "ElementType", "RoleType", "Unit", "DefaultValue", "SortOrder", "Created", "CreatedBy", "LastModified", "LastModifiedBy")
SELECT
    gen_random_uuid(),
    pg."Id",
    'BatteryCapacity',
    'Battery Capacity',
    'Decimal',
    'User',
    'Ah',
    '200',
    1,
    NOW(),
    'system',
    NOW(),
    'system'
FROM "ParameterGroups" pg
WHERE pg."Name" = 'BatteryConfiguration'
  AND NOT EXISTS (
      SELECT 1 FROM "Parameters" WHERE "Name" = 'BatteryCapacity'
  );

-- Parameter 2: MaxBatteryChargeCurrent
INSERT INTO "Parameters" ("Id", "ParameterGroupId", "Name", "Description", "ElementType", "RoleType", "Unit", "DefaultValue", "SortOrder", "Created", "CreatedBy", "LastModified", "LastModifiedBy")
SELECT
    gen_random_uuid(),
    pg."Id",
    'MaxBatteryChargeCurrent',
    'Max Battery Charge Current',
    'Decimal',
    'User',
    'A',
    '100',
    2,
    NOW(),
    'system',
    NOW(),
    'system'
FROM "ParameterGroups" pg
WHERE pg."Name" = 'BatteryConfiguration'
  AND NOT EXISTS (
      SELECT 1 FROM "Parameters" WHERE "Name" = 'MaxBatteryChargeCurrent'
  );

-- Parameter 3: MaxBatteryDischargeCurrent
INSERT INTO "Parameters" ("Id", "ParameterGroupId", "Name", "Description", "ElementType", "RoleType", "Unit", "DefaultValue", "SortOrder", "Created", "CreatedBy", "LastModified", "LastModifiedBy")
SELECT
    gen_random_uuid(),
    pg."Id",
    'MaxBatteryDischargeCurrent',
    'Max Battery Discharge Current',
    'Decimal',
    'User',
    'A',
    '100',
    3,
    NOW(),
    'system',
    NOW(),
    'system'
FROM "ParameterGroups" pg
WHERE pg."Name" = 'BatteryConfiguration'
  AND NOT EXISTS (
      SELECT 1 FROM "Parameters" WHERE "Name" = 'MaxBatteryDischargeCurrent'
  );

-- =====================================================================
-- PART 3: ADD PARAMETER VALUE SETTING SCHEMES (if not exists)
-- =====================================================================

-- Single-phase-loftty model values
INSERT INTO "ParameterValueSettingSchemes" ("Id", "ParameterId", "ParameterGroupId", "Value", "SortOrder", "ModelNumber", "Created", "CreatedBy", "LastModified", "LastModifiedBy")
SELECT
    gen_random_uuid(),
    p."Id",
    NULL,
    v.value,
    0,
    'single-phase-loftty',
    NOW(),
    'system',
    NOW(),
    'system'
FROM "Parameters" p
CROSS JOIN (VALUES
    ('BatteryCapacity', '200'),
    ('MaxBatteryChargeCurrent', '100'),
    ('MaxBatteryDischargeCurrent', '100')
) AS v(param_name, value)
WHERE p."Name" = v.param_name
  AND NOT EXISTS (
      SELECT 1 FROM "ParameterValueSettingSchemes" pvs
      WHERE pvs."ParameterId" = p."Id"
        AND pvs."ModelNumber" = 'single-phase-loftty'
  );

-- Three-phase-loftty model values
INSERT INTO "ParameterValueSettingSchemes" ("Id", "ParameterId", "ParameterGroupId", "Value", "SortOrder", "ModelNumber", "Created", "CreatedBy", "LastModified", "LastModifiedBy")
SELECT
    gen_random_uuid(),
    p."Id",
    NULL,
    v.value,
    0,
    'three-phase-loftty',
    NOW(),
    'system',
    NOW(),
    'system'
FROM "Parameters" p
CROSS JOIN (VALUES
    ('BatteryCapacity', '200'),
    ('MaxBatteryChargeCurrent', '100'),
    ('MaxBatteryDischargeCurrent', '100')
) AS v(param_name, value)
WHERE p."Name" = v.param_name
  AND NOT EXISTS (
      SELECT 1 FROM "ParameterValueSettingSchemes" pvs
      WHERE pvs."ParameterId" = p."Id"
        AND pvs."ModelNumber" = 'three-phase-loftty'
  );

-- =====================================================================
-- PART 4: ADD PARAMETERS TO EXISTING DEVICES (if not exists)
-- =====================================================================
-- This copies the new parameters from ParameterValueSettingSchemes to
-- SetupDeviceParameterValues for all existing devices that match the model.

-- Add to existing devices with single-phase-loftty model
INSERT INTO "SetupDeviceParameterValues" ("Id", "SetupDeviceId", "ParameterId", "ParameterGroupId", "Value", "SortOrder", "ModelNumber", "Created", "CreatedBy", "LastModified", "LastModifiedBy")
SELECT
    gen_random_uuid(),
    sd."Id",
    pvs."ParameterId",
    pvs."ParameterGroupId",
    pvs."Value",
    pvs."SortOrder",
    pvs."ModelNumber",
    NOW(),
    'system',
    NOW(),
    'system'
FROM "SetupDevices" sd
JOIN "Devices" d ON sd."DeviceId" = d."Id"
JOIN "ParameterValueSettingSchemes" pvs ON d."ModelNumber" ILIKE '%' || pvs."ModelNumber" || '%'
JOIN "Parameters" p ON pvs."ParameterId" = p."Id"
WHERE pvs."ModelNumber" = 'single-phase-loftty'
  AND p."Name" IN ('BatteryCapacity', 'MaxBatteryChargeCurrent', 'MaxBatteryDischargeCurrent')
  AND NOT EXISTS (
      SELECT 1 FROM "SetupDeviceParameterValues" sdpv
      WHERE sdpv."SetupDeviceId" = sd."Id"
        AND sdpv."ParameterId" = pvs."ParameterId"
  );

-- Add to existing devices with three-phase-loftty model
INSERT INTO "SetupDeviceParameterValues" ("Id", "SetupDeviceId", "ParameterId", "ParameterGroupId", "Value", "SortOrder", "ModelNumber", "Created", "CreatedBy", "LastModified", "LastModifiedBy")
SELECT
    gen_random_uuid(),
    sd."Id",
    pvs."ParameterId",
    pvs."ParameterGroupId",
    pvs."Value",
    pvs."SortOrder",
    pvs."ModelNumber",
    NOW(),
    'system',
    NOW(),
    'system'
FROM "SetupDevices" sd
JOIN "Devices" d ON sd."DeviceId" = d."Id"
JOIN "ParameterValueSettingSchemes" pvs ON d."ModelNumber" ILIKE '%' || pvs."ModelNumber" || '%'
JOIN "Parameters" p ON pvs."ParameterId" = p."Id"
WHERE pvs."ModelNumber" = 'three-phase-loftty'
  AND p."Name" IN ('BatteryCapacity', 'MaxBatteryChargeCurrent', 'MaxBatteryDischargeCurrent')
  AND NOT EXISTS (
      SELECT 1 FROM "SetupDeviceParameterValues" sdpv
      WHERE sdpv."SetupDeviceId" = sd."Id"
        AND sdpv."ParameterId" = pvs."ParameterId"
  );

-- =====================================================================
-- VERIFICATION
-- =====================================================================
SELECT 'New Parameters Added:' AS status;

SELECT
    pg."Name" AS "Group",
    p."Name" AS "Parameter",
    p."ElementType",
    p."RoleType",
    p."Unit",
    p."DefaultValue",
    p."SortOrder"
FROM "Parameters" p
JOIN "ParameterGroups" pg ON p."ParameterGroupId" = pg."Id"
WHERE p."Name" IN ('BatteryCapacity', 'MaxBatteryChargeCurrent', 'MaxBatteryDischargeCurrent')
ORDER BY p."SortOrder";

SELECT 'Parameter Value Schemes:' AS status;

SELECT
    p."Name" AS "Parameter",
    pvs."ModelNumber",
    pvs."Value"
FROM "ParameterValueSettingSchemes" pvs
JOIN "Parameters" p ON pvs."ParameterId" = p."Id"
WHERE p."Name" IN ('BatteryCapacity', 'MaxBatteryChargeCurrent', 'MaxBatteryDischargeCurrent')
ORDER BY p."Name", pvs."ModelNumber";

SELECT 'Existing Devices Updated:' AS status;

SELECT
    d."SerialNumber" AS "Device Serial",
    d."ModelNumber" AS "Device Model",
    p."Name" AS "Parameter",
    sdpv."Value",
    sdpv."Created"
FROM "SetupDeviceParameterValues" sdpv
JOIN "SetupDevices" sd ON sdpv."SetupDeviceId" = sd."Id"
JOIN "Devices" d ON sd."DeviceId" = d."Id"
JOIN "Parameters" p ON sdpv."ParameterId" = p."Id"
WHERE p."Name" IN ('BatteryCapacity', 'MaxBatteryChargeCurrent', 'MaxBatteryDischargeCurrent')
ORDER BY d."SerialNumber", p."SortOrder";
