-- =============================================
-- Device Notification Types Seed Data
-- This script populates the DeviceNotificationTypes table with real alarm and notification data
-- from Inverter (Solenux LHG8K-P1LV-EU) and Battery (PYLON) devices
-- =============================================

-- Disable foreign key checks temporarily
SET session_replication_role = 'replica';

-- Clear existing data
TRUNCATE TABLE "DeviceNotificationTypes" RESTART IDENTITY CASCADE;

-- =============================================
-- INVERTER NOTIFICATIONS (INV)
-- Source: Solenux LHG8K-P1LV-EU User Manual - Fault Information and Processing
-- =============================================

-- Inverter Errors (ERR)
INSERT INTO "DeviceNotificationTypes" ("Id", "Code", "Message", "Status", "Description", "Register", "Value", "Category", "Version", "IsActive", "Created", "CreatedBy")
VALUES
-- Critical Faults
(gen_random_uuid(), 'INV-ERR-F08', 'Grid Relay Failure', 'Grid Relay Failure', 'When inverter is in Split phase (120/240Vac) or three-phase (120/208Vac) system, the backup load port N line needs to connect ground. If the fault still exists, please contact us for help.', 555, 8, 'ERROR', '1.0', true, NOW(), NULL),
(gen_random_uuid(), 'INV-ERR-F18', 'AC Over Current Fault', 'AC Over Current Fault of Hardware', 'AC side over current fault. 1. Please check whether the backup load power and common load power are within the range. 2. Restart and check whether it is in normal. 3. Seek help from us, if can not go back to normal state.', 555, 18, 'ERROR', '1.0', true, NOW(), NULL),
(gen_random_uuid(), 'INV-ERR-F20', 'DC Over Current Fault', 'DC Over Current Fault of the Hardware', 'DC side over current fault. 1. Check PV module connect and battery connect. 2. If inverter startup with big power load, it may report F20. Please reduce the load power connected. 3. Turn off the DC switch and AC switch and then wait one minute, then turn on the DC/AC switch again. 4. Seek help from us, if can not go back to normal state.', 556, 20, 'ERROR', '1.0', true, NOW(), NULL),
(gen_random_uuid(), 'INV-ERR-F22', 'TZ_EmergeStop_Fault', 'TZ Emergency Stop Fault', 'Please contact your installer for help.', 556, 22, 'ERROR', '1.0', true, NOW(), NULL),
(gen_random_uuid(), 'INV-ERR-F23', 'AC Leakage Current', 'AC Leakage Current is Transient Over Current', 'Leakage current fault. 1. Check PV side cable ground connection. 2. Restart the system 2-3 times. 3. If the fault still exists, please contact us for help.', 556, 23, 'ERROR', '1.0', true, NOW(), NULL),
(gen_random_uuid(), 'INV-ERR-F24', 'DC Insulation Impedance Failure', 'DC Insulation Impedance Failure', 'PV isolation resistance is too low. 1. Check the connection of PV panels and inverter is firmly and correctly. 2. Check whether the PE cable of inverter is connected to ground. 3. Seek help from us, if can not go back to normal state.', 557, 24, 'ERROR', '1.0', true, NOW(), NULL),
(gen_random_uuid(), 'INV-ERR-F26', 'DC Busbar Unbalanced', 'The DC Busbar is Unbalanced', 'DC bus bar voltage imbalance. 1. Please wait for a while and check whether it is normal. 2. When the battery voltage mode, and the load of L1 and load of L2 is big different, it will report the F26. 3. Restart the system 2-3 times. 4. Seek help from us, if can not go back to normal state.', 557, 26, 'ERROR', '1.0', true, NOW(), NULL),
(gen_random_uuid(), 'INV-ERR-F29', 'Parallel CANBus Fault', 'Parallel CANBus Fault', 'Parallel mode communication fault. 1. When in parallel mode, check the parallel communication cable connection and hybrid inverter communication address setting. 2. During the parallel system startup period, inverters will report F29 when all inverters are in ON status, it will disappear automatically. If the fault still exists, please contact us for help.', 557, 29, 'ERROR', '1.0', true, NOW(), NULL),
(gen_random_uuid(), 'INV-ERR-F34', 'AC Overcurrent Fault', 'AC Overcurrent Fault', 'Check the backup load connected, make sure it is in allowed power range. If the fault still exists, please contact us for help.', 558, 34, 'ERROR', '1.0', true, NOW(), NULL),
(gen_random_uuid(), 'INV-ERR-F35', 'No AC Grid', 'No AC Grid', 'No utility. 1. Please confirm grid is lost or not. 2. Check the grid connection is good or not. 3. Check the switch between inverter and grid is on or not. 4. Seek help from us, if can not go back to normal state.', 558, 35, 'ERROR', '1.0', true, NOW(), NULL),
(gen_random_uuid(), 'INV-ERR-F41', 'Parallel System Stop', 'Parallel System Stop', 'Hybrid inverter working status check. If there is 1 pcs hybrid inverter is in OFF status, the other hybrid inverters may report F41 fault in parallel system. If the fault still exists, please contact us for help.', 559, 41, 'ERROR', '1.0', true, NOW(), NULL),
(gen_random_uuid(), 'INV-ERR-F42', 'AC Line Low Voltage', 'AC Line Low Voltage', 'Grid voltage fault. 1. Check the AC voltage is in the range of standard voltage in specification. 2. Check whether grid AC cables are firmly and correctly connected. 3. Seek help from us, if can not go back to normal state.', 559, 42, 'ERROR', '1.0', true, NOW(), NULL),
(gen_random_uuid(), 'INV-ERR-F47', 'AC Over Frequency', 'AC Over Frequency', 'Grid frequency out of range. 1. Check the frequency is in the range of specification or not. 2. Check whether AC cables are firmly and correctly connected. 3. Seek help from us, if can not go back to normal state.', 559, 47, 'ERROR', '1.0', true, NOW(), NULL),
(gen_random_uuid(), 'INV-ERR-F48', 'AC Lower Frequency', 'AC Lower Frequency', 'Grid frequency out of range. 1. Check the frequency is in the range of specification or not. 2. Check whether AC cables are firmly and correctly connected. 3. Seek help from us, if can not go back to normal state.', 559, 48, 'ERROR', '1.0', true, NOW(), NULL),
(gen_random_uuid(), 'INV-ERR-F56', 'DC Busbar Voltage Too Low', 'DC Busbar Voltage is Too Low', 'Battery voltage low. 1. Check whether battery voltage is too low. 2. If the battery voltage is too low, using PV or grid to charge the battery. 3. Seek help from us, if can not go back to normal state.', 560, 56, 'ERROR', '1.0', true, NOW(), NULL),
(gen_random_uuid(), 'INV-ERR-F58', 'BMS Communication Fault', 'BMS Communication Fault', 'BMS communication fault. 1. It tells the communication between hybrid inverter and battery BMS disconnected when "BMS_Err-Stop" is active. 2. If don''t want to see this happen, you can disable "BMS_Err-Stop" item on the LCD. 3. If the fault still exists, please contact us for help.', 560, 58, 'ERROR', '1.0', true, NOW(), NULL),
(gen_random_uuid(), 'INV-ERR-F63', 'ARC Fault', 'ARC Fault', 'ARC fault detection is only for US market. 1. Check PV module cable connection and clear the fault. 2. Seek help from us, if can not go back to normal state.', 560, 63, 'ERROR', '1.0', true, NOW(), NULL),
(gen_random_uuid(), 'INV-ERR-F64', 'Heat Sink High Temperature', 'Heat Sink High Temperature Failure', 'Heat sink temperature is too high. 1. Check whether the work environment temperature is too high. 2. Turn off the inverter for 10mins and restart. 3. Seek help from us, if can not go back to normal state.', 560, 64, 'ERROR', '1.0', true, NOW(), NULL);

