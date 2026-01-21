-- =====================================================================
-- ADD MISSING PARAMETERS TO EXISTING DEVICES
-- SAFE - Only adds missing data, NEVER updates existing values
--
-- Use this script when:
-- - You've added new parameters to seed_parameter_group_schema.sql
-- - You want to add those new parameters to existing devices
-- - You want to preserve all existing user customizations
--
-- This script is IDEMPOTENT - safe to run multiple times
--
-- Created: 2026-01-19
-- =====================================================================

BEGIN;

-- =====================================================================
-- PART 1: ADD MISSING PARAMETER GROUPS
-- (Same as seed script - ensures groups exist)
-- =====================================================================

-- Level 1 groups
INSERT INTO "ParameterGroups" ("Id", "ParentId", "Name", "Description", "Version", "SortOrder", "Created", "CreatedBy", "LastModified", "LastModifiedBy")
SELECT gen_random_uuid(), NULL, g.name, g.description, '1.0', g.sort_order, NOW(), 'system', NOW(), 'system'
FROM (VALUES
    ('BatterySettings', 'Battery Settings - Charge sources, thresholds, protection', 1),
    ('SystemWorkMode', 'System Work Mode - Export strategy, energy priority', 2),
    ('TimeOfUse', 'Time of Use - 6 time slots, schedules', 3),
    ('GeneratorAndSmartLoad', 'Generator & Smart Load - Gen charging, peak shaving, smart load', 4),
    ('MaintenanceSettings', 'Maintenance Settings - Admin-level configuration', 5)
) AS g(name, description, sort_order)
WHERE NOT EXISTS (SELECT 1 FROM "ParameterGroups" WHERE "Name" = g.name AND "ParentId" IS NULL);

-- Level 2 groups - BatterySettings
INSERT INTO "ParameterGroups" ("Id", "ParentId", "Name", "Description", "Version", "SortOrder", "Created", "CreatedBy", "LastModified", "LastModifiedBy")
SELECT gen_random_uuid(), p."Id", g.name, g.description, '1.0', g.sort_order, NOW(), 'system', NOW(), 'system'
FROM "ParameterGroups" p
CROSS JOIN (VALUES
    ('BatteryConfiguration', 'Battery Configuration - Capacity and current limits', 0),
    ('ChargeSources', 'Charge Sources', 1),
    ('ProtectionThresholds', 'Protection Thresholds', 2)
) AS g(name, description, sort_order)
WHERE p."Name" = 'BatterySettings' AND p."ParentId" IS NULL
  AND NOT EXISTS (SELECT 1 FROM "ParameterGroups" WHERE "Name" = g.name AND "ParentId" = p."Id");

-- Level 2 groups - SystemWorkMode
INSERT INTO "ParameterGroups" ("Id", "ParentId", "Name", "Description", "Version", "SortOrder", "Created", "CreatedBy", "LastModified", "LastModifiedBy")
SELECT gen_random_uuid(), p."Id", g.name, g.description, '1.0', g.sort_order, NOW(), 'system', NOW(), 'system'
FROM "ParameterGroups" p
CROSS JOIN (VALUES
    ('ExportStrategy', 'Export Strategy', 1),
    ('EnergyPriority', 'Energy Priority', 2),
    ('InverterMode', 'Inverter Mode', 3),
    ('GridPeakShaving', 'Grid Peak Shaving', 4),
    ('GridStandard', 'Grid Standard Selection (User-level)', 5)
) AS g(name, description, sort_order)
WHERE p."Name" = 'SystemWorkMode' AND p."ParentId" IS NULL
  AND NOT EXISTS (SELECT 1 FROM "ParameterGroups" WHERE "Name" = g.name AND "ParentId" = p."Id");

-- Level 2 groups - TimeOfUse
INSERT INTO "ParameterGroups" ("Id", "ParentId", "Name", "Description", "Version", "SortOrder", "Created", "CreatedBy", "LastModified", "LastModifiedBy")
SELECT gen_random_uuid(), p."Id", g.name, g.description, '1.0', g.sort_order, NOW(), 'system', NOW(), 'system'
FROM "ParameterGroups" p
CROSS JOIN (VALUES
    ('ActiveDays', 'Active Days', 1),
    ('TimeSlot1', 'Time Slot 1', 2), ('TimeSlot2', 'Time Slot 2', 3), ('TimeSlot3', 'Time Slot 3', 4),
    ('TimeSlot4', 'Time Slot 4', 5), ('TimeSlot5', 'Time Slot 5', 6), ('TimeSlot6', 'Time Slot 6', 7)
) AS g(name, description, sort_order)
WHERE p."Name" = 'TimeOfUse' AND p."ParentId" IS NULL
  AND NOT EXISTS (SELECT 1 FROM "ParameterGroups" WHERE "Name" = g.name AND "ParentId" = p."Id");

