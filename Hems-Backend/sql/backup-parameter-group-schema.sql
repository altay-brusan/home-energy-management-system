-- =====================================================================
-- Backup SQL Script: Parameter Groups, Parameters, and Value Settings
-- Generated from database export on 2025-12-21
-- Maintains all FK/PK relationships with auto-generated UUIDs
-- =====================================================================

-- =====================================================================
-- PART 1: PARAMETER GROUPS (36 groups in hierarchical structure)
-- =====================================================================

-- Insert all parameter groups maintaining parent-child relationships
-- Using CTEs to preserve hierarchy: Top-level -> Second-level -> Third-level

WITH
-- Level 1: Top-level groups (ParentId = NULL)
level1_groups AS (
    INSERT INTO "ParameterGroups" ("Id", "ParentId", "Name", "Description", "Version", "SortOrder", "Created", "CreatedBy", "LastModified", "LastModifiedBy")
    VALUES
        (gen_random_uuid(), NULL, 'BatterySettings', 'Battery Settings - Charge sources, thresholds, protection', '1.0', 1, NOW(), 'system', NOW(), 'system'),
        (gen_random_uuid(), NULL, 'SystemWorkMode', 'System Work Mode - Export strategy, energy priority', '1.0', 2, NOW(), 'system', NOW(), 'system'),
        (gen_random_uuid(), NULL, 'TimeOfUse', 'Time of Use - 6 time slots, schedules', '1.0', 3, NOW(), 'system', NOW(), 'system'),
        (gen_random_uuid(), NULL, 'GeneratorAndSmartLoad', 'Generator & Smart Load - Gen charging, peak shaving, smart load', '1.0', 4, NOW(), 'system', NOW(), 'system'),
        (gen_random_uuid(), NULL, 'MaintenanceSettings', 'Maintenance Settings - Admin-level configuration', '1.0', 5, NOW(), 'system', NOW(), 'system')
    RETURNING "Id", "Name"
),

-- Level 2: Second-level groups (children of level 1)
level2_groups AS (
    INSERT INTO "ParameterGroups" ("Id", "ParentId", "Name", "Description", "Version", "SortOrder", "Created", "CreatedBy", "LastModified", "LastModifiedBy")
    SELECT
        gen_random_uuid(),
        l1."Id",
        g.name,
        g.description,
        '1.0',
        g.sort_order,
        NOW(),
        'system',
        NOW(),
        'system'
    FROM level1_groups l1
    CROSS JOIN (VALUES
        ('BatterySettings', 'ChargeSources', 'Charge Sources', 1),
        ('BatterySettings', 'ProtectionThresholds', 'Protection Thresholds', 2),
        ('SystemWorkMode', 'ExportStrategy', 'Export Strategy', 1),
        ('SystemWorkMode', 'EnergyPriority', 'Energy Priority', 2),
        ('SystemWorkMode', 'InverterMode', 'Inverter Mode', 3),
        ('SystemWorkMode', 'GridPeakShaving', 'Grid Peak Shaving', 4),
        ('TimeOfUse', 'ActiveDays', 'Active Days', 1),
        ('TimeOfUse', 'TimeSlot1', 'Time Slot 1', 2),
        ('TimeOfUse', 'TimeSlot2', 'Time Slot 2', 3),
        ('TimeOfUse', 'TimeSlot3', 'Time Slot 3', 4),
        ('TimeOfUse', 'TimeSlot4', 'Time Slot 4', 5),
        ('TimeOfUse', 'TimeSlot5', 'Time Slot 5', 6),
        ('TimeOfUse', 'TimeSlot6', 'Time Slot 6', 7),
        ('GeneratorAndSmartLoad', 'GeneratorSettings', 'Generator Settings', 1),
        ('GeneratorAndSmartLoad', 'GeneratorPeakShaving', 'Generator Peak Shaving', 2),
        ('GeneratorAndSmartLoad', 'SmartLoad', 'Smart Load', 3),
        ('MaintenanceSettings', 'BatteryMaintenance', 'Battery Maintenance Configuration', 1),
        ('MaintenanceSettings', 'GridCodeConfiguration', 'Grid Code and Protection Settings', 2),
        ('MaintenanceSettings', 'AdvancedFunctions', 'Advanced Inverter Functions', 3),
        ('MaintenanceSettings', 'ExternalRelayConfiguration', 'External Relay Settings', 4),
        ('MaintenanceSettings', 'GeneratorOverride', 'Generator and Smart Load Override', 5)
    ) AS g(parent_name, name, description, sort_order)
    WHERE l1."Name" = g.parent_name
    RETURNING "Id", "Name"
),