-- Inverter Warnings (WARN)
INSERT INTO "DeviceNotificationTypes" ("Id", "Code", "Message", "Status", "Description", "Register", "Value", "Category", "Version", "IsActive", "Created", "CreatedBy")
VALUES
(gen_random_uuid(), 'INV-WARN-F13', 'Working Mode Change', 'Working Mode Change', 'When the grid type and frequency changed it will report F13. When the battery mode was changed to "No battery" mode, it will report F13. For some old FW version, it will report F13 when the system work mode changed. Generally, it will disappear automatically when shows F13. If still same, and turn off the DC switch and AC switch and wait for one minute and then turn on the DC/AC switch. Seek help from us, if can not go back to normal state.', 555, 13, 'WARNING', '1.0', true, NOW(), NULL);

-- =============================================
-- INVERTER NODE-RED DECODED ALARMS
-- Source: Node-RED flow alarm decoding logic
-- These are generic alarm codes used by the IoT gateway
-- =============================================

-- Node-RED Fault Codes (ERR)
INSERT INTO "DeviceNotificationTypes" ("Id", "Code", "Message", "Status", "Description", "Register", "Value", "Category", "Version", "IsActive", "Created", "CreatedBy")
VALUES
(gen_random_uuid(), 'INV-ERR-DC-VOLTAGE-HIGH', 'DC Voltage High', 'DC Input Voltage Too High', 'DC input voltage from PV or battery has exceeded safe operating limits. Check PV string voltage and battery voltage.', 655, 0, 'ERROR', '1.0', true, NOW(), NULL),
(gen_random_uuid(), 'INV-ERR-BUS-VOLTAGE-HIGH', 'Bus Voltage High', 'DC Bus Voltage Too High', 'Internal DC bus voltage is too high. This may indicate a power regulation issue.', 655, 1, 'ERROR', '1.0', true, NOW(), NULL),
(gen_random_uuid(), 'INV-ERR-BUS-VOLTAGE-LOW', 'Bus Voltage Low', 'DC Bus Voltage Too Low', 'Internal DC bus voltage is too low. Check battery voltage and PV input.', 655, 2, 'ERROR', '1.0', true, NOW(), NULL),
(gen_random_uuid(), 'INV-ERR-BUS-VOLTAGE-UNBALANCED', 'Bus Voltage Unbalanced', 'DC Bus Voltage Imbalance Detected', 'DC bus voltage is unbalanced between positive and negative rails. This may affect three-phase output quality.', 655, 3, 'ERROR', '1.0', true, NOW(), NULL),
(gen_random_uuid(), 'INV-ERR-GRID-VOLTAGE-HIGH', 'Grid Voltage High', 'AC Grid Voltage Too High', 'Grid voltage exceeds upper limit. Inverter will disconnect from grid for safety.', 655, 4, 'ERROR', '1.0', true, NOW(), NULL),
(gen_random_uuid(), 'INV-ERR-GRID-VOLTAGE-LOW', 'Grid Voltage Low', 'AC Grid Voltage Too Low', 'Grid voltage below minimum threshold. Check grid connection and utility supply.', 655, 5, 'ERROR', '1.0', true, NOW(), NULL),
(gen_random_uuid(), 'INV-ERR-GRID-FREQ-HIGH', 'Grid Frequency High', 'AC Grid Frequency Too High', 'Grid frequency exceeds upper limit. Inverter will disconnect from grid per safety standards.', 655, 6, 'ERROR', '1.0', true, NOW(), NULL),
(gen_random_uuid(), 'INV-ERR-GRID-FREQ-LOW', 'Grid Frequency Low', 'AC Grid Frequency Too Low', 'Grid frequency below minimum threshold. Check utility grid stability.', 655, 7, 'ERROR', '1.0', true, NOW(), NULL),
(gen_random_uuid(), 'INV-ERR-GRID-DISCONNECT', 'Grid Disconnected', 'AC Grid Connection Lost', 'No grid detected. Inverter will operate in off-grid mode if configured. Check grid connection and breakers.', 655, 8, 'ERROR', '1.0', true, NOW(), NULL),
(gen_random_uuid(), 'INV-ERR-OUTPUT-CURRENT-HIGH', 'Output Current High', 'Output Current Exceeds Limit', 'Inverter output current is too high. Reduce load or check for short circuit conditions.', 655, 9, 'ERROR', '1.0', true, NOW(), NULL),
(gen_random_uuid(), 'INV-ERR-OVER-TEMP', 'Inverter Over Temperature', 'Inverter Temperature Critical', 'Inverter internal temperature has reached critical level. Check cooling system and ambient temperature.', 655, 10, 'ERROR', '1.0', true, NOW(), NULL),
(gen_random_uuid(), 'INV-ERR-LEAKAGE-CURRENT', 'Leakage Current High', 'AC Leakage Current Detected', 'Excessive leakage current detected. Check AC wiring insulation and ground connection.', 655, 11, 'ERROR', '1.0', true, NOW(), NULL),
(gen_random_uuid(), 'INV-ERR-HARDWARE-FAULT', 'Hardware Fault', 'Internal Hardware Failure', 'Internal hardware component failure detected. Contact technical support.', 655, 12, 'ERROR', '1.0', true, NOW(), NULL),
(gen_random_uuid(), 'INV-ERR-EEPROM-FAULT', 'EEPROM Fault', 'EEPROM Read/Write Error', 'Cannot read or write to internal EEPROM memory. Configuration may be lost. Contact support.', 655, 13, 'ERROR', '1.0', true, NOW(), NULL),
(gen_random_uuid(), 'INV-ERR-RELAY-FAULT', 'Relay Fault', 'Internal Relay Failure', 'One or more internal relays failed to operate correctly. Check relay contacts.', 655, 14, 'ERROR', '1.0', true, NOW(), NULL),
(gen_random_uuid(), 'INV-ERR-DSP-FAULT', 'DSP Communication Fault', 'DSP Processor Communication Error', 'Communication failure between main controller and DSP processor. Restart inverter or contact support.', 655, 15, 'ERROR', '1.0', true, NOW(), NULL);

