-- =====================================================================
-- UPDATE EXISTING DEVICE SETTINGS
-- Updates ALL tables with new default values:
-- 1. Parameters.DefaultValue (for reference)
-- 2. ParameterValueSettingSchemes.Value (for new device initialization)
-- 3. SetupDeviceParameterValues.Value (for EXISTING devices - what users see!)
--
-- IMPORTANT: This script will UPDATE existing device values!
-- Run this when you want to reset all devices to new defaults.
--
-- Created: 2025-01-19
-- Updated: 2025-01-19 - Now updates SetupDeviceParameterValues
-- =====================================================================

BEGIN;

-- =====================================================================
-- PART 1: CREATE BATTERYCONFIG SUBGROUP (if not exists)
-- =====================================================================

INSERT INTO "ParameterGroups" ("Id", "ParentId", "Name", "Description", "Version", "SortOrder", "Created", "CreatedBy", "LastModified", "LastModifiedBy")
SELECT
    gen_random_uuid(),
    pg."Id",
    'BatteryConfiguration',
    'Battery Configuration - Capacity and current limits',
    '1.0',
    0,
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
-- PART 2: ADD NEW PARAMETERS (if not exists)
-- =====================================================================

-- BatteryCapacity
INSERT INTO "Parameters" ("Id", "ParameterGroupId", "Name", "Description", "ElementType", "RoleType", "Unit", "DefaultValue", "SortOrder", "Created", "CreatedBy", "LastModified", "LastModifiedBy")
SELECT gen_random_uuid(), pg."Id", 'BatteryCapacity', 'Battery Capacity', 'Decimal', 'User', 'Ah', '200', 1, NOW(), 'system', NOW(), 'system'
FROM "ParameterGroups" pg WHERE pg."Name" = 'BatteryConfiguration' AND NOT EXISTS (SELECT 1 FROM "Parameters" WHERE "Name" = 'BatteryCapacity');

-- MaxBatteryChargeCurrent
INSERT INTO "Parameters" ("Id", "ParameterGroupId", "Name", "Description", "ElementType", "RoleType", "Unit", "DefaultValue", "SortOrder", "Created", "CreatedBy", "LastModified", "LastModifiedBy")
SELECT gen_random_uuid(), pg."Id", 'MaxBatteryChargeCurrent', 'Max Battery Charge Current', 'Decimal', 'User', 'A', '100', 2, NOW(), 'system', NOW(), 'system'
FROM "ParameterGroups" pg WHERE pg."Name" = 'BatteryConfiguration' AND NOT EXISTS (SELECT 1 FROM "Parameters" WHERE "Name" = 'MaxBatteryChargeCurrent');

-- MaxBatteryDischargeCurrent
INSERT INTO "Parameters" ("Id", "ParameterGroupId", "Name", "Description", "ElementType", "RoleType", "Unit", "DefaultValue", "SortOrder", "Created", "CreatedBy", "LastModified", "LastModifiedBy")
SELECT gen_random_uuid(), pg."Id", 'MaxBatteryDischargeCurrent', 'Max Battery Discharge Current', 'Decimal', 'User', 'A', '100', 3, NOW(), 'system', NOW(), 'system'
FROM "ParameterGroups" pg WHERE pg."Name" = 'BatteryConfiguration' AND NOT EXISTS (SELECT 1 FROM "Parameters" WHERE "Name" = 'MaxBatteryDischargeCurrent');

-- MaxSolarSellPower
INSERT INTO "Parameters" ("Id", "ParameterGroupId", "Name", "Description", "ElementType", "RoleType", "Unit", "DefaultValue", "SortOrder", "Created", "CreatedBy", "LastModified", "LastModifiedBy")
SELECT gen_random_uuid(), pg."Id", 'MaxSolarSellPower', 'Maximum Solar Sell Power', 'Decimal', 'User', 'W', '12000', 2, NOW(), 'system', NOW(), 'system'
FROM "ParameterGroups" pg WHERE pg."Name" = 'InverterMode' AND NOT EXISTS (SELECT 1 FROM "Parameters" WHERE "Name" = 'MaxSolarSellPower');


-- =====================================================================
-- PART 3: UPDATE Parameters.DefaultValue (all parameters)
-- =====================================================================

-- Create temp table with all parameter values
CREATE TEMP TABLE temp_param_values (
    param_name VARCHAR(100) PRIMARY KEY,
    default_value VARCHAR(100)
);