-- Level 2 groups - GeneratorAndSmartLoad
INSERT INTO "ParameterGroups" ("Id", "ParentId", "Name", "Description", "Version", "SortOrder", "Created", "CreatedBy", "LastModified", "LastModifiedBy")
SELECT gen_random_uuid(), p."Id", g.name, g.description, '1.0', g.sort_order, NOW(), 'system', NOW(), 'system'
FROM "ParameterGroups" p
CROSS JOIN (VALUES
    ('GeneratorSettings', 'Generator Settings', 1),
    ('GeneratorPeakShaving', 'Generator Peak Shaving', 2),
    ('SmartLoad', 'Smart Load', 3)
) AS g(name, description, sort_order)
WHERE p."Name" = 'GeneratorAndSmartLoad' AND p."ParentId" IS NULL
  AND NOT EXISTS (SELECT 1 FROM "ParameterGroups" WHERE "Name" = g.name AND "ParentId" = p."Id");

-- Level 2 groups - MaintenanceSettings
INSERT INTO "ParameterGroups" ("Id", "ParentId", "Name", "Description", "Version", "SortOrder", "Created", "CreatedBy", "LastModified", "LastModifiedBy")
SELECT gen_random_uuid(), p."Id", g.name, g.description, '1.0', g.sort_order, NOW(), 'system', NOW(), 'system'
FROM "ParameterGroups" p
CROSS JOIN (VALUES
    ('BatteryMaintenance', 'Battery Maintenance Configuration', 1),
    ('GridStandardDetails', 'Grid Protection and Power Factor Settings (Admin-level)', 2),
    ('AdvancedFunctions', 'Advanced Inverter Functions', 3),
    ('ExternalRelayConfiguration', 'External Relay Settings', 4),
    ('GeneratorOverride', 'Generator and Smart Load Override', 5)
) AS g(name, description, sort_order)
WHERE p."Name" = 'MaintenanceSettings' AND p."ParentId" IS NULL
  AND NOT EXISTS (SELECT 1 FROM "ParameterGroups" WHERE "Name" = g.name AND "ParentId" = p."Id");

-- Level 3 groups
INSERT INTO "ParameterGroups" ("Id", "ParentId", "Name", "Description", "Version", "SortOrder", "Created", "CreatedBy", "LastModified", "LastModifiedBy")
SELECT gen_random_uuid(), p."Id", g.name, g.description, '1.0', g.sort_order, NOW(), 'system', NOW(), 'system'
FROM "ParameterGroups" p
CROSS JOIN (VALUES
    ('ChargeSources', 'GridCharging', 'Grid Charging Settings', 1),
    ('ChargeSources', 'GeneratorCharging', 'Generator Charging Settings', 2),
    ('GridStandardDetails', 'ProtectionParameters', 'Grid Protection Thresholds', 1),
    ('GridStandardDetails', 'PowerFactorSettings', 'Power Factor and Regulation', 2),
    ('AdvancedFunctions', 'ArcFaultProtection', 'Arc Fault Detection Settings', 1),
    ('AdvancedFunctions', 'IslandProtection', 'Island Protection Settings', 2),
    ('AdvancedFunctions', 'ParallelSettings', 'Parallel Operation Configuration', 3),
    ('AdvancedFunctions', 'CTSettings', 'Current Transformer Settings', 4)
) AS g(parent_name, name, description, sort_order)
WHERE p."Name" = g.parent_name
  AND NOT EXISTS (SELECT 1 FROM "ParameterGroups" WHERE "Name" = g.name AND "ParentId" = p."Id");


-- =====================================================================
-- PART 2: ADD MISSING PARAMETERS
-- =====================================================================

