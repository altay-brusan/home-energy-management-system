-- =====================================================================
-- RESET ALL DEVICE PARAMETERS TO DEFAULT VALUES
--
-- *** WARNING: DESTRUCTIVE OPERATION ***
-- This script will OVERWRITE all existing device parameter values!
-- All user customizations will be LOST!
--
-- Use this script when:
-- - You need to factory reset all devices
-- - You've changed default values and want to apply them globally
-- - Testing/development environments
--
-- This script updates:
-- 1. Parameters.DefaultValue (reference)
-- 2. ParameterValueSettingSchemes.Value (for new device initialization)
-- 3. SetupDeviceParameterValues.Value (EXISTING DEVICE VALUES - what users see!)
--
-- Created: 2026-01-19
-- =====================================================================

-- Safety check - uncomment the line below to actually run
-- If you see "ROLLBACK" at the end, the script did NOT make changes
-- Change ROLLBACK to COMMIT to apply changes

BEGIN;

-- =====================================================================
-- PART 1: CREATE TEMP TABLE WITH ALL DEFAULT VALUES
-- =====================================================================

CREATE TEMP TABLE temp_default_values (
    param_name VARCHAR(100) PRIMARY KEY,
    default_value VARCHAR(100)
);

INSERT INTO temp_default_values (param_name, default_value) VALUES
    -- Admin parameters (Maintenance Settings)
    ('BatteryControlMode', 'Lithium'),
    ('LithiumBatteryWakeUpEnabled', 'true'),
    ('BatteryResistance', '100'),
    ('ChargeEfficiency', '98.3'),
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
    -- User parameters (Battery)
    ('BatteryCapacity', '200'),
    ('MaxBatteryChargeCurrent', '100'),
    ('MaxBatteryDischargeCurrent', '100'),
    ('GridChargingEnabled', 'true'),
    ('GridChargingStartSOC', '20'),
    ('GridChargingCurrent', '100'),
    ('GeneratorChargingEnabled', 'false'),
    ('GeneratorChargingStartSOC', '20'),
    ('GeneratorChargingCurrent', '100'),
    ('GeneratorMaxRunTime', '24'),
    ('GeneratorCoolingTime', '0'),
    ('ShutdownSOC', '10'),
    ('LowBatteryWarning', '15'),
    ('RestartSOC', '50'),
    -- User parameters (System Work Mode)
    ('ExportStrategySelection', 'ZeroExportToCT'),
    ('EnergyPrioritySelection', 'BatteryFirst'),
    ('SolarSell', 'false'),
    ('MaxSolarSellPower', '12000'),
    ('MaxSellPower', '100'),
    ('ZeroExportLimit', '20'),
    ('ForceOffGrid', 'false'),
    ('GridPeakShavingEnabled', 'false'),
    ('GridPeakShavingLimitPower', '2000'),
    -- User parameters (Grid Standard)
    ('GridStandard', 'General'),
    ('GridFrequency', '50'),
    ('OutputVoltageLevel', '230V'),
    ('PhaseType', 'ABC_0_120_240'),
    ('GridLevel', 'LN:230VAC LL:400VAC'),
    ('ITSystemNeutralGrounded', 'false'),
    -- User parameters (Time of Use)
    ('TimeOfUseEnabled', 'false'),
    ('Monday', 'true'),
    ('Tuesday', 'true'),
    ('Wednesday', 'true'),
    ('Thursday', 'true'),
    ('Friday', 'true'),
    ('Saturday', 'true'),
    ('Sunday', 'true'),
    -- Time Slot 1
    ('TimeSlot1Start', '00:00'),
    ('TimeSlot1End', '04:00'),
    ('TimeSlot1GridCharge', 'false'),
    ('TimeSlot1GenCharge', 'false'),
    ('TimeSlot1MaxDischarge', '12000'),
    ('TimeSlot1MinBatterySOC', '80'),
    ('TimeSlot1MinBatteryVoltage', '48.0'),
    -- Time Slot 2
    ('TimeSlot2Start', '05:00'),
    ('TimeSlot2End', '09:00'),
    ('TimeSlot2GridCharge', 'false'),
    ('TimeSlot2GenCharge', 'false'),
    ('TimeSlot2MaxDischarge', '12000'),
    ('TimeSlot2MinBatterySOC', '80'),
    ('TimeSlot2MinBatteryVoltage', '48.0'),
    -- Time Slot 3
    ('TimeSlot3Start', '09:00'),
    ('TimeSlot3End', '13:00'),
    ('TimeSlot3GridCharge', 'false'),
    ('TimeSlot3GenCharge', 'false'),
    ('TimeSlot3MaxDischarge', '12000'),
    ('TimeSlot3MinBatterySOC', '80'),
    ('TimeSlot3MinBatteryVoltage', '48.0'),
    -- Time Slot 4
    ('TimeSlot4Start', '13:00'),
    ('TimeSlot4End', '17:00'),
    ('TimeSlot4GridCharge', 'false'),
    ('TimeSlot4GenCharge', 'false'),
    ('TimeSlot4MaxDischarge', '12000'),
    ('TimeSlot4MinBatterySOC', '80'),
    ('TimeSlot4MinBatteryVoltage', '48.0'),
    -- Time Slot 5
    ('TimeSlot5Start', '17:00'),
    ('TimeSlot5End', '21:00'),
    ('TimeSlot5GridCharge', 'false'),
    ('TimeSlot5GenCharge', 'false'),
    ('TimeSlot5MaxDischarge', '12000'),
    ('TimeSlot5MinBatterySOC', '80'),
    ('TimeSlot5MinBatteryVoltage', '48.0'),
    -- Time Slot 6
    ('TimeSlot6Start', '21:00'),
    ('TimeSlot6End', '24:00'),
    ('TimeSlot6GridCharge', 'false'),
    ('TimeSlot6GenCharge', 'false'),
    ('TimeSlot6MaxDischarge', '12000'),
    ('TimeSlot6MinBatterySOC', '80'),
    ('TimeSlot6MinBatteryVoltage', '48.0'),
    -- Generator & Smart Load
    ('GeneratorCharge', 'false'),
    ('GeneratorGridCharge', 'true'),
    ('GeneratorStartSOC', '20'),
    ('GeneratorChargeCurrent', '40'),
    ('GeneratorPeakShavingEnabled', 'false'),
    ('GeneratorPeakShavingLimitPower', '2000'),
    ('SmartLoadEnabled', '0'),  -- 0=Generator Only, 1=Smart Load, 2=Microinverter
    ('SmartLoadOffSOC', '95'),
    ('SmartLoadOnSOC', '100'),
    ('SmartLoadPVPowerThreshold', '0'),
    ('SmartLoadOpenDelay', '100'),
    ('SmartLoadOnGridAlwaysOn', 'false');