INSERT INTO temp_param_values (param_name, default_value) VALUES
    -- Admin parameters
    ('BatteryControlMode', 'Lithium'),
    ('LithiumBatteryWakeUpEnabled', 'true'),
    ('BatteryResistance', '100'),
    ('ChargeEfficiency', '98.3'),
    ('GridStandard', 'General'),
    ('GridFrequency', '50'),
    ('OutputVoltageLevel', '230V'),
    ('GridVoltageHigh', '265'),
    ('GridVoltageLow', '185'),
    ('GridFrequencyHigh', '51.5'),
    ('GridFrequencyLow', '48'),
    ('ReconnectionTime', '60'),
    ('PowerFactorTarget', '1.0'),
    ('ActivePowerRegulation', '10.0'),
    ('ReactivePowerRegulation', '10.0'),
    ('ArcFaultDetectionEnabled', 'true'),
    ('IslandProtectionEnabled', 'true'),
    ('ParallelEnabled', 'false'),
    ('ParallelRole', 'standalone'),
    ('ParallelPhase', 'A'),
    ('ModbusAddress', '1'),
    ('ExternalCTRatio', '2000'),
    ('ExternalMeterCTRatio', '30'),
    ('LithiumBatteryLossFaultEnabled', 'true'),
    ('ExternalRelayFeatureEnabled', 'false'),
    ('RelayBitmap', '0'),
    ('ForceSmartLoadOn', 'false'),
    -- BatteryConfiguration
    ('BatteryCapacity', '200'),
    ('MaxBatteryChargeCurrent', '100'),
    ('MaxBatteryDischargeCurrent', '100'),
    -- ChargeSources
    ('GridChargingEnabled', 'true'),
    ('GridChargingStartSOC', '20'),
    ('GridChargingCurrent', '100'),
    ('GeneratorChargingEnabled', 'false'),
    ('GeneratorChargingStartSOC', '20'),
    ('GeneratorChargingCurrent', '100'),
    ('GeneratorMaxRunTime', '24'),
    ('GeneratorCoolingTime', '0'),
    -- ProtectionThresholds
    ('ShutdownSOC', '10'),
    ('LowBatteryWarning', '15'),
    ('RestartSOC', '50'),
    -- SystemWorkMode
    ('ExportStrategySelection', 'ZeroExportToCT'),
    ('EnergyPrioritySelection', 'BatteryFirst'),
    ('SolarSell', 'false'),
    ('MaxSolarSellPower', '12000'),
    ('MaxSellPower', '100'),
    ('ZeroExportLimit', '20'),
    ('ForceOffGrid', 'false'),
    ('GridPeakShavingEnabled', 'false'),
    ('GridPeakShavingLimitPower', '2000'),
    -- TimeOfUse
    ('TimeOfUseEnabled', 'false'),
    ('Monday', 'true'),
    ('Tuesday', 'true'),
    ('Wednesday', 'true'),
    ('Thursday', 'true'),
    ('Friday', 'true'),
    ('Saturday', 'true'),
    ('Sunday', 'true'),
    -- TimeSlot1
    ('TimeSlot1Start', '00:00'),
    ('TimeSlot1End', '04:00'),
    ('TimeSlot1GridCharge', 'false'),
    ('TimeSlot1GenCharge', 'false'),
    ('TimeSlot1MaxDischarge', '12000'),
    ('TimeSlot1MinBatterySOC', '80'),
    ('TimeSlot1MinBatteryVoltage', '48.0'),
    -- TimeSlot2
    ('TimeSlot2Start', '05:00'),
    ('TimeSlot2End', '09:00'),
    ('TimeSlot2GridCharge', 'false'),
    ('TimeSlot2GenCharge', 'false'),
    ('TimeSlot2MaxDischarge', '12000'),
    ('TimeSlot2MinBatterySOC', '80'),
    ('TimeSlot2MinBatteryVoltage', '48.0'),
    -- TimeSlot3
    ('TimeSlot3Start', '09:00'),
    ('TimeSlot3End', '13:00'),
    ('TimeSlot3GridCharge', 'false'),
    ('TimeSlot3GenCharge', 'false'),
    ('TimeSlot3MaxDischarge', '12000'),
    ('TimeSlot3MinBatterySOC', '80'),
    ('TimeSlot3MinBatteryVoltage', '48.0'),
    -- TimeSlot4
    ('TimeSlot4Start', '13:00'),
    ('TimeSlot4End', '17:00'),
    ('TimeSlot4GridCharge', 'false'),
    ('TimeSlot4GenCharge', 'false'),
    ('TimeSlot4MaxDischarge', '12000'),
    ('TimeSlot4MinBatterySOC', '80'),
    ('TimeSlot4MinBatteryVoltage', '48.0'),
    -- TimeSlot5
    ('TimeSlot5Start', '17:00'),
    ('TimeSlot5End', '21:00'),
    ('TimeSlot5GridCharge', 'false'),
    ('TimeSlot5GenCharge', 'false'),
    ('TimeSlot5MaxDischarge', '12000'),
    ('TimeSlot5MinBatterySOC', '80'),
    ('TimeSlot5MinBatteryVoltage', '48.0'),
    -- TimeSlot6
    ('TimeSlot6Start', '21:00'),
    ('TimeSlot6End', '24:00'),
    ('TimeSlot6GridCharge', 'false'),
    ('TimeSlot6GenCharge', 'false'),
    ('TimeSlot6MaxDischarge', '12000'),
    ('TimeSlot6MinBatterySOC', '80'),
    ('TimeSlot6MinBatteryVoltage', '48.0'),
    -- GeneratorSettings
    ('GeneratorCharge', 'false'),
    ('GeneratorGridCharge', 'true'),
    ('GeneratorStartSOC', '20'),
    ('GeneratorChargeCurrent', '40'),
    ('GeneratorPeakShavingEnabled', 'false'),
    ('GeneratorPeakShavingLimitPower', '2000'),
    -- SmartLoad
    ('SmartLoadEnabled', 'false'),
    ('SmartLoadOffSOC', '95'),
    ('SmartLoadOnSOC', '100'),
    ('SmartLoadPVPowerThreshold', '0'),
    ('SmartLoadOpenDelay', '100'),
    ('SmartLoadOnGridAlwaysOn', 'false');