-- Admin parameters
INSERT INTO "Parameters" ("Id", "ParameterGroupId", "Name", "Description", "ElementType", "RoleType", "Unit", "DefaultValue", "SortOrder", "Created", "CreatedBy", "LastModified", "LastModifiedBy")
SELECT gen_random_uuid(), pg."Id", p.name, p.description, p.element_type, 'Admin', p.unit, p.default_value, p.sort_order, NOW(), 'system', NOW(), 'system'
FROM "ParameterGroups" pg
CROSS JOIN (VALUES
    ('BatteryMaintenance', 'BatteryControlMode', 'Battery Control Mode (Voltage/Capacity/No Battery)', 'Text', '', 'Lithium', 1),
    ('BatteryMaintenance', 'LithiumBatteryWakeUpEnabled', 'Lithium Battery Wake-Up Enable', 'Boolean', '', 'true', 2),
    ('BatteryMaintenance', 'BatteryResistance', 'Battery Internal Resistance', 'Decimal', 'mOhm', '100', 3),
    ('BatteryMaintenance', 'ChargeEfficiency', 'Battery Charge Efficiency', 'Decimal', '%', '98.3', 4),
    ('ProtectionParameters', 'GridVoltageHigh', 'Grid High Voltage Protection Point', 'Decimal', 'V', '265', 1),
    ('ProtectionParameters', 'GridVoltageLow', 'Grid Low Voltage Protection Point', 'Decimal', 'V', '185', 2),
    ('ProtectionParameters', 'GridFrequencyHigh', 'Grid High Frequency Protection Point', 'Decimal', 'Hz', '51.5', 3),
    ('ProtectionParameters', 'GridFrequencyLow', 'Grid Low Frequency Protection Point', 'Decimal', 'Hz', '48', 4),
    ('ProtectionParameters', 'ReconnectionTime', 'Grid Reconnection Time', 'Decimal', 's', '60', 5),
    ('PowerFactorSettings', 'PowerFactorTarget', 'Output Power Factor Target', 'Decimal', '', '1.0', 1),
    ('PowerFactorSettings', 'ActivePowerRegulation', 'Active Power Regulation Step', 'Decimal', '%', '10.0', 2),
    ('PowerFactorSettings', 'ReactivePowerRegulation', 'Reactive Power Regulation Step', 'Decimal', '%', '10.0', 3),
    ('ArcFaultProtection', 'ArcFaultDetectionEnabled', 'Solar Arc Fault Detection Enable', 'Boolean', '', 'true', 1),
    ('IslandProtection', 'IslandProtectionEnabled', 'Island Protection Enable', 'Boolean', '', 'true', 1),
    ('ParallelSettings', 'ParallelEnabled', 'Parallel Operation Enable', 'Boolean', '', 'false', 1),
    ('ParallelSettings', 'ParallelRole', 'Parallel Role (Master/Slave/Standalone)', 'Text', '', 'standalone', 2),
    ('ParallelSettings', 'ParallelPhase', 'Parallel Phase Assignment (A/B/C)', 'Text', '', 'A', 3),
    ('ParallelSettings', 'ModbusAddress', 'Modbus Address (0-63)', 'Decimal', '', '1', 4),
    ('CTSettings', 'ExternalCTRatio', 'External CT Ratio', 'Decimal', '', '2000', 1),
    ('CTSettings', 'ExternalMeterCTRatio', 'External Meter CT Ratio', 'Decimal', '', '30', 2),
    ('AdvancedFunctions', 'LithiumBatteryLossFaultEnabled', 'Lithium Battery Loss Fault Enable', 'Boolean', '', 'true', 1),
    ('ExternalRelayConfiguration', 'ExternalRelayFeatureEnabled', 'External Relay Feature Enable', 'Boolean', '', 'false', 1),
    ('ExternalRelayConfiguration', 'RelayBitmap', 'External Relay Bitmap (0-65535)', 'Decimal', '', '0', 2),
    ('GeneratorOverride', 'ForceSmartLoadOn', 'Force Smart Load On (Override)', 'Boolean', '', 'false', 1)
) AS p(group_name, name, description, element_type, unit, default_value, sort_order)
WHERE pg."Name" = p.group_name
  AND NOT EXISTS (SELECT 1 FROM "Parameters" WHERE "Name" = p.name);