-- Level 3: Third-level groups (children of level 2)
level3_groups AS (
    INSERT INTO "ParameterGroups" ("Id", "ParentId", "Name", "Description", "Version", "SortOrder", "Created", "CreatedBy", "LastModified", "LastModifiedBy")
    SELECT
        gen_random_uuid(),
        l2."Id",
        g.name,
        g.description,
        '1.0',
        g.sort_order,
        NOW(),
        'system',
        NOW(),
        'system'
    FROM level2_groups l2
    CROSS JOIN (VALUES
        ('ChargeSources', 'GridCharging', 'Grid Charging Settings', 1),
        ('ChargeSources', 'GeneratorCharging', 'Generator Charging Settings', 2),
        ('GridCodeConfiguration', 'GridParameters', 'Grid Standard and Frequency Settings', 1),
        ('GridCodeConfiguration', 'ProtectionParameters', 'Grid Protection Thresholds', 2),
        ('GridCodeConfiguration', 'PowerFactorSettings', 'Power Factor and Regulation', 3),
        ('AdvancedFunctions', 'ArcFaultProtection', 'Arc Fault Detection Settings', 1),
        ('AdvancedFunctions', 'IslandProtection', 'Island Protection Settings', 2),
        ('AdvancedFunctions', 'ParallelSettings', 'Parallel Operation Configuration', 3),
        ('AdvancedFunctions', 'CTSettings', 'Current Transformer Settings', 4)
    ) AS g(parent_name, name, description, sort_order)
    WHERE l2."Name" = g.parent_name
    RETURNING "Id", "Name"
)

-- Return summary of created groups
SELECT 'Parameter Groups Created' AS status,
       (SELECT COUNT(*) FROM level1_groups) +
       (SELECT COUNT(*) FROM level2_groups) +
       (SELECT COUNT(*) FROM level3_groups) AS total_count;


-- =====================================================================
-- PART 2: PARAMETERS (109 parameters)
-- =====================================================================

-- Admin-level Maintenance Parameters (27 parameters)
INSERT INTO "Parameters" ("Id", "ParameterGroupId", "Name", "Description", "ElementType", "RoleType", "Unit", "DefaultValue", "SortOrder", "Created", "CreatedBy", "LastModified", "LastModifiedBy")
SELECT
    gen_random_uuid(),
    pg."Id",
    p.name,
    p.description,
    p.element_type,
    'Admin',
    p.unit,
    p.default_value,
    p.sort_order,
    NOW(),
    'system',
    NOW(),
    'system'
FROM "ParameterGroups" pg
CROSS JOIN (VALUES
    ('BatteryMaintenance', 'BatteryControlMode', 'Battery Control Mode (Voltage/Capacity/No Battery)', 'Text', '', 'capacity', 1),
    ('BatteryMaintenance', 'LithiumBatteryWakeUpEnabled', 'Lithium Battery Wake-Up Enable', 'Boolean', '', 'true', 2),
    ('BatteryMaintenance', 'BatteryResistance', 'Battery Internal Resistance', 'Decimal', 'mΩ', '100', 3),
    ('BatteryMaintenance', 'ChargeEfficiency', 'Battery Charge Efficiency', 'Decimal', '%', '98.3', 4),
    ('GridParameters', 'GridStandard', 'Grid Standard (General/UL1741/CPUC RULE21/SRD-UL1741/CEI 0-21)', 'Text', '', 'General', 1),
    ('GridParameters', 'GridFrequency', 'Grid Frequency', 'Decimal', 'Hz', '50', 2),
    ('GridParameters', 'OutputVoltageLevel', 'Output Voltage Level (220V/230V/240V/120V/133V)', 'Text', '', '230V', 3),
    ('ProtectionParameters', 'GridVoltageHigh', 'Grid High Voltage Protection Point', 'Decimal', 'V', '253', 1),
    ('ProtectionParameters', 'GridVoltageLow', 'Grid Low Voltage Protection Point', 'Decimal', 'V', '184', 2),
    ('ProtectionParameters', 'GridFrequencyHigh', 'Grid High Frequency Protection Point', 'Decimal', 'Hz', '50.3', 3),
    ('ProtectionParameters', 'GridFrequencyLow', 'Grid Low Frequency Protection Point', 'Decimal', 'Hz', '49.7', 4),
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
    ('CTSettings', 'ExternalCTRatio', 'External CT Ratio', 'Decimal', '', '30', 1),
    ('CTSettings', 'ExternalMeterCTRatio', 'External Meter CT Ratio', 'Decimal', '', '30', 2),
    ('AdvancedFunctions', 'LithiumBatteryLossFaultEnabled', 'Lithium Battery Loss Fault Enable', 'Boolean', '', 'true', 1),
    ('ExternalRelayConfiguration', 'ExternalRelayFeatureEnabled', 'External Relay Feature Enable', 'Boolean', '', 'false', 1),
    ('ExternalRelayConfiguration', 'RelayBitmap', 'External Relay Bitmap (0-65535)', 'Decimal', '', '0', 2),
    ('GeneratorOverride', 'ForceSmartLoadOn', 'Force Smart Load On (Override)', 'Boolean', '', 'false', 1)
) AS p(group_name, name, description, element_type, unit, default_value, sort_order)
WHERE pg."Name" = p.group_name;