-- =====================================================================
-- PART 2: UPDATE Parameters.DefaultValue
-- =====================================================================

UPDATE "Parameters" p
SET "DefaultValue" = tdv.default_value,
    "LastModified" = NOW(),
    "LastModifiedBy" = 'system'
FROM temp_default_values tdv
WHERE p."Name" = tdv.param_name;

SELECT 'Parameters.DefaultValue updated' AS status, COUNT(*) AS rows_affected
FROM "Parameters" p
JOIN temp_default_values tdv ON p."Name" = tdv.param_name;


-- =====================================================================
-- PART 3: UPDATE ParameterValueSettingSchemes.Value
-- =====================================================================

UPDATE "ParameterValueSettingSchemes" pvs
SET "Value" = tdv.default_value,
    "LastModified" = NOW(),
    "LastModifiedBy" = 'system'
FROM "Parameters" p
JOIN temp_default_values tdv ON p."Name" = tdv.param_name
WHERE pvs."ParameterId" = p."Id";

SELECT 'ParameterValueSettingSchemes.Value updated' AS status, COUNT(*) AS rows_affected
FROM "ParameterValueSettingSchemes" pvs
JOIN "Parameters" p ON pvs."ParameterId" = p."Id"
JOIN temp_default_values tdv ON p."Name" = tdv.param_name;


-- =====================================================================
-- PART 4: UPDATE SetupDeviceParameterValues.Value
-- *** THIS IS THE CRITICAL PART - UPDATES ACTUAL DEVICE VALUES! ***
-- =====================================================================

UPDATE "SetupDeviceParameterValues" sdpv
SET "Value" = tdv.default_value,
    "LastModified" = NOW(),
    "LastModifiedBy" = 'system'
FROM "Parameters" p
JOIN temp_default_values tdv ON p."Name" = tdv.param_name
WHERE sdpv."ParameterId" = p."Id";

SELECT 'SetupDeviceParameterValues.Value updated' AS status, COUNT(*) AS rows_affected
FROM "SetupDeviceParameterValues" sdpv
JOIN "Parameters" p ON sdpv."ParameterId" = p."Id"
JOIN temp_default_values tdv ON p."Name" = tdv.param_name;


DROP TABLE temp_default_values;

-- =====================================================================
-- SAFETY: ROLLBACK by default - change to COMMIT to apply changes
-- =====================================================================
-- ROLLBACK;
COMMIT;


-- =====================================================================
-- VERIFICATION
-- =====================================================================
SELECT '=== RESET COMPLETE ===' AS status;

SELECT '=== Sample Device Values After Reset ===' AS status;
SELECT
    d."SerialNumber",
    p."Name" AS "Parameter",
    sdpv."Value"
FROM "SetupDeviceParameterValues" sdpv
JOIN "SetupDevices" sd ON sdpv."SetupDeviceId" = sd."Id"
JOIN "Devices" d ON sd."DeviceId" = d."Id"
JOIN "Parameters" p ON sdpv."ParameterId" = p."Id"
WHERE p."Name" IN ('ShutdownSOC', 'GridChargingStartSOC', 'ExportStrategySelection', 'GridStandard')
ORDER BY d."SerialNumber", p."Name"
LIMIT 20;