-- Update Parameters.DefaultValue
UPDATE "Parameters" p
SET "DefaultValue" = tpv.default_value,
    "LastModified" = NOW(),
    "LastModifiedBy" = 'system'
FROM temp_param_values tpv
WHERE p."Name" = tpv.param_name;


-- =====================================================================
-- PART 4: ADD/UPDATE ParameterValueSettingSchemes
-- =====================================================================

-- Add missing schemes for single-phase-loftty
INSERT INTO "ParameterValueSettingSchemes" ("Id", "ParameterId", "ParameterGroupId", "Value", "SortOrder", "ModelNumber", "Created", "CreatedBy", "LastModified", "LastModifiedBy")
SELECT
    gen_random_uuid(),
    p."Id",
    NULL,
    tpv.default_value,
    0,
    'single-phase-loftty',
    NOW(),
    'system',
    NOW(),
    'system'
FROM "Parameters" p
JOIN temp_param_values tpv ON p."Name" = tpv.param_name
WHERE NOT EXISTS (
    SELECT 1 FROM "ParameterValueSettingSchemes" pvs
    WHERE pvs."ParameterId" = p."Id" AND pvs."ModelNumber" = 'single-phase-loftty'
);

-- Add missing schemes for three-phase-loftty
INSERT INTO "ParameterValueSettingSchemes" ("Id", "ParameterId", "ParameterGroupId", "Value", "SortOrder", "ModelNumber", "Created", "CreatedBy", "LastModified", "LastModifiedBy")
SELECT
    gen_random_uuid(),
    p."Id",
    NULL,
    tpv.default_value,
    0,
    'three-phase-loftty',
    NOW(),
    'system',
    NOW(),
    'system'
FROM "Parameters" p
JOIN temp_param_values tpv ON p."Name" = tpv.param_name
WHERE NOT EXISTS (
    SELECT 1 FROM "ParameterValueSettingSchemes" pvs
    WHERE pvs."ParameterId" = p."Id" AND pvs."ModelNumber" = 'three-phase-loftty'
);

-- Update existing ParameterValueSettingSchemes for single-phase-loftty
UPDATE "ParameterValueSettingSchemes" pvs
SET "Value" = tpv.default_value,
    "LastModified" = NOW(),
    "LastModifiedBy" = 'system'
FROM "Parameters" p
JOIN temp_param_values tpv ON p."Name" = tpv.param_name
WHERE pvs."ParameterId" = p."Id"
  AND pvs."ModelNumber" = 'single-phase-loftty';