-- User-level Parameters (82 parameters)
INSERT INTO "Parameters" ("Id", "ParameterGroupId", "Name", "Description", "ElementType", "RoleType", "Unit", "DefaultValue", "SortOrder", "Created", "CreatedBy", "LastModified", "LastModifiedBy")
SELECT
    gen_random_uuid(),
    pg."Id",
    p.name,
    p.description,
    p.element_type,
    'User',
    p.unit,
    p.default_value,
    p.sort_order,
    NOW(),
    'system',
    NOW(),
    'system'
FROM "ParameterGroups" pg
CROSS JOIN (VALUES
    ('GridCharging', 'GridChargingEnabled', 'Enable/Disable Grid Charging', 'Boolean', '', 'true', 1),
    ('GridCharging', 'GridChargingStartSOC', 'Grid Charging Start State of Charge', 'Decimal', '%', '30', 2),
    ('GridCharging', 'GridChargingCurrent', 'Grid Charging Current', 'Decimal', 'A', '40', 3),
    ('GeneratorCharging', 'GeneratorChargingEnabled', 'Enable/Disable Generator Charging', 'Boolean', '', 'false', 1),
    ('GeneratorCharging', 'GeneratorChargingStartSOC', 'Generator Charging Start State of Charge', 'Decimal', '%', '30', 2),
    ('GeneratorCharging', 'GeneratorChargingCurrent', 'Generator Charging Current', 'Decimal', 'A', '40', 3),
    ('GeneratorCharging', 'GeneratorMaxRunTime', 'Generator Maximum Run Time', 'Decimal', 'hrs', '24', 4),
    ('GeneratorCharging', 'GeneratorCoolingTime', 'Generator Cooling Time', 'Decimal', 'hrs', '0', 5),
    ('ProtectionThresholds', 'ShutdownSOC', 'Shutdown SOC (inverter off)', 'Decimal', '%', '20', 1),
    ('ProtectionThresholds', 'LowBatteryWarning', 'Low Battery Warning (alarm)', 'Decimal', '%', '35', 2),
    ('ProtectionThresholds', 'RestartSOC', 'Restart SOC (resume output)', 'Decimal', '%', '50', 3),
    ('ExportStrategy', 'ExportStrategySelection', 'Export Strategy Selection (SellingFirst, ZeroExportToLoad, ZeroExportToCT)', 'Text', '', 'ZeroExportToCT', 1),
    ('EnergyPriority', 'EnergyPrioritySelection', 'Energy Priority Selection (BatteryFirst, LoadFirst)', 'Text', '', 'BatteryFirst', 1),
    ('InverterMode', 'SolarSell', 'Solar Sell Enable/Disable', 'Boolean', '', 'false', 1),
    ('InverterMode', 'MaxSellPower', 'Maximum Sell Power', 'Decimal', 'W', '12000', 2),
    ('InverterMode', 'ZeroExportLimit', 'Zero Export Limit', 'Decimal', 'W', '2', 3),
    ('InverterMode', 'ForceOffGrid', 'Force Off-Grid Mode', 'Boolean', '', 'false', 4),
    ('GridPeakShaving', 'GridPeakShavingEnabled', 'Enable/Disable Grid Peak Shaving', 'Boolean', '', 'false', 1),
    ('GridPeakShaving', 'GridPeakShavingLimitPower', 'Grid Peak Shaving Limit Power', 'Decimal', 'W', '8000', 2),
    ('TimeOfUse', 'TimeOfUseEnabled', 'Enable/Disable Time of Use', 'Boolean', '', 'true', 1),
    ('ActiveDays', 'Monday', 'Active on Monday', 'Boolean', '', 'true', 1),
    ('ActiveDays', 'Tuesday', 'Active on Tuesday', 'Boolean', '', 'true', 2),
    ('ActiveDays', 'Wednesday', 'Active on Wednesday', 'Boolean', '', 'true', 3),
    ('ActiveDays', 'Thursday', 'Active on Thursday', 'Boolean', '', 'true', 4),
    ('ActiveDays', 'Friday', 'Active on Friday', 'Boolean', '', 'true', 5),
    ('ActiveDays', 'Saturday', 'Active on Saturday', 'Boolean', '', 'true', 6),
    ('ActiveDays', 'Sunday', 'Active on Sunday', 'Boolean', '', 'true', 7),
    ('TimeSlot1', 'TimeSlot1Start', 'Time Slot 1 Start Time', 'Text', '', '00:00', 1),
    ('TimeSlot1', 'TimeSlot1End', 'Time Slot 1 End Time', 'Text', '', '06:00', 2),
    ('TimeSlot1', 'TimeSlot1GridCharge', 'Time Slot 1 Grid Charge', 'Boolean', '', 'true', 3),
    ('TimeSlot1', 'TimeSlot1GenCharge', 'Time Slot 1 Generator Charge', 'Boolean', '', 'false', 4),
    ('TimeSlot1', 'TimeSlot1MaxDischarge', 'Time Slot 1 Maximum Discharge', 'Decimal', 'W', '3000', 5),
    ('TimeSlot1', 'TimeSlot1MinBatterySOC', 'Time Slot 1 Minimum Battery SOC', 'Decimal', '%', '30', 6),
    ('TimeSlot1', 'TimeSlot1MinBatteryVoltage', 'Time Slot 1 Minimum Battery Voltage', 'Decimal', 'V', '48.0', 7),
    ('TimeSlot2', 'TimeSlot2Start', 'Time Slot 2 Start Time', 'Text', '', '06:00', 1),
    ('TimeSlot2', 'TimeSlot2End', 'Time Slot 2 End Time', 'Text', '', '10:00', 2),
    ('TimeSlot2', 'TimeSlot2GridCharge', 'Time Slot 2 Grid Charge', 'Boolean', '', 'false', 3),
    ('TimeSlot2', 'TimeSlot2GenCharge', 'Time Slot 2 Generator Charge', 'Boolean', '', 'false', 4),
    ('TimeSlot2', 'TimeSlot2MaxDischarge', 'Time Slot 2 Maximum Discharge', 'Decimal', 'W', '3000', 5),
    ('TimeSlot2', 'TimeSlot2MinBatterySOC', 'Time Slot 2 Minimum Battery SOC', 'Decimal', '%', '30', 6),
    ('TimeSlot2', 'TimeSlot2MinBatteryVoltage', 'Time Slot 2 Minimum Battery Voltage', 'Decimal', 'V', '48.0', 7),
    ('TimeSlot3', 'TimeSlot3Start', 'Time Slot 3 Start Time', 'Text', '', '10:00', 1),
    ('TimeSlot3', 'TimeSlot3End', 'Time Slot 3 End Time', 'Text', '', '14:00', 2),
    ('TimeSlot3', 'TimeSlot3GridCharge', 'Time Slot 3 Grid Charge', 'Boolean', '', 'false', 3),
    ('TimeSlot3', 'TimeSlot3GenCharge', 'Time Slot 3 Generator Charge', 'Boolean', '', 'false', 4),
    ('TimeSlot3', 'TimeSlot3MaxDischarge', 'Time Slot 3 Maximum Discharge', 'Decimal', 'W', '3000', 5),
    ('TimeSlot3', 'TimeSlot3MinBatterySOC', 'Time Slot 3 Minimum Battery SOC', 'Decimal', '%', '30', 6),
    ('TimeSlot3', 'TimeSlot3MinBatteryVoltage', 'Time Slot 3 Minimum Battery Voltage', 'Decimal', 'V', '48.0', 7),
    ('TimeSlot4', 'TimeSlot4Start', 'Time Slot 4 Start Time', 'Text', '', '14:00', 1),
    ('TimeSlot4', 'TimeSlot4End', 'Time Slot 4 End Time', 'Text', '', '18:00', 2),
    ('TimeSlot4', 'TimeSlot4GridCharge', 'Time Slot 4 Grid Charge', 'Boolean', '', 'false', 3),
    ('TimeSlot4', 'TimeSlot4GenCharge', 'Time Slot 4 Generator Charge', 'Boolean', '', 'false', 4),
    ('TimeSlot4', 'TimeSlot4MaxDischarge', 'Time Slot 4 Maximum Discharge', 'Decimal', 'W', '3000', 5),
    ('TimeSlot4', 'TimeSlot4MinBatterySOC', 'Time Slot 4 Minimum Battery SOC', 'Decimal', '%', '30', 6),
    ('TimeSlot4', 'TimeSlot4MinBatteryVoltage', 'Time Slot 4 Minimum Battery Voltage', 'Decimal', 'V', '48.0', 7),
    ('TimeSlot5', 'TimeSlot5Start', 'Time Slot 5 Start Time', 'Text', '', '18:00', 1),
    ('TimeSlot5', 'TimeSlot5End', 'Time Slot 5 End Time', 'Text', '', '22:00', 2),
    ('TimeSlot5', 'TimeSlot5GridCharge', 'Time Slot 5 Grid Charge', 'Boolean', '', 'false', 3),
    ('TimeSlot5', 'TimeSlot5GenCharge', 'Time Slot 5 Generator Charge', 'Boolean', '', 'false', 4),
    ('TimeSlot5', 'TimeSlot5MaxDischarge', 'Time Slot 5 Maximum Discharge', 'Decimal', 'W', '3000', 5),
    ('TimeSlot5', 'TimeSlot5MinBatterySOC', 'Time Slot 5 Minimum Battery SOC', 'Decimal', '%', '30', 6),
    ('TimeSlot5', 'TimeSlot5MinBatteryVoltage', 'Time Slot 5 Minimum Battery Voltage', 'Decimal', 'V', '48.0', 7),
    ('TimeSlot6', 'TimeSlot6Start', 'Time Slot 6 Start Time', 'Text', '', '22:00', 1),
    ('TimeSlot6', 'TimeSlot6End', 'Time Slot 6 End Time', 'Text', '', '24:00', 2),
    ('TimeSlot6', 'TimeSlot6GridCharge', 'Time Slot 6 Grid Charge', 'Boolean', '', 'false', 3),
    ('TimeSlot6', 'TimeSlot6GenCharge', 'Time Slot 6 Generator Charge', 'Boolean', '', 'false', 4),
    ('TimeSlot6', 'TimeSlot6MaxDischarge', 'Time Slot 6 Maximum Discharge', 'Decimal', 'W', '3000', 5),
    ('TimeSlot6', 'TimeSlot6MinBatterySOC', 'Time Slot 6 Minimum Battery SOC', 'Decimal', '%', '30', 6),
    ('TimeSlot6', 'TimeSlot6MinBatteryVoltage', 'Time Slot 6 Minimum Battery Voltage', 'Decimal', 'V', '48.0', 7),
    ('GeneratorSettings', 'GeneratorCharge', 'Generator Charge Enable/Disable', 'Boolean', '', 'false', 1),
    ('GeneratorSettings', 'GeneratorGridCharge', 'Generator Grid Charge Enable/Disable', 'Boolean', '', 'true', 2),
    ('GeneratorSettings', 'GeneratorStartSOC', 'Generator Start State of Charge', 'Decimal', '%', '30', 3),
    ('GeneratorSettings', 'GeneratorChargeCurrent', 'Generator Charge Current', 'Decimal', 'A', '40', 4),
    ('GeneratorPeakShaving', 'GeneratorPeakShavingEnabled', 'Enable/Disable Generator Peak Shaving', 'Boolean', '', 'false', 1),
    ('GeneratorPeakShaving', 'GeneratorPeakShavingLimitPower', 'Generator Peak Shaving Limit Power', 'Decimal', 'W', '8000', 2),
    ('SmartLoad', 'SmartLoadEnabled', 'Enable/Disable Smart Load', 'Boolean', '', 'false', 1),
    ('SmartLoad', 'SmartLoadOffSOC', 'Smart Load OFF SOC (turn off below)', 'Decimal', '%', '95', 2),
    ('SmartLoad', 'SmartLoadOnSOC', 'Smart Load ON SOC (turn on above)', 'Decimal', '%', '100', 3),
    ('SmartLoad', 'SmartLoadPVPowerThreshold', 'Smart Load PV Power Threshold', 'Decimal', 'W', '0', 4),
    ('SmartLoad', 'SmartLoadOpenDelay', 'Smart Load Open Delay', 'Decimal', 'min', '100', 5),
    ('SmartLoad', 'SmartLoadOnGridAlwaysOn', 'Smart Load On Grid Always On', 'Boolean', '', 'false', 6)
) AS p(group_name, name, description, element_type, unit, default_value, sort_order)
WHERE pg."Name" = p.group_name;