-- Node-RED Warning Codes (WARN)
INSERT INTO "DeviceNotificationTypes" ("Id", "Code", "Message", "Status", "Description", "Register", "Value", "Category", "Version", "IsActive", "Created", "CreatedBy")
VALUES
(gen_random_uuid(), 'INV-WARN-BATTERY-VOLTAGE-HIGH', 'Battery Voltage High', 'Battery Voltage Approaching Limit', 'Battery voltage is approaching upper limit. Charging will be reduced or stopped.', 653, 0, 'WARNING', '1.0', true, NOW(), NULL),
(gen_random_uuid(), 'INV-WARN-BATTERY-VOLTAGE-LOW', 'Battery Voltage Low', 'Battery Voltage Low Warning', 'Battery voltage is low. System may switch to grid or reduce discharge power.', 653, 1, 'WARNING', '1.0', true, NOW(), NULL),
(gen_random_uuid(), 'INV-WARN-BATTERY-COMM-FAIL', 'Battery Communication Failure', 'BMS Communication Lost', 'Communication with battery management system failed. Battery protection may be limited.', 653, 2, 'WARNING', '1.0', true, NOW(), NULL),
(gen_random_uuid(), 'INV-WARN-TEMP-HIGH', 'Temperature High', 'Inverter Temperature Warning', 'Inverter temperature is elevated. System may reduce power to prevent overheating.', 653, 3, 'WARNING', '1.0', true, NOW(), NULL),
(gen_random_uuid(), 'INV-WARN-PV-VOLTAGE-HIGH', 'PV Voltage High', 'PV Input Voltage Warning', 'PV input voltage is approaching limit. Check PV string configuration.', 653, 4, 'WARNING', '1.0', true, NOW(), NULL),
(gen_random_uuid(), 'INV-WARN-FAN-LOCKED', 'Fan Locked', 'Cooling Fan Malfunction', 'Cooling fan is not operating properly. Check for obstructions or fan motor failure.', 653, 5, 'WARNING', '1.0', true, NOW(), NULL),
(gen_random_uuid(), 'INV-WARN-FEED-IN-POWER-LIMIT', 'Feed-in Power Limited', 'Grid Feed-in Power Limit Active', 'Export power to grid is being limited per configuration or grid requirements.', 653, 6, 'WARNING', '1.0', true, NOW(), NULL),
(gen_random_uuid(), 'INV-WARN-FIRMWARE-UPDATE', 'Firmware Update Available', 'Firmware Update Notification', 'A firmware update is available or in progress. System performance may be temporarily affected.', 653, 7, 'WARNING', '1.0', true, NOW(), NULL),
(gen_random_uuid(), 'INV-WARN-PHASE-IMBALANCE', 'Phase Imbalance', 'Three-Phase Load Imbalance', 'Significant imbalance detected between three-phase loads. Balance loads across phases for optimal performance.', 653, 8, 'WARNING', '1.0', true, NOW(), NULL),
(gen_random_uuid(), 'INV-WARN-PHASE-VOLTAGE-UNBALANCED', 'Phase Voltage Unbalanced', 'Three-Phase Voltage Imbalance', 'Voltage imbalance detected between three phases. Check grid connection and phase wiring.', 653, 9, 'WARNING', '1.0', true, NOW(), NULL);