-- User parameters (condensed - all user params)
INSERT INTO "Parameters" ("Id", "ParameterGroupId", "Name", "Description", "ElementType", "RoleType", "Unit", "DefaultValue", "SortOrder", "Created", "CreatedBy", "LastModified", "LastModifiedBy")
SELECT gen_random_uuid(), pg."Id", p.name, p.description, p.element_type, 'User', p.unit, p.default_value, p.sort_order, NOW(), 'system', NOW(), 'system'
FROM "ParameterGroups" pg
CROSS JOIN (VALUES
    ('BatteryConfiguration', 'BatteryCapacity', 'Battery Capacity', 'Decimal', 'Ah', '200', 1),
    ('BatteryConfiguration', 'MaxBatteryChargeCurrent', 'Max Battery Charge Current', 'Decimal', 'A', '100', 2),
    ('BatteryConfiguration', 'MaxBatteryDischargeCurrent', 'Max Battery Discharge Current', 'Decimal', 'A', '100', 3),
    ('GridCharging', 'GridChargingEnabled', 'Enable/Disable Grid Charging', 'Boolean', '', 'true', 1),
    ('GridCharging', 'GridChargingStartSOC', 'Grid Charging Start State of Charge', 'Decimal', '%', '20', 2),
    ('GridCharging', 'GridChargingCurrent', 'Grid Charging Current', 'Decimal', 'A', '100', 3),
    ('GeneratorCharging', 'GeneratorChargingEnabled', 'Enable/Disable Generator Charging', 'Boolean', '', 'false', 1),
    ('GeneratorCharging', 'GeneratorChargingStartSOC', 'Generator Charging Start State of Charge', 'Decimal', '%', '20', 2),
    ('GeneratorCharging', 'GeneratorChargingCurrent', 'Generator Charging Current', 'Decimal', 'A', '100', 3),
    ('GeneratorCharging', 'GeneratorMaxRunTime', 'Generator Maximum Run Time', 'Decimal', 'hrs', '24', 4),
    ('GeneratorCharging', 'GeneratorCoolingTime', 'Generator Cooling Time', 'Decimal', 'hrs', '0', 5),
    ('ProtectionThresholds', 'ShutdownSOC', 'Shutdown SOC (inverter off)', 'Decimal', '%', '10', 1),
    ('ProtectionThresholds', 'LowBatteryWarning', 'Low Battery Warning (alarm)', 'Decimal', '%', '15', 2),
    ('ProtectionThresholds', 'RestartSOC', 'Restart SOC (resume output)', 'Decimal', '%', '50', 3),
    ('ExportStrategy', 'ExportStrategySelection', 'Export Strategy Selection', 'Text', '', 'ZeroExportToCT', 1),
    ('EnergyPriority', 'EnergyPrioritySelection', 'Energy Priority Selection', 'Text', '', 'BatteryFirst', 1),
    ('InverterMode', 'SolarSell', 'Solar Sell Enable/Disable', 'Boolean', '', 'false', 1),
    ('InverterMode', 'MaxSolarSellPower', 'Maximum Solar Sell Power', 'Decimal', 'W', '12000', 2),
    ('InverterMode', 'MaxSellPower', 'Maximum Sell Power', 'Decimal', 'W', '100', 3),
    ('InverterMode', 'ZeroExportLimit', 'Zero Export Limit', 'Decimal', 'W', '20', 4),
    ('InverterMode', 'ForceOffGrid', 'Force Off-Grid Mode', 'Boolean', '', 'false', 5),
    ('GridPeakShaving', 'GridPeakShavingEnabled', 'Enable/Disable Grid Peak Shaving', 'Boolean', '', 'false', 1),
    ('GridPeakShaving', 'GridPeakShavingLimitPower', 'Grid Peak Shaving Limit Power', 'Decimal', 'W', '2000', 2),
    ('GridStandard', 'GridStandard', 'Grid Standard', 'Text', '', 'General', 1),
    ('GridStandard', 'GridFrequency', 'Grid Frequency (50Hz or 60Hz)', 'Text', 'Hz', '50', 2),
    ('GridStandard', 'OutputVoltageLevel', 'Output Voltage Level', 'Text', '', '230V', 3),
    ('GridStandard', 'PhaseType', 'Grid Phase Sequence (ABC/ACB)', 'Text', '', 'ABC_0_120_240', 4),
    ('GridStandard', 'GridLevel', 'Grid Level (LN/LL Voltage)', 'Text', '', 'LN:230VAC LL:400VAC', 5),
    ('GridStandard', 'ITSystemNeutralGrounded', 'IT System Neutral Grounded', 'Boolean', '', 'false', 6),
    ('TimeOfUse', 'TimeOfUseEnabled', 'Enable/Disable Time of Use', 'Boolean', '', 'false', 1),
    ('ActiveDays', 'Monday', 'Active on Monday', 'Boolean', '', 'true', 1),
    ('ActiveDays', 'Tuesday', 'Active on Tuesday', 'Boolean', '', 'true', 2),
    ('ActiveDays', 'Wednesday', 'Active on Wednesday', 'Boolean', '', 'true', 3),
    ('ActiveDays', 'Thursday', 'Active on Thursday', 'Boolean', '', 'true', 4),
    ('ActiveDays', 'Friday', 'Active on Friday', 'Boolean', '', 'true', 5),
    ('ActiveDays', 'Saturday', 'Active on Saturday', 'Boolean', '', 'true', 6),
    ('ActiveDays', 'Sunday', 'Active on Sunday', 'Boolean', '', 'true', 7),
    ('TimeSlot1', 'TimeSlot1Start', 'Time Slot 1 Start Time', 'Text', '', '00:00', 1),
    ('TimeSlot1', 'TimeSlot1End', 'Time Slot 1 End Time', 'Text', '', '04:00', 2),
    ('TimeSlot1', 'TimeSlot1GridCharge', 'Time Slot 1 Grid Charge', 'Boolean', '', 'false', 3),
    ('TimeSlot1', 'TimeSlot1GenCharge', 'Time Slot 1 Generator Charge', 'Boolean', '', 'false', 4),
    ('TimeSlot1', 'TimeSlot1MaxDischarge', 'Time Slot 1 Maximum Discharge', 'Decimal', 'W', '12000', 5),
    ('TimeSlot1', 'TimeSlot1MinBatterySOC', 'Time Slot 1 Minimum Battery SOC', 'Decimal', '%', '80', 6),
    ('TimeSlot1', 'TimeSlot1MinBatteryVoltage', 'Time Slot 1 Minimum Battery Voltage', 'Decimal', 'V', '48.0', 7),
    ('TimeSlot2', 'TimeSlot2Start', 'Time Slot 2 Start Time', 'Text', '', '05:00', 1),
    ('TimeSlot2', 'TimeSlot2End', 'Time Slot 2 End Time', 'Text', '', '09:00', 2),
    ('TimeSlot2', 'TimeSlot2GridCharge', 'Time Slot 2 Grid Charge', 'Boolean', '', 'false', 3),
    ('TimeSlot2', 'TimeSlot2GenCharge', 'Time Slot 2 Generator Charge', 'Boolean', '', 'false', 4),
    ('TimeSlot2', 'TimeSlot2MaxDischarge', 'Time Slot 2 Maximum Discharge', 'Decimal', 'W', '12000', 5),
    ('TimeSlot2', 'TimeSlot2MinBatterySOC', 'Time Slot 2 Minimum Battery SOC', 'Decimal', '%', '80', 6),
    ('TimeSlot2', 'TimeSlot2MinBatteryVoltage', 'Time Slot 2 Minimum Battery Voltage', 'Decimal', 'V', '48.0', 7),
    ('TimeSlot3', 'TimeSlot3Start', 'Time Slot 3 Start Time', 'Text', '', '09:00', 1),
    ('TimeSlot3', 'TimeSlot3End', 'Time Slot 3 End Time', 'Text', '', '13:00', 2),
    ('TimeSlot3', 'TimeSlot3GridCharge', 'Time Slot 3 Grid Charge', 'Boolean', '', 'false', 3),
    ('TimeSlot3', 'TimeSlot3GenCharge', 'Time Slot 3 Generator Charge', 'Boolean', '', 'false', 4),
    ('TimeSlot3', 'TimeSlot3MaxDischarge', 'Time Slot 3 Maximum Discharge', 'Decimal', 'W', '12000', 5),
    ('TimeSlot3', 'TimeSlot3MinBatterySOC', 'Time Slot 3 Minimum Battery SOC', 'Decimal', '%', '80', 6),
    ('TimeSlot3', 'TimeSlot3MinBatteryVoltage', 'Time Slot 3 Minimum Battery Voltage', 'Decimal', 'V', '48.0', 7),
    ('TimeSlot4', 'TimeSlot4Start', 'Time Slot 4 Start Time', 'Text', '', '13:00', 1),
    ('TimeSlot4', 'TimeSlot4End', 'Time Slot 4 End Time', 'Text', '', '17:00', 2),
    ('TimeSlot4', 'TimeSlot4GridCharge', 'Time Slot 4 Grid Charge', 'Boolean', '', 'false', 3),
    ('TimeSlot4', 'TimeSlot4GenCharge', 'Time Slot 4 Generator Charge', 'Boolean', '', 'false', 4),
    ('TimeSlot4', 'TimeSlot4MaxDischarge', 'Time Slot 4 Maximum Discharge', 'Decimal', 'W', '12000', 5),
    ('TimeSlot4', 'TimeSlot4MinBatterySOC', 'Time Slot 4 Minimum Battery SOC', 'Decimal', '%', '80', 6),
    ('TimeSlot4', 'TimeSlot4MinBatteryVoltage', 'Time Slot 4 Minimum Battery Voltage', 'Decimal', 'V', '48.0', 7),
    ('TimeSlot5', 'TimeSlot5Start', 'Time Slot 5 Start Time', 'Text', '', '17:00', 1),
    ('TimeSlot5', 'TimeSlot5End', 'Time Slot 5 End Time', 'Text', '', '21:00', 2),
    ('TimeSlot5', 'TimeSlot5GridCharge', 'Time Slot 5 Grid Charge', 'Boolean', '', 'false', 3),
    ('TimeSlot5', 'TimeSlot5GenCharge', 'Time Slot 5 Generator Charge', 'Boolean', '', 'false', 4),
    ('TimeSlot5', 'TimeSlot5MaxDischarge', 'Time Slot 5 Maximum Discharge', 'Decimal', 'W', '12000', 5),
    ('TimeSlot5', 'TimeSlot5MinBatterySOC', 'Time Slot 5 Minimum Battery SOC', 'Decimal', '%', '80', 6),
    ('TimeSlot5', 'TimeSlot5MinBatteryVoltage', 'Time Slot 5 Minimum Battery Voltage', 'Decimal', 'V', '48.0', 7),
    ('TimeSlot6', 'TimeSlot6Start', 'Time Slot 6 Start Time', 'Text', '', '21:00', 1),
    ('TimeSlot6', 'TimeSlot6End', 'Time Slot 6 End Time', 'Text', '', '24:00', 2),
    ('TimeSlot6', 'TimeSlot6GridCharge', 'Time Slot 6 Grid Charge', 'Boolean', '', 'false', 3),
    ('TimeSlot6', 'TimeSlot6GenCharge', 'Time Slot 6 Generator Charge', 'Boolean', '', 'false', 4),
    ('TimeSlot6', 'TimeSlot6MaxDischarge', 'Time Slot 6 Maximum Discharge', 'Decimal', 'W', '12000', 5),
    ('TimeSlot6', 'TimeSlot6MinBatterySOC', 'Time Slot 6 Minimum Battery SOC', 'Decimal', '%', '80', 6),
    ('TimeSlot6', 'TimeSlot6MinBatteryVoltage', 'Time Slot 6 Minimum Battery Voltage', 'Decimal', 'V', '48.0', 7),
    ('GeneratorSettings', 'GeneratorCharge', 'Generator Charge Enable/Disable', 'Boolean', '', 'false', 1),
    ('GeneratorSettings', 'GeneratorGridCharge', 'Generator Grid Charge Enable/Disable', 'Boolean', '', 'true', 2),
    ('GeneratorSettings', 'GeneratorStartSOC', 'Generator Start State of Charge', 'Decimal', '%', '20', 3),
    ('GeneratorSettings', 'GeneratorChargeCurrent', 'Generator Charge Current', 'Decimal', 'A', '40', 4),
    ('GeneratorPeakShaving', 'GeneratorPeakShavingEnabled', 'Enable/Disable Generator Peak Shaving', 'Boolean', '', 'false', 1),
    ('GeneratorPeakShaving', 'GeneratorPeakShavingLimitPower', 'Generator Peak Shaving Limit Power', 'Decimal', 'W', '2000', 2),
    ('SmartLoad', 'SmartLoadEnabled', 'Generator port connection mode', 'Decimal', '', '0', 1),
    ('SmartLoad', 'SmartLoadOffSOC', 'Smart Load OFF SOC (turn off below)', 'Decimal', '%', '95', 2),
    ('SmartLoad', 'SmartLoadOnSOC', 'Smart Load ON SOC (turn on above)', 'Decimal', '%', '100', 3),
    ('SmartLoad', 'SmartLoadPVPowerThreshold', 'Smart Load PV Power Threshold', 'Decimal', 'W', '0', 4),
    ('SmartLoad', 'SmartLoadOpenDelay', 'Smart Load Open Delay', 'Decimal', 'min', '100', 5),
    ('SmartLoad', 'SmartLoadOnGridAlwaysOn', 'Smart Load On Grid Always On', 'Boolean', '', 'false', 6)
) AS p(group_name, name, description, element_type, unit, default_value, sort_order)
WHERE pg."Name" = p.group_name
  AND NOT EXISTS (SELECT 1 FROM "Parameters" WHERE "Name" = p.name);