-- =====================================================================
-- PART 3: PARAMETER VALUE SETTING SCHEMES
-- =====================================================================
-- Note: Creating schemes for both single-phase-loftty and three-phase-loftty models

-- Single-phase-loftty Model (100 parameter value settings)
INSERT INTO "ParameterValueSettingSchemes" ("Id", "ParameterId", "ParameterGroupId", "Value", "SortOrder", "ModelNumber", "Created", "CreatedBy", "LastModified", "LastModifiedBy")
SELECT
    gen_random_uuid(),
    p."Id",
    NULL,
    v.value,
    v.sort_order,
    'single-phase-loftty',
    NOW(),
    'system',
    NOW(),
    'system'
FROM "Parameters" p
CROSS JOIN (VALUES
    ('GridChargingEnabled', 'true', 0),
    ('GridChargingStartSOC', '30', 0),
    ('GridChargingCurrent', '40', 0),
    ('GeneratorChargingEnabled', 'false', 0),
    ('GeneratorChargingStartSOC', '30', 0),
    ('GeneratorChargingCurrent', '40', 0),
    ('GeneratorMaxRunTime', '24', 0),
    ('GeneratorCoolingTime', '0', 0),
    ('ShutdownSOC', '20', 0),
    ('LowBatteryWarning', '35', 0),
    ('RestartSOC', '50', 0),
    ('ExportStrategySelection', 'ZeroExportToCT', 0),
    ('EnergyPrioritySelection', 'BatteryFirst', 0),
    ('SolarSell', 'false', 0),
    ('MaxSellPower', '12000', 0),
    ('ZeroExportLimit', '2', 0),
    ('ForceOffGrid', 'false', 0),
    ('GridPeakShavingEnabled', 'false', 0),
    ('GridPeakShavingLimitPower', '8000', 0),
    ('TimeOfUseEnabled', 'true', 0),
    ('Monday', 'true', 0),
    ('Tuesday', 'true', 0),
    ('Wednesday', 'true', 0),
    ('Thursday', 'true', 0),
    ('Friday', 'true', 0),
    ('Saturday', 'true', 0),
    ('Sunday', 'true', 0),
    ('TimeSlot1Start', '00:00', 0),
    ('TimeSlot1End', '06:00', 0),
    ('TimeSlot1GridCharge', 'true', 0),
    ('TimeSlot1GenCharge', 'false', 0),
    ('TimeSlot1MaxDischarge', '3000', 0),
    ('TimeSlot1MinBatterySOC', '30', 0),
    ('TimeSlot1MinBatteryVoltage', '48.0', 0),
    ('TimeSlot2Start', '06:00', 0),
    ('TimeSlot2End', '10:00', 0),
    ('TimeSlot2GridCharge', 'false', 0),
    ('TimeSlot2GenCharge', 'false', 0),
    ('TimeSlot2MaxDischarge', '3000', 0),
    ('TimeSlot2MinBatterySOC', '30', 0),
    ('TimeSlot2MinBatteryVoltage', '48.0', 0),
    ('TimeSlot3Start', '10:00', 0),
    ('TimeSlot3End', '14:00', 0),
    ('TimeSlot3GridCharge', 'false', 0),
    ('TimeSlot3GenCharge', 'false', 0),
    ('TimeSlot3MaxDischarge', '3000', 0),
    ('TimeSlot3MinBatterySOC', '30', 0),
    ('TimeSlot3MinBatteryVoltage', '48.0', 0),
    ('TimeSlot4Start', '14:00', 0),
    ('TimeSlot4End', '18:00', 0),
    ('TimeSlot4GridCharge', 'false', 0),
    ('TimeSlot4GenCharge', 'false', 0),
    ('TimeSlot4MaxDischarge', '3000', 0),
    ('TimeSlot4MinBatterySOC', '30', 0),
    ('TimeSlot4MinBatteryVoltage', '48.0', 0),
    ('TimeSlot5Start', '18:00', 0),
    ('TimeSlot5End', '22:00', 0),
    ('TimeSlot5GridCharge', 'false', 0),
    ('TimeSlot5GenCharge', 'false', 0),
    ('TimeSlot5MaxDischarge', '3000', 0),
    ('TimeSlot5MinBatterySOC', '30', 0),
    ('TimeSlot5MinBatteryVoltage', '48.0', 0),
    ('TimeSlot6Start', '22:00', 0),
    ('TimeSlot6End', '24:00', 0),
    ('TimeSlot6GridCharge', 'false', 0),
    ('TimeSlot6GenCharge', 'false', 0),
    ('TimeSlot6MaxDischarge', '3000', 0),
    ('TimeSlot6MinBatterySOC', '30', 0),
    ('TimeSlot6MinBatteryVoltage', '48.0', 0),
    ('GeneratorCharge', 'false', 0),
    ('GeneratorGridCharge', 'true', 0),
    ('GeneratorStartSOC', '30', 0),
    ('GeneratorChargeCurrent', '40', 0),
    ('GeneratorPeakShavingEnabled', 'false', 0),
    ('GeneratorPeakShavingLimitPower', '8000', 0),
    ('SmartLoadEnabled', 'false', 0),
    ('SmartLoadOffSOC', '95', 0),
    ('SmartLoadOnSOC', '100', 0),
    ('SmartLoadPVPowerThreshold', '0', 0),
    ('SmartLoadOpenDelay', '100', 0),
    ('SmartLoadOnGridAlwaysOn', 'false', 0)
) AS v(param_name, value, sort_order)
WHERE p."Name" = v.param_name;