-- =============================================
-- BATTERY NOTIFICATIONS (BAT)
-- Source: PYLON CAN Bus Protocol V1.2
-- =============================================

-- Battery Protection Errors (ERR) - Critical protection triggers
INSERT INTO "DeviceNotificationTypes" ("Id", "Code", "Message", "Status", "Description", "Register", "Value", "Category", "Version", "IsActive", "Created", "CreatedBy")
VALUES
(gen_random_uuid(), 'BAT-ERR-P00', 'Cell Over Voltage Protect', 'Cell/Module Over Voltage Protection', 'Battery cell or module voltage has exceeded safe limits and protection has been triggered. Charging must be stopped immediately.', 857, 1, 'ERROR', '1.0', true, NOW(), NULL),
(gen_random_uuid(), 'BAT-ERR-P01', 'Cell Under Voltage Protect', 'Cell/Module Under Voltage Protection', 'Battery cell or module voltage has dropped below safe limits and protection has been triggered. Discharging must be stopped immediately.', 857, 2, 'ERROR', '1.0', true, NOW(), NULL),
(gen_random_uuid(), 'BAT-ERR-P02', 'Cell Over Temperature Protect', 'Cell Over Temperature Protection', 'Battery cell temperature has exceeded safe operating limits. System must be shut down to prevent thermal damage.', 857, 3, 'ERROR', '1.0', true, NOW(), NULL),
(gen_random_uuid(), 'BAT-ERR-P03', 'Cell Under Temperature Protect', 'Cell Under Temperature Protection', 'Battery cell temperature is below safe operating limits. Charging and discharging operations are suspended.', 857, 4, 'ERROR', '1.0', true, NOW(), NULL),
(gen_random_uuid(), 'BAT-ERR-P04', 'Charge Over Current Protect', 'Charge Over Current Protection', 'Charging current has exceeded maximum safe limits. Charging circuit has been opened to protect battery.', 857, 8, 'ERROR', '1.0', true, NOW(), NULL),
(gen_random_uuid(), 'BAT-ERR-P05', 'Discharge Over Current Protect', 'Discharge Over Current Protection', 'Discharge current has exceeded maximum safe limits. Discharge circuit has been opened to protect battery.', 857, 7, 'ERROR', '1.0', true, NOW(), NULL),
(gen_random_uuid(), 'BAT-ERR-P06', 'System Error', 'BMS System Error', 'Internal battery management system error detected. This indicates a fault in the BMS hardware or software.', 857, 11, 'ERROR', '1.0', true, NOW(), NULL);