-- Update existing ParameterValueSettingSchemes for three-phase-loftty
UPDATE "ParameterValueSettingSchemes" pvs
SET "Value" = tpv.default_value,
    "LastModified" = NOW(),
    "LastModifiedBy" = 'system'
FROM "Parameters" p
JOIN temp_param_values tpv ON p."Name" = tpv.param_name
WHERE pvs."ParameterId" = p."Id"
  AND pvs."ModelNumber" = 'three-phase-loftty';


-- =====================================================================
-- PART 5: ADD NEW PARAMETERS TO EXISTING DEVICES (if not exists)
-- =====================================================================

-- Add missing parameters to single-phase-loftty devices
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
WHERE pvs."ModelNumber" = 'single-phase-loftty'
  AND NOT EXISTS (
      SELECT 1 FROM "SetupDeviceParameterValues" sdpv
      WHERE sdpv."SetupDeviceId" = sd."Id"
        AND sdpv."ParameterId" = pvs."ParameterId"
  );

-- Add missing parameters to three-phase-loftty devices
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
WHERE pvs."ModelNumber" = 'three-phase-loftty'
  AND NOT EXISTS (
      SELECT 1 FROM "SetupDeviceParameterValues" sdpv
      WHERE sdpv."SetupDeviceId" = sd."Id"
        AND sdpv."ParameterId" = pvs."ParameterId"
  );


-- =====================================================================
-- PART 6: UPDATE EXISTING DEVICE VALUES (SetupDeviceParameterValues)
-- This is what users actually see in the app!
-- =====================================================================

-- Update existing SetupDeviceParameterValues to new defaults
UPDATE "SetupDeviceParameterValues" sdpv
SET "Value" = tpv.default_value,
    "LastModified" = NOW(),
    "LastModifiedBy" = 'system'
FROM "Parameters" p
JOIN temp_param_values tpv ON p."Name" = tpv.param_name
WHERE sdpv."ParameterId" = p."Id";

DROP TABLE temp_param_values;

COMMIT;


-- =====================================================================
-- VERIFICATION
-- =====================================================================

SELECT '=== PARAMETERS UPDATED ===' AS status;
SELECT p."Name", p."DefaultValue"
FROM "Parameters" p
WHERE p."Name" IN ('BatteryControlMode', 'GridVoltageHigh', 'GridChargingStartSOC', 'MaxSolarSellPower', 'MaxSellPower', 'ZeroExportLimit', 'TimeOfUseEnabled', 'TimeSlot1End', 'TimeSlot1MaxDischarge')
ORDER BY p."Name";

SELECT '=== PARAMETER VALUE SCHEMES (three-phase-loftty) ===' AS status;
SELECT p."Name", pvs."Value"
FROM "ParameterValueSettingSchemes" pvs
JOIN "Parameters" p ON pvs."ParameterId" = p."Id"
WHERE pvs."ModelNumber" = 'three-phase-loftty'
  AND p."Name" IN ('BatteryControlMode', 'GridVoltageHigh', 'GridChargingStartSOC', 'MaxSolarSellPower', 'MaxSellPower', 'ZeroExportLimit', 'TimeOfUseEnabled', 'TimeSlot1End', 'TimeSlot1MaxDischarge')
ORDER BY p."Name";

SELECT '=== SETUP DEVICE PARAMETER VALUES (sample) ===' AS status;
SELECT
    d."SerialNumber",
    p."Name",
    sdpv."Value"
FROM "SetupDeviceParameterValues" sdpv
JOIN "SetupDevices" sd ON sdpv."SetupDeviceId" = sd."Id"
JOIN "Devices" d ON sd."DeviceId" = d."Id"
JOIN "Parameters" p ON sdpv."ParameterId" = p."Id"
WHERE p."Name" IN ('MaxSolarSellPower', 'MaxSellPower', 'ZeroExportLimit', 'TimeSlot1End')
ORDER BY d."SerialNumber", p."Name"
LIMIT 20;

SELECT '=== SUMMARY ===' AS status;
SELECT 'Parameters' AS "Table", COUNT(*) AS "Updated" FROM "Parameters" p
JOIN (SELECT 'BatteryControlMode' AS n UNION SELECT 'GridVoltageHigh' UNION SELECT 'MaxSellPower') x ON p."Name" = x.n
UNION ALL
SELECT 'ParameterValueSettingSchemes', COUNT(*) FROM "ParameterValueSettingSchemes"
UNION ALL
SELECT 'SetupDeviceParameterValues', COUNT(*) FROM "SetupDeviceParameterValues";