-- =====================================================================
-- PART 3: ADD MISSING PARAMETER VALUE SETTING SCHEMES
-- =====================================================================

-- Create temp table with all default values
CREATE TEMP TABLE temp_defaults (
    param_name VARCHAR(100) PRIMARY KEY,
    default_value VARCHAR(100)
);

INSERT INTO temp_defaults (param_name, default_value) VALUES
    ('BatteryControlMode', 'Lithium'), ('LithiumBatteryWakeUpEnabled', 'true'),
    ('BatteryResistance', '100'), ('ChargeEfficiency', '98.3'),
    ('GridVoltageHigh', '265'), ('GridVoltageLow', '185'),
    ('GridFrequencyHigh', '51.5'), ('GridFrequencyLow', '48'), ('ReconnectionTime', '60'),
    ('PowerFactorTarget', '1.0'), ('ActivePowerRegulation', '10.0'), ('ReactivePowerRegulation', '10.0'),
    ('ArcFaultDetectionEnabled', 'true'), ('IslandProtectionEnabled', 'true'),
    ('ParallelEnabled', 'false'), ('ParallelRole', 'standalone'), ('ParallelPhase', 'A'), ('ModbusAddress', '1'),
    ('ExternalCTRatio', '2000'), ('ExternalMeterCTRatio', '30'),
    ('LithiumBatteryLossFaultEnabled', 'true'), ('ExternalRelayFeatureEnabled', 'false'),
    ('RelayBitmap', '0'), ('ForceSmartLoadOn', 'false'),
    ('BatteryCapacity', '200'), ('MaxBatteryChargeCurrent', '100'), ('MaxBatteryDischargeCurrent', '100'),
    ('GridChargingEnabled', 'true'), ('GridChargingStartSOC', '20'), ('GridChargingCurrent', '100'),
    ('GeneratorChargingEnabled', 'false'), ('GeneratorChargingStartSOC', '20'), ('GeneratorChargingCurrent', '100'),
    ('GeneratorMaxRunTime', '24'), ('GeneratorCoolingTime', '0'),
    ('ShutdownSOC', '10'), ('LowBatteryWarning', '15'), ('RestartSOC', '50'),
    ('ExportStrategySelection', 'ZeroExportToCT'), ('EnergyPrioritySelection', 'BatteryFirst'),
    ('SolarSell', 'false'), ('MaxSolarSellPower', '12000'), ('MaxSellPower', '100'),
    ('ZeroExportLimit', '20'), ('ForceOffGrid', 'false'),
    ('GridPeakShavingEnabled', 'false'), ('GridPeakShavingLimitPower', '2000'),
    ('GridStandard', 'General'), ('GridFrequency', '50'), ('OutputVoltageLevel', '230V'),
    ('PhaseType', 'ABC_0_120_240'), ('GridLevel', 'LN:230VAC LL:400VAC'), ('ITSystemNeutralGrounded', 'false'),
    ('TimeOfUseEnabled', 'false'),
    ('Monday', 'true'), ('Tuesday', 'true'), ('Wednesday', 'true'), ('Thursday', 'true'),
    ('Friday', 'true'), ('Saturday', 'true'), ('Sunday', 'true'),
    ('TimeSlot1Start', '00:00'), ('TimeSlot1End', '04:00'), ('TimeSlot1GridCharge', 'false'),
    ('TimeSlot1GenCharge', 'false'), ('TimeSlot1MaxDischarge', '12000'), ('TimeSlot1MinBatterySOC', '80'), ('TimeSlot1MinBatteryVoltage', '48.0'),
    ('TimeSlot2Start', '05:00'), ('TimeSlot2End', '09:00'), ('TimeSlot2GridCharge', 'false'),
    ('TimeSlot2GenCharge', 'false'), ('TimeSlot2MaxDischarge', '12000'), ('TimeSlot2MinBatterySOC', '80'), ('TimeSlot2MinBatteryVoltage', '48.0'),
    ('TimeSlot3Start', '09:00'), ('TimeSlot3End', '13:00'), ('TimeSlot3GridCharge', 'false'),
    ('TimeSlot3GenCharge', 'false'), ('TimeSlot3MaxDischarge', '12000'), ('TimeSlot3MinBatterySOC', '80'), ('TimeSlot3MinBatteryVoltage', '48.0'),
    ('TimeSlot4Start', '13:00'), ('TimeSlot4End', '17:00'), ('TimeSlot4GridCharge', 'false'),
    ('TimeSlot4GenCharge', 'false'), ('TimeSlot4MaxDischarge', '12000'), ('TimeSlot4MinBatterySOC', '80'), ('TimeSlot4MinBatteryVoltage', '48.0'),
    ('TimeSlot5Start', '17:00'), ('TimeSlot5End', '21:00'), ('TimeSlot5GridCharge', 'false'),
    ('TimeSlot5GenCharge', 'false'), ('TimeSlot5MaxDischarge', '12000'), ('TimeSlot5MinBatterySOC', '80'), ('TimeSlot5MinBatteryVoltage', '48.0'),
    ('TimeSlot6Start', '21:00'), ('TimeSlot6End', '24:00'), ('TimeSlot6GridCharge', 'false'),
    ('TimeSlot6GenCharge', 'false'), ('TimeSlot6MaxDischarge', '12000'), ('TimeSlot6MinBatterySOC', '80'), ('TimeSlot6MinBatteryVoltage', '48.0'),
    ('GeneratorCharge', 'false'), ('GeneratorGridCharge', 'true'), ('GeneratorStartSOC', '20'), ('GeneratorChargeCurrent', '40'),
    ('GeneratorPeakShavingEnabled', 'false'), ('GeneratorPeakShavingLimitPower', '2000'),
    ('SmartLoadEnabled', '0'), ('SmartLoadOffSOC', '95'), ('SmartLoadOnSOC', '100'),
    ('SmartLoadPVPowerThreshold', '0'), ('SmartLoadOpenDelay', '100'), ('SmartLoadOnGridAlwaysOn', 'false');

