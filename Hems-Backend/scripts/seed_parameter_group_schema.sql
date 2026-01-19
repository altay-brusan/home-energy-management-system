-- =====================================================================
-- SEED SQL Script: Parameter Groups, Parameters, and Value Settings
-- For FRESH DATABASE SEEDING ONLY
-- For updating existing databases, use: update_existing_device_settings.sql
--
-- Updated: 2025-01-19 - New default values
-- =====================================================================

-- =====================================================================
-- PART 1: PARAMETER GROUPS (37 groups)
-- =====================================================================

WITH
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
level2_groups AS (
    INSERT INTO "ParameterGroups" ("Id", "ParentId", "Name", "Description", "Version", "SortOrder", "Created", "CreatedBy", "LastModified", "LastModifiedBy")
    SELECT gen_random_uuid(), l1."Id", g.name, g.description, '1.0', g.sort_order, NOW(), 'system', NOW(), 'system'
    FROM level1_groups l1
    CROSS JOIN (VALUES
        ('BatterySettings', 'BatteryConfiguration', 'Battery Configuration - Capacity and current limits', 0),
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
level3_groups AS (
    INSERT INTO "ParameterGroups" ("Id", "ParentId", "Name", "Description", "Version", "SortOrder", "Created", "CreatedBy", "LastModified", "LastModifiedBy")
    SELECT gen_random_uuid(), l2."Id", g.name, g.description, '1.0', g.sort_order, NOW(), 'system', NOW(), 'system'
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
SELECT 'Parameter Groups Created' AS status,
       (SELECT COUNT(*) FROM level1_groups) +
       (SELECT COUNT(*) FROM level2_groups) +
       (SELECT COUNT(*) FROM level3_groups) AS total_count;


-- =====================================================================
-- PART 2: PARAMETERS (112 parameters with NEW default values)
-- =====================================================================

-- Admin-level Parameters
INSERT INTO "Parameters" ("Id", "ParameterGroupId", "Name", "Description", "ElementType", "RoleType", "Unit", "DefaultValue", "SortOrder", "Created", "CreatedBy", "LastModified", "LastModifiedBy")
SELECT gen_random_uuid(), pg."Id", p.name, p.description, p.element_type, 'Admin', p.unit, p.default_value, p.sort_order, NOW(), 'system', NOW(), 'system'
FROM "ParameterGroups" pg
CROSS JOIN (VALUES
    ('BatteryMaintenance', 'BatteryControlMode', 'Battery Control Mode (Voltage/Capacity/No Battery)', 'Text', '', 'Lithium', 1),
    ('BatteryMaintenance', 'LithiumBatteryWakeUpEnabled', 'Lithium Battery Wake-Up Enable', 'Boolean', '', 'true', 2),
    ('BatteryMaintenance', 'BatteryResistance', 'Battery Internal Resistance', 'Decimal', 'mΩ', '100', 3),
    ('BatteryMaintenance', 'ChargeEfficiency', 'Battery Charge Efficiency', 'Decimal', '%', '98.3', 4),
    ('GridParameters', 'GridStandard', 'Grid Standard', 'Text', '', 'General', 1),
    ('GridParameters', 'GridFrequency', 'Grid Frequency', 'Decimal', 'Hz', '50', 2),
    ('GridParameters', 'OutputVoltageLevel', 'Output Voltage Level', 'Text', '', '230V', 3),
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
WHERE pg."Name" = p.group_name;

-- User-level Parameters
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
    ('SmartLoad', 'SmartLoadEnabled', 'Enable/Disable Smart Load', 'Boolean', '', 'false', 1),
    ('SmartLoad', 'SmartLoadOffSOC', 'Smart Load OFF SOC (turn off below)', 'Decimal', '%', '95', 2),
    ('SmartLoad', 'SmartLoadOnSOC', 'Smart Load ON SOC (turn on above)', 'Decimal', '%', '100', 3),
    ('SmartLoad', 'SmartLoadPVPowerThreshold', 'Smart Load PV Power Threshold', 'Decimal', 'W', '0', 4),
    ('SmartLoad', 'SmartLoadOpenDelay', 'Smart Load Open Delay', 'Decimal', 'min', '100', 5),
    ('SmartLoad', 'SmartLoadOnGridAlwaysOn', 'Smart Load On Grid Always On', 'Boolean', '', 'false', 6)
) AS p(group_name, name, description, element_type, unit, default_value, sort_order)
WHERE pg."Name" = p.group_name;


-- =====================================================================
-- PART 3: PARAMETER VALUE SETTING SCHEMES (with NEW values)
-- =====================================================================

-- Single-phase-loftty Model
INSERT INTO "ParameterValueSettingSchemes" ("Id", "ParameterId", "ParameterGroupId", "Value", "SortOrder", "ModelNumber", "Created", "CreatedBy", "LastModified", "LastModifiedBy")
SELECT gen_random_uuid(), p."Id", NULL, v.value, 0, 'single-phase-loftty', NOW(), 'system', NOW(), 'system'
FROM "Parameters" p
CROSS JOIN (VALUES
    ('BatteryCapacity', '200'), ('MaxBatteryChargeCurrent', '100'), ('MaxBatteryDischargeCurrent', '100'),
    ('GridChargingEnabled', 'true'), ('GridChargingStartSOC', '20'), ('GridChargingCurrent', '100'),
    ('GeneratorChargingEnabled', 'false'), ('GeneratorChargingStartSOC', '20'), ('GeneratorChargingCurrent', '100'),
    ('GeneratorMaxRunTime', '24'), ('GeneratorCoolingTime', '0'),
    ('ShutdownSOC', '10'), ('LowBatteryWarning', '15'), ('RestartSOC', '50'),
    ('ExportStrategySelection', 'ZeroExportToCT'), ('EnergyPrioritySelection', 'BatteryFirst'),
    ('SolarSell', 'false'), ('MaxSolarSellPower', '12000'), ('MaxSellPower', '100'),
    ('ZeroExportLimit', '20'), ('ForceOffGrid', 'false'),
    ('GridPeakShavingEnabled', 'false'), ('GridPeakShavingLimitPower', '2000'),
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
    ('SmartLoadEnabled', 'false'), ('SmartLoadOffSOC', '95'), ('SmartLoadOnSOC', '100'),
    ('SmartLoadPVPowerThreshold', '0'), ('SmartLoadOpenDelay', '100'), ('SmartLoadOnGridAlwaysOn', 'false')
) AS v(param_name, value)
WHERE p."Name" = v.param_name;

-- Three-phase-loftty Model (includes Admin parameters)
INSERT INTO "ParameterValueSettingSchemes" ("Id", "ParameterId", "ParameterGroupId", "Value", "SortOrder", "ModelNumber", "Created", "CreatedBy", "LastModified", "LastModifiedBy")
SELECT gen_random_uuid(), p."Id", NULL, v.value, 0, 'three-phase-loftty', NOW(), 'system', NOW(), 'system'
FROM "Parameters" p
CROSS JOIN (VALUES
    -- Admin parameters
    ('BatteryControlMode', 'Lithium'), ('LithiumBatteryWakeUpEnabled', 'true'),
    ('BatteryResistance', '100'), ('ChargeEfficiency', '98.3'),
    ('GridStandard', 'General'), ('GridFrequency', '50'), ('OutputVoltageLevel', '230V'),
    ('GridVoltageHigh', '265'), ('GridVoltageLow', '185'),
    ('GridFrequencyHigh', '51.5'), ('GridFrequencyLow', '48'), ('ReconnectionTime', '60'),
    ('PowerFactorTarget', '1.0'), ('ActivePowerRegulation', '10.0'), ('ReactivePowerRegulation', '10.0'),
    ('ArcFaultDetectionEnabled', 'true'), ('IslandProtectionEnabled', 'true'),
    ('ParallelEnabled', 'false'), ('ParallelRole', 'standalone'), ('ParallelPhase', 'A'), ('ModbusAddress', '1'),
    ('ExternalCTRatio', '2000'), ('ExternalMeterCTRatio', '30'),
    ('LithiumBatteryLossFaultEnabled', 'true'), ('ExternalRelayFeatureEnabled', 'false'),
    ('RelayBitmap', '0'), ('ForceSmartLoadOn', 'false'),
    -- User parameters
    ('BatteryCapacity', '200'), ('MaxBatteryChargeCurrent', '100'), ('MaxBatteryDischargeCurrent', '100'),
    ('GridChargingEnabled', 'true'), ('GridChargingStartSOC', '20'), ('GridChargingCurrent', '100'),
    ('GeneratorChargingEnabled', 'false'), ('GeneratorChargingStartSOC', '20'), ('GeneratorChargingCurrent', '100'),
    ('GeneratorMaxRunTime', '24'), ('GeneratorCoolingTime', '0'),
    ('ShutdownSOC', '10'), ('LowBatteryWarning', '15'), ('RestartSOC', '50'),
    ('ExportStrategySelection', 'ZeroExportToCT'), ('EnergyPrioritySelection', 'BatteryFirst'),
    ('SolarSell', 'false'), ('MaxSolarSellPower', '12000'), ('MaxSellPower', '100'),
    ('ZeroExportLimit', '20'), ('ForceOffGrid', 'false'),
    ('GridPeakShavingEnabled', 'false'), ('GridPeakShavingLimitPower', '2000'),
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
    ('SmartLoadEnabled', 'false'), ('SmartLoadOffSOC', '95'), ('SmartLoadOnSOC', '100'),
    ('SmartLoadPVPowerThreshold', '0'), ('SmartLoadOpenDelay', '1'), ('SmartLoadOnGridAlwaysOn', 'false')
) AS v(param_name, value)
WHERE p."Name" = v.param_name;


-- =====================================================================
-- VERIFICATION
-- =====================================================================
SELECT 'ParameterGroups' AS "Entity", COUNT(*) AS "Count" FROM "ParameterGroups"
UNION ALL SELECT 'Parameters', COUNT(*) FROM "Parameters"
UNION ALL SELECT 'ParameterValueSettingSchemes', COUNT(*) FROM "ParameterValueSettingSchemes";