-- Battery Alarms (ALARM) - Warning conditions before protection triggers
INSERT INTO "DeviceNotificationTypes" ("Id", "Code", "Message", "Status", "Description", "Register", "Value", "Category", "Version", "IsActive", "Created", "CreatedBy")
VALUES
(gen_random_uuid(), 'BAT-ALARM-A00', 'Cell High Voltage Alarm', 'Cell/Module High Voltage Alarm', 'Battery cell or module voltage is approaching upper limit. Reduce charging current or stop charging to prevent over-voltage protection.', 857, 16, 'ALARM', '1.0', true, NOW(), NULL),
(gen_random_uuid(), 'BAT-ALARM-A01', 'Cell Low Voltage Alarm', 'Cell/Module Low Voltage Alarm', 'Battery cell or module voltage is approaching lower limit. Reduce discharge current or stop discharging to prevent under-voltage protection.', 857, 17, 'ALARM', '1.0', true, NOW(), NULL),
(gen_random_uuid(), 'BAT-ALARM-A02', 'Cell High Temperature Alarm', 'Cell High Temperature Alarm', 'Battery cell temperature is approaching upper operating limit. Reduce load or improve cooling to prevent over-temperature protection.', 857, 18, 'ALARM', '1.0', true, NOW(), NULL),
(gen_random_uuid(), 'BAT-ALARM-A03', 'Cell Low Temperature Alarm', 'Cell Low Temperature Alarm', 'Battery cell temperature is approaching lower operating limit. Ambient temperature may be too low for safe operation.', 857, 19, 'ALARM', '1.0', true, NOW(), NULL),
(gen_random_uuid(), 'BAT-ALARM-A04', 'Charge High Current Alarm', 'Charge High Current Alarm', 'Charging current is approaching maximum safe limit. Reduce charging power to prevent over-current protection.', 857, 24, 'ALARM', '1.0', true, NOW(), NULL),
(gen_random_uuid(), 'BAT-ALARM-A05', 'Discharge High Current Alarm', 'Discharge High Current Alarm', 'Discharge current is approaching maximum safe limit. Reduce load to prevent over-current protection.', 857, 23, 'ALARM', '1.0', true, NOW(), NULL),
(gen_random_uuid(), 'BAT-ALARM-A06', 'Internal Communication Fail', 'Slave Communication Offline', 'Communication with one or more battery modules or parallel battery groups has been lost. Check module connections.', 857, 27, 'ALARM', '1.0', true, NOW(), NULL);