-- Add missing schemes for single-phase-loftty
INSERT INTO "ParameterValueSettingSchemes" ("Id", "ParameterId", "ParameterGroupId", "Value", "SortOrder", "ModelNumber", "Created", "CreatedBy", "LastModified", "LastModifiedBy")
SELECT gen_random_uuid(), p."Id", NULL, td.default_value, 0, 'single-phase-loftty', NOW(), 'system', NOW(), 'system'
FROM "Parameters" p
JOIN temp_defaults td ON p."Name" = td.param_name
WHERE NOT EXISTS (
    SELECT 1 FROM "ParameterValueSettingSchemes" pvs
    WHERE pvs."ParameterId" = p."Id" AND pvs."ModelNumber" = 'single-phase-loftty'
);

-- Add missing schemes for three-phase-loftty
INSERT INTO "ParameterValueSettingSchemes" ("Id", "ParameterId", "ParameterGroupId", "Value", "SortOrder", "ModelNumber", "Created", "CreatedBy", "LastModified", "LastModifiedBy")
SELECT gen_random_uuid(), p."Id", NULL, td.default_value, 0, 'three-phase-loftty', NOW(), 'system', NOW(), 'system'
FROM "Parameters" p
JOIN temp_defaults td ON p."Name" = td.param_name
WHERE NOT EXISTS (
    SELECT 1 FROM "ParameterValueSettingSchemes" pvs
    WHERE pvs."ParameterId" = p."Id" AND pvs."ModelNumber" = 'three-phase-loftty'
);