-- Three-phase-loftty Model (108 parameter value settings: 81 User + 27 Admin)
INSERT INTO "ParameterValueSettingSchemes" ("Id", "ParameterId", "ParameterGroupId", "Value", "SortOrder", "ModelNumber", "Created", "CreatedBy", "LastModified", "LastModifiedBy")
SELECT
    gen_random_uuid(),
    p."Id",
    NULL,
    v.value,
    v.sort_order,
    'three-phase-loftty',
    NOW(),
    'system',
    NOW(),
    'system'
FROM "Parameters" p
CROSS JOIN (VALUES
    -- Admin-level parameters (27)
    ('BatteryControlMode', 'capacity', 0),
    ('LithiumBatteryWakeUpEnabled', 'true', 0),
    ('BatteryResistance', '100', 0),
    ('ChargeEfficiency', '98.3', 0),
    ('GridStandard', 'General', 0),
    ('GridFrequency', '50', 0),
    ('OutputVoltageLevel', '230V', 0),
    ('GridVoltageHigh', '253', 0),
    ('GridVoltageLow', '184', 0),
    ('GridFrequencyHigh', '50.3', 0),
    ('GridFrequencyLow', '49.7', 0),
    ('ReconnectionTime', '60', 0),
    ('PowerFactorTarget', '1.0', 0),
    ('ActivePowerRegulation', '10.0', 0),
    ('ReactivePowerRegulation', '10.0', 0),
    ('ArcFaultDetectionEnabled', 'true', 0),
    ('IslandProtectionEnabled', 'true', 0),
    ('ParallelEnabled', 'false', 0),
    ('ParallelRole', 'standalone', 0),
    ('ParallelPhase', 'A', 0),
    ('ModbusAddress', '1', 0),
    ('ExternalCTRatio', '30', 0),
    ('ExternalMeterCTRatio', '30', 0),
    ('LithiumBatteryLossFaultEnabled', 'true', 0),
    ('ExternalRelayFeatureEnabled', 'false', 0),
    ('RelayBitmap', '0', 0),
    ('ForceSmartLoadOn', 'false', 0),
    -- User-level parameters (81) - Note: Different values for protection thresholds
    ('GridChargingEnabled', 'true', 0),
    ('GridChargingStartSOC', '30', 0),
    ('GridChargingCurrent', '40', 0),
    ('GeneratorChargingEnabled', 'false', 0),
    ('GeneratorChargingStartSOC', '30', 0),
    ('GeneratorChargingCurrent', '40', 0),
    ('GeneratorMaxRunTime', '24', 0),
    ('GeneratorCoolingTime', '0', 0),
    ('ShutdownSOC', '10', 0),
    ('LowBatteryWarning', '20', 0),
    ('RestartSOC', '40', 0),
    ('ExportStrategySelection', 'ZeroExportToCT', 0),
    ('EnergyPrioritySelection', 'BatteryFirst', 0),
    ('SolarSell', 'false', 0),
    ('MaxSellPower', '12000', 0),
    ('ZeroExportLimit', '2', 0),
    ('ForceOffGrid', 'false', 0),
    ('GridPeakShavingEnabled', 'false', 0),
    ('GridPeakShavingLimitPower', '8000', 0),
    ('TimeOfUseEnabled', 'true', 0),
    ('Monday', 'true', 0),
    ('Tuesday', 'true', 0),
    ('Wednesday', 'true', 0),
    ('Thursday', 'true', 0),
    ('Friday', 'true', 0),
    ('Saturday', 'true', 0),
    ('Sunday', 'true', 0),
    ('TimeSlot1Start', '00:00', 0),
    ('TimeSlot1End', '06:00', 0),
    ('TimeSlot1GridCharge', 'true', 0),
    ('TimeSlot1GenCharge', 'false', 0),
    ('TimeSlot1MaxDischarge', '3000', 0),
    ('TimeSlot1MinBatterySOC', '30', 0),
    ('TimeSlot1MinBatteryVoltage', '48.0', 0),
    ('TimeSlot2Start', '06:00', 0),
    ('TimeSlot2End', '10:00', 0),
    ('TimeSlot2GridCharge', 'false', 0),
    ('TimeSlot2GenCharge', 'false', 0),
    ('TimeSlot2MaxDischarge', '3000', 0),
    ('TimeSlot2MinBatterySOC', '30', 0),
    ('TimeSlot2MinBatteryVoltage', '48.0', 0),
    ('TimeSlot3Start', '10:00', 0),
    ('TimeSlot3End', '14:00', 0),
    ('TimeSlot3GridCharge', 'false', 0),
    ('TimeSlot3GenCharge', 'false', 0),
    ('TimeSlot3MaxDischarge', '3000', 0),
    ('TimeSlot3MinBatterySOC', '30', 0),
    ('TimeSlot3MinBatteryVoltage', '48.0', 0),
    ('TimeSlot4Start', '14:00', 0),
    ('TimeSlot4End', '18:00', 0),
    ('TimeSlot4GridCharge', 'false', 0),
    ('TimeSlot4GenCharge', 'false', 0),
    ('TimeSlot4MaxDischarge', '3000', 0),
    ('TimeSlot4MinBatterySOC', '30', 0),
    ('TimeSlot4MinBatteryVoltage', '48.0', 0),
    ('TimeSlot5Start', '18:00', 0),
    ('TimeSlot5End', '22:00', 0),
    ('TimeSlot5GridCharge', 'false', 0),
    ('TimeSlot5GenCharge', 'false', 0),
    ('TimeSlot5MaxDischarge', '3000', 0),
    ('TimeSlot5MinBatterySOC', '30', 0),
    ('TimeSlot5MinBatteryVoltage', '48.0', 0),
    ('TimeSlot6Start', '22:00', 0),
    ('TimeSlot6End', '24:00', 0),
    ('TimeSlot6GridCharge', 'false', 0),
    ('TimeSlot6GenCharge', 'false', 0),
    ('TimeSlot6MaxDischarge', '3000', 0),
    ('TimeSlot6MinBatterySOC', '30', 0),
    ('TimeSlot6MinBatteryVoltage', '48.0', 0),
    ('GeneratorCharge', 'false', 0),
    ('GeneratorGridCharge', 'true', 0),
    ('GeneratorStartSOC', '30', 0),
    ('GeneratorChargeCurrent', '40', 0),
    ('GeneratorPeakShavingEnabled', 'false', 0),
    ('GeneratorPeakShavingLimitPower', '8000', 0),
    ('SmartLoadEnabled', 'false', 0),
    ('SmartLoadOffSOC', '95', 0),
    ('SmartLoadOnSOC', '100', 0),
    ('SmartLoadPVPowerThreshold', '0', 0),
    ('SmartLoadOpenDelay', '1', 0),
    ('SmartLoadOnGridAlwaysOn', 'false', 0)
) AS v(param_name, value, sort_order)
WHERE p."Name" = v.param_name;


-- =====================================================================
-- VERIFICATION QUERIES
-- =====================================================================
-- Uncomment to verify after restoration

-- Count all parameter groups
-- SELECT COUNT(*) AS "ParameterGroupsCount" FROM "ParameterGroups";

-- Count all parameters
-- SELECT COUNT(*) AS "ParametersCount" FROM "Parameters";

-- Count parameter value settings by model
-- SELECT
--     "ModelNumber",
--     COUNT(*) AS "SchemeCount"
-- FROM "ParameterValueSettingSchemes"
-- GROUP BY "ModelNumber"
-- ORDER BY "ModelNumber";

-- Expected Results:
-- ParameterGroups: 36
-- Parameters: 109 (27 Admin + 82 User)
-- ParameterValueSettingSchemes: 208 total (100 single-phase + 108 three-phase)