-- Battery Info Notifications (INFO) - Normal operational status
INSERT INTO "DeviceNotificationTypes" ("Id", "Code", "Message", "Status", "Description", "Register", "Value", "Category", "Version", "IsActive", "Created", "CreatedBy")
VALUES
(gen_random_uuid(), 'BAT-INFO-I00', 'Charge Voltage Limit', 'Battery Charge Voltage Limit', 'Maximum charging voltage currently accepted by battery. This value is dynamically adjusted by BMS based on battery state.', 849, 0, 'INFO', '1.0', true, NOW(), NULL),
(gen_random_uuid(), 'BAT-INFO-I01', 'Charge Current Limit', 'Battery Charge Current Limit', 'Maximum charging current currently accepted by battery. This value is dynamically adjusted by BMS based on battery state and temperature.', 849, 1, 'INFO', '1.0', true, NOW(), NULL),
(gen_random_uuid(), 'BAT-INFO-I02', 'Discharge Current Limit', 'Battery Discharge Current Limit', 'Maximum discharge current battery can provide. This value is dynamically adjusted by BMS based on battery state and temperature.', 849, 2, 'INFO', '1.0', true, NOW(), NULL),
(gen_random_uuid(), 'BAT-INFO-I03', 'Discharge Voltage Limit', 'Battery Discharge Voltage Limit', 'Minimum discharge voltage allowed by battery. Discharge should stop when battery voltage reaches this limit.', 849, 3, 'INFO', '1.0', true, NOW(), NULL),
(gen_random_uuid(), 'BAT-INFO-I04', 'SOC', 'Battery State of Charge', 'Current battery charge level as a percentage (0-100%). This indicates how much energy remains in the battery.', 853, 0, 'INFO', '1.0', true, NOW(), NULL),
(gen_random_uuid(), 'BAT-INFO-I05', 'SOH', 'Battery State of Health', 'Battery health status as a percentage (0-100%). This indicates the overall condition and remaining lifespan of the battery.', 853, 1, 'INFO', '1.0', true, NOW(), NULL),
(gen_random_uuid(), 'BAT-INFO-I06', 'Average Module Voltage', 'Average Module Voltage', 'Average voltage across all battery modules in the pack. Used for monitoring overall pack voltage balance.', 854, 0, 'INFO', '1.0', true, NOW(), NULL),
(gen_random_uuid(), 'BAT-INFO-I07', 'Total Current', 'Battery Total Current', 'Total current flowing into (+) or out of (-) the battery pack. Positive values indicate charging, negative values indicate discharging.', 854, 1, 'INFO', '1.0', true, NOW(), NULL),
(gen_random_uuid(), 'BAT-INFO-I08', 'Average Cell Temperature', 'Average Cell Temperature', 'Average temperature across all battery cells in degrees Celsius. Critical for monitoring thermal management.', 854, 2, 'INFO', '1.0', true, NOW(), NULL),
(gen_random_uuid(), 'BAT-INFO-I09', 'Full Charge Request', 'BMS Full Charge Request', 'BMS is requesting a full charge cycle. This typically occurs when SOC hasn''t reached 97% in the past 30 days to recalibrate SOC calculation.', 860, 3, 'INFO', '1.0', true, NOW(), NULL),
(gen_random_uuid(), 'BAT-INFO-I10', 'Force Charge Request', 'BMS Force Charge Request', 'Battery is at low capacity and requires immediate charging. This is a critical request to prevent deep discharge damage.', 860, 4, 'INFO', '1.0', true, NOW(), NULL),
(gen_random_uuid(), 'BAT-INFO-I11', 'Discharge Enable', 'Discharge Enable Status', 'Indicates whether discharge is permitted (1) or must be stopped (0). Controlled by BMS based on battery protection status.', 860, 6, 'INFO', '1.0', true, NOW(), NULL),
(gen_random_uuid(), 'BAT-INFO-I12', 'Charge Enable', 'Charge Enable Status', 'Indicates whether charging is permitted (1) or must be stopped (0). Controlled by BMS based on battery protection status.', 860, 7, 'INFO', '1.0', true, NOW(), NULL);

-- Re-enable foreign key checks
SET session_replication_role = 'origin';

-- Summary
DO $$
DECLARE
    total_count INTEGER;
    inv_count INTEGER;
    bat_count INTEGER;
BEGIN
    SELECT COUNT(*) INTO total_count FROM "DeviceNotificationTypes";
    SELECT COUNT(*) INTO inv_count FROM "DeviceNotificationTypes" WHERE "Code" LIKE 'INV-%';
    SELECT COUNT(*) INTO bat_count FROM "DeviceNotificationTypes" WHERE "Code" LIKE 'BAT-%';

    RAISE NOTICE '========================================';
    RAISE NOTICE 'Device Notification Types Seed Complete';
    RAISE NOTICE '========================================';
    RAISE NOTICE 'Total notification types inserted: %', total_count;
    RAISE NOTICE 'Inverter notifications: %', inv_count;
    RAISE NOTICE 'Battery notifications: %', bat_count;
    RAISE NOTICE '========================================';
END $$;