-- =====================================================================
-- PART 4: ADD MISSING PARAMETERS TO EXISTING DEVICES
-- This is the key part - adds new parameters to SetupDeviceParameterValues
-- =====================================================================

-- Add missing parameters to single-phase devices
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

-- Add missing parameters to three-phase devices
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

DROP TABLE temp_defaults;

COMMIT;

-- =====================================================================
-- VERIFICATION
-- =====================================================================
SELECT '=== SUMMARY ===' AS status;
SELECT 'ParameterGroups' AS "Table", COUNT(*) AS "Count" FROM "ParameterGroups"
UNION ALL SELECT 'Parameters', COUNT(*) FROM "Parameters"
UNION ALL SELECT 'ParameterValueSettingSchemes', COUNT(*) FROM "ParameterValueSettingSchemes"
UNION ALL SELECT 'SetupDeviceParameterValues', COUNT(*) FROM "SetupDeviceParameterValues";

SELECT '=== DEVICES WITH NEW PARAMS ===' AS status;
SELECT
    d."SerialNumber",
    COUNT(DISTINCT sdpv."ParameterId") AS "Total Parameters"
FROM "SetupDeviceParameterValues" sdpv
JOIN "SetupDevices" sd ON sdpv."SetupDeviceId" = sd."Id"
JOIN "Devices" d ON sd."DeviceId" = d."Id"
GROUP BY d."SerialNumber"
ORDER BY d."SerialNumber"
LIMIT 10;
