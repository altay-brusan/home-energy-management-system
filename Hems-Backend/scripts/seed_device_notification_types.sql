-- =====================================================
-- DeviceNotificationTypes Seed Script
-- Generated from inverter and BMS documentation
-- Template: [INV/BMS]-[SEVERITY_CODE]-[device's code]
-- Severity Codes: ALM (Alarm), ERR (Error), WAR (Warning)
-- Severity Values: ALARM, WARNING, ERROR
-- Category: Device region/component (e.g., fan, thermal, battery, grid, etc.)
-- =====================================================

-- Step 1: Disable foreign key checks
ALTER TABLE "DeviceNotifications" DISABLE TRIGGER ALL;

-- Step 2: Delete all existing notification types
DELETE FROM "DeviceNotifications";
DELETE FROM "DeviceNotificationTypes";

-- Step 3: Re-enable foreign key checks
ALTER TABLE "DeviceNotifications" ENABLE TRIGGER ALL;

-- Step 4: Insert new DeviceNotificationTypes
-- Using gen_random_uuid() for PostgreSQL UUID generation
-- Manufacturer: LOFTTY (Three-phase hybrid inverter and BMS)

-- =====================================================
-- INVERTER ALARMS (INV-ALM-Fxx)
-- Severity: ALARM - Critical alarms that require immediate attention
-- =====================================================

INSERT INTO "DeviceNotificationTypes" ("Id", "Code", "Message", "Status", "Description", "Register", "Value", "Category", "Severity", "Version", "Manufacturer", "IsActive", "Created", "CreatedBy", "LastModified", "LastModifiedBy")
VALUES
-- F01: DC input polarity reverse fault
(gen_random_uuid(), 'INV-ALM-F01', 'DC Input Polarity Reverse Fault', 'ACTIVE', 'Check the PV input polarity. The positive and negative terminals may be reversed.', 0, 0, 'dc-input', 'ALARM', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F07: DC_START_Failure
(gen_random_uuid(), 'INV-ALM-F07', 'DC Start Failure', 'ACTIVE', 'The BUS voltage cannot be built from PV or battery. Restart the inverter. If the fault still exists, please contact support.', 0, 0, 'startup', 'ALARM', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F08: GFDI_Relay_Failure
(gen_random_uuid(), 'INV-ALM-F08', 'GFDI Relay Failure', 'ACTIVE', 'When inverter is in Split phase or three-phase system, the backup load port N line needs to connect ground. If the fault still exists, please contact support.', 0, 0, 'relay', 'ALARM', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F13: Working mode change
(gen_random_uuid(), 'INV-ALM-F13', 'Working Mode Change', 'ACTIVE', 'Grid type and frequency changed or battery mode was changed to No battery mode. Generally, it will disappear automatically. If still same, turn off DC and AC switch and wait one minute then turn on again.', 0, 0, 'mode-change', 'ALARM', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F15: AC over current fault of software
(gen_random_uuid(), 'INV-ALM-F15', 'AC Over Current Fault (Software)', 'ACTIVE', 'AC side over current fault. Check whether the backup load power and common load power are within the range. Restart and check whether it is normal.', 0, 0, 'ac-current', 'ALARM', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F16: AC leakage current fault
(gen_random_uuid(), 'INV-ALM-F16', 'AC Leakage Current Fault', 'ACTIVE', 'Leakage current fault. Check the PV side cable ground connection. Restart the system 2-3 times. If the fault still exists, please contact support.', 0, 0, 'leakage', 'ALARM', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F18: AC over current fault of hardware
(gen_random_uuid(), 'INV-ALM-F18', 'AC Over Current Fault (Hardware)', 'ACTIVE', 'AC side over current fault. Check whether the backup load power and common load power are within the range. Restart and check whether it is normal.', 0, 0, 'ac-current', 'ALARM', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F20: DC over current fault of the hardware
(gen_random_uuid(), 'INV-ALM-F20', 'DC Over Current Fault (Hardware)', 'ACTIVE', 'DC side over current fault. Check PV module connect and battery connect. When in off-grid mode, reduce load power connected. Turn off DC and AC switch and wait one minute then turn on again.', 0, 0, 'dc-current', 'ALARM', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F21: Tz_HV_Overcurr_fault (BUS over current)
(gen_random_uuid(), 'INV-ALM-F21', 'BUS Over Current Fault', 'ACTIVE', 'BUS over current. Check the PV input current and battery current setting. Please check each battery status such as voltage/SOC and make sure all the parameters are same.', 0, 0, 'bus', 'ALARM', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F22: Tz_EmergStop_Fault
(gen_random_uuid(), 'INV-ALM-F22', 'Emergency Stop Fault', 'ACTIVE', 'Remotely shutdown. The inverter is remotely controlled.', 0, 0, 'remote-control', 'ALARM', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F23: DC insulation failure / GFCI transient over current
(gen_random_uuid(), 'INV-ALM-F23', 'DC Insulation Failure', 'ACTIVE', 'PV isolation resistance is too low. Check the connection of PV panels and inverter is firmly and correctly. Check whether the PE cable of inverter is connected to ground.', 0, 0, 'dc-insulation', 'ALARM', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F24: DC insulation impedance failure
(gen_random_uuid(), 'INV-ALM-F24', 'DC Insulation Impedance Failure', 'ACTIVE', 'PV isolation resistance is too low. Check the connection of PV panels and inverter is firmly and correctly. Check whether the PE cable of inverter is connected to ground.', 0, 0, 'dc-insulation', 'ALARM', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F26: The DC busbar is unbalanced
(gen_random_uuid(), 'INV-ALM-F26', 'DC Busbar Unbalanced', 'ACTIVE', 'Please wait for a while and check whether it is normal. When the hybrid in split phase mode and the load of L1 and load of L2 is big different, it will report F26. When there is DC leakage current, it will report F26.', 0, 0, 'bus', 'ALARM', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F29: Parallel CANBus fault
(gen_random_uuid(), 'INV-ALM-F29', 'Parallel CAN Bus Fault', 'ACTIVE', 'When in parallel mode, check the parallel communication cable connection and hybrid inverter communication address setting. During the parallel system startup period, inverters will report F29. When all inverters are in ON status, it will disappear automatically.', 0, 0, 'parallel', 'ALARM', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F34: AC Overcurrent fault
(gen_random_uuid(), 'INV-ALM-F34', 'AC Over Current Fault', 'ACTIVE', 'Check the backup load connected, make sure it is in allowed power range. If the fault still exists, please contact support.', 0, 0, 'ac-current', 'ALARM', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F35: No AC grid
(gen_random_uuid(), 'INV-ALM-F35', 'No AC Grid', 'ACTIVE', 'No Utility. Please confirm grid is lost or not. Check the grid connection is good or not. Check the switch between inverter and grid is on or not.', 0, 0, 'grid', 'ALARM', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F41: Parallel system stop
(gen_random_uuid(), 'INV-ALM-F41', 'Parallel System Stop', 'ACTIVE', 'Check the hybrid inverter working status. If there is 1 pcs hybrid inverter is in OFF status, the other hybrid inverters may report F41 fault. If the fault still exists, please contact support.', 0, 0, 'parallel', 'ALARM', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F42: AC line low voltage
(gen_random_uuid(), 'INV-ALM-F42', 'AC Line Low Voltage', 'ACTIVE', 'Grid voltage fault. Check the AC voltage is in the range of standard voltage in specification. Check whether grid AC cables are firmly and correctly connected.', 0, 0, 'grid', 'ALARM', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F47: AC over frequency
(gen_random_uuid(), 'INV-ALM-F47', 'AC Over Frequency', 'ACTIVE', 'Grid frequency out of range. Check the frequency is in the range of specification or not. Check whether AC cables are firmly and correctly connected.', 0, 0, 'grid', 'ALARM', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F48: AC lower frequency
(gen_random_uuid(), 'INV-ALM-F48', 'AC Lower Frequency', 'ACTIVE', 'Grid frequency out of range. Check the frequency is in the range of specification or not. Check whether AC cables are firmly and correctly connected.', 0, 0, 'grid', 'ALARM', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F55: DC busbar voltage is too high
(gen_random_uuid(), 'INV-ALM-F55', 'DC Busbar Voltage Too High', 'ACTIVE', 'BUS voltage is too high. Check whether battery voltage is too high. Check the PV input voltage, make sure it is within the allowed range.', 0, 0, 'bus', 'ALARM', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F56: DC busbar voltage is too low
(gen_random_uuid(), 'INV-ALM-F56', 'DC Busbar Voltage Too Low', 'ACTIVE', 'Battery voltage low. Check whether battery voltage is too low. If the battery voltage is too low, using PV or grid to charge the battery.', 0, 0, 'bus', 'ALARM', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F58: BMS communication fault
(gen_random_uuid(), 'INV-ALM-F58', 'BMS Communication Fault', 'ACTIVE', 'Communication between hybrid inverter and battery BMS disconnected when BMS_Err-Stop is active. If you do not want to see this happen, you can disable BMS_Err-Stop item on the LCD.', 0, 0, 'bms', 'ALARM', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F62: DRMs0_stop
(gen_random_uuid(), 'INV-ALM-F62', 'DRM Stop', 'ACTIVE', 'The DRM function is for Australia market only. Check the DRM function is active or not.', 0, 0, 'drm', 'ALARM', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F63: ARC fault
(gen_random_uuid(), 'INV-ALM-F63', 'ARC Fault', 'ACTIVE', 'ARC fault detection is only for US market. Check PV module cable connection and clear the fault.', 0, 0, 'arc', 'ALARM', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F64: Heat sink high temperature failure
(gen_random_uuid(), 'INV-ALM-F64', 'Heat Sink High Temperature', 'ACTIVE', 'Heat sink temperature is too high. Check whether the work environment temperature is too high. Turn off the inverter for 10 mins and restart.', 0, 0, 'thermal', 'ALARM', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- =====================================================
-- BMS PROTECTION FLAGS (BMS-ALM-Pxx)
-- Severity: ALARM - Critical protection events from battery management system
-- Based on CAN ID 0x359 Byte 0-1 (Protection flags)
-- =====================================================

-- P01: Cell or module over voltage protection (Byte 0, Bit 1)
(gen_random_uuid(), 'BMS-ALM-P01', 'Cell Over Voltage Protection', 'ACTIVE', 'Cell or module over voltage protection triggered. The battery system has detected voltage exceeding safe limits. Follow BMS suggested current limits.', 0, 0, 'battery-cell', 'ALARM', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- P02: Cell or module under voltage protection (Byte 0, Bit 2)
(gen_random_uuid(), 'BMS-ALM-P02', 'Cell Under Voltage Protection', 'ACTIVE', 'Cell or module under voltage protection triggered. The battery needs charging. Follow force charge flag to avoid triggering such protection.', 0, 0, 'battery-cell', 'ALARM', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- P03: Cell over temperature protection (Byte 0, Bit 3)
(gen_random_uuid(), 'BMS-ALM-P03', 'Cell Over Temperature Protection', 'ACTIVE', 'Cell over temperature protection triggered. Allow the battery to cool down before resuming operation.', 0, 0, 'battery-thermal', 'ALARM', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- P04: Cell under temperature protection (Byte 0, Bit 4)
(gen_random_uuid(), 'BMS-ALM-P04', 'Cell Under Temperature Protection', 'ACTIVE', 'Cell under temperature protection triggered. The battery is too cold for safe operation. Allow it to warm up.', 0, 0, 'battery-thermal', 'ALARM', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- P05: Discharge over current protection (Byte 0, Bit 7)
(gen_random_uuid(), 'BMS-ALM-P05', 'Discharge Over Current Protection', 'ACTIVE', 'Discharge over current protection triggered. Reduce the load to prevent battery damage.', 0, 0, 'battery-current', 'ALARM', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- P06: Charge over current protection (Byte 1, Bit 0)
(gen_random_uuid(), 'BMS-ALM-P06', 'Charge Over Current Protection', 'ACTIVE', 'Charge over current protection triggered. The charging current exceeds safe limits.', 0, 0, 'battery-current', 'ALARM', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- P07: System error (Byte 1, Bit 3)
(gen_random_uuid(), 'BMS-ALM-P07', 'BMS System Error', 'ACTIVE', 'BMS system error detected. Stop charge and discharge immediately. Need restart or troubleshooting. When flag is 0, operation can resume.', 0, 0, 'bms-system', 'ALARM', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- =====================================================
-- BMS ALARM FLAGS (BMS-WAR-Axx)
-- Severity: WARNING - Warning-level alarms from battery management system
-- Based on CAN ID 0x359 Byte 2-3 (Alarm flags)
-- =====================================================

-- A01: Cell or module high voltage alarm (Byte 2, Bit 1)
(gen_random_uuid(), 'BMS-WAR-A01', 'Cell High Voltage Alarm', 'ACTIVE', 'Cell or module high voltage alarm. Voltage is approaching limits but not yet critical. Monitor and follow BMS suggested current.', 0, 0, 'battery-cell', 'WARNING', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- A02: Cell or module low voltage alarm (Byte 2, Bit 2)
(gen_random_uuid(), 'BMS-WAR-A02', 'Cell Low Voltage Alarm', 'ACTIVE', 'Cell or module low voltage alarm. Battery charge level is low. Consider charging the battery soon.', 0, 0, 'battery-cell', 'WARNING', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- A03: Cell high temperature alarm (Byte 2, Bit 3)
(gen_random_uuid(), 'BMS-WAR-A03', 'Cell High Temperature Alarm', 'ACTIVE', 'Cell high temperature alarm. The battery is getting warm. Reduce load or improve ventilation.', 0, 0, 'battery-thermal', 'WARNING', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- A04: Cell low temperature alarm (Byte 2, Bit 4)
(gen_random_uuid(), 'BMS-WAR-A04', 'Cell Low Temperature Alarm', 'ACTIVE', 'Cell low temperature alarm. The battery is getting cold. Operation may be limited in cold conditions.', 0, 0, 'battery-thermal', 'WARNING', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- A05: Discharge high current alarm (Byte 2, Bit 7)
(gen_random_uuid(), 'BMS-WAR-A05', 'Discharge High Current Alarm', 'ACTIVE', 'Discharge high current alarm. The discharge current is approaching limits. Consider reducing load.', 0, 0, 'battery-current', 'WARNING', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- A06: Charge high current alarm (Byte 3, Bit 0)
(gen_random_uuid(), 'BMS-WAR-A06', 'Charge High Current Alarm', 'ACTIVE', 'Charge high current alarm. The charging current is approaching limits.', 0, 0, 'battery-current', 'WARNING', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- A07: Slave battery/group communication offline (Byte 3, Bit 3)
(gen_random_uuid(), 'BMS-WAR-A07', 'Slave Battery Communication Offline', 'ACTIVE', 'Slave battery or slave group communication offline. This may be due to low capacity, error, or lost communication connection. First try to charge the system, if not working then restart or troubleshoot.', 0, 0, 'battery-communication', 'WARNING', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- =====================================================
-- BMS ERROR FLAGS (BMS-ERR-Exx)
-- Severity: ERROR - Error-level events from extended BMS protocol (0x35A)
-- =====================================================

-- E01: Contactor/MOSFET error
(gen_random_uuid(), 'BMS-ERR-E01', 'Contactor/MOSFET Error', 'ACTIVE', 'One or more battery modules have MOSFET failure or relay error. Stop charge and discharge. Troubleshooting required.', 0, 0, 'battery-hardware', 'ERROR', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- E02: Short circuit protection
(gen_random_uuid(), 'BMS-ERR-E02', 'Short Circuit Protection', 'ACTIVE', 'One or more battery modules experienced short circuit protection. Immediate inspection required.', 0, 0, 'battery-protection', 'ERROR', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- E03: BMS hardware/sensor error
(gen_random_uuid(), 'BMS-ERR-E03', 'BMS Hardware Error', 'ACTIVE', 'BMS detected reversed connection, input overvoltage, hardware failure, sensor error, or internal BMS error. Professional service required.', 0, 0, 'bms-hardware', 'ERROR', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- =====================================================
-- INVERTER WARNINGS (INV-WAR-Wxx)
-- Severity: WARNING - Warning-level events from inverter
-- =====================================================

-- W01: Battery voltage high warning
(gen_random_uuid(), 'INV-WAR-W01', 'Battery Voltage High Warning', 'ACTIVE', 'Battery voltage is approaching high limit. Monitor the system.', 0, 0, 'battery', 'WARNING', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- W02: Battery voltage low warning
(gen_random_uuid(), 'INV-WAR-W02', 'Battery Voltage Low Warning', 'ACTIVE', 'Battery voltage is low. Consider charging the battery.', 0, 0, 'battery', 'WARNING', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- W03: Temperature high warning
(gen_random_uuid(), 'INV-WAR-W03', 'Temperature High Warning', 'ACTIVE', 'System temperature is getting high. Check ventilation and reduce load if necessary.', 0, 0, 'thermal', 'WARNING', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- W04: PV voltage high warning
(gen_random_uuid(), 'INV-WAR-W04', 'PV Voltage High Warning', 'ACTIVE', 'PV input voltage is approaching high limit. Check PV array configuration.', 0, 0, 'pv', 'WARNING', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- W05: Fan locked warning
(gen_random_uuid(), 'INV-WAR-W05', 'Fan Locked Warning', 'ACTIVE', 'Cooling fan may be locked or not functioning properly. Check fan operation.', 0, 0, 'fan', 'WARNING', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- W06: Feed-in power limit warning
(gen_random_uuid(), 'INV-WAR-W06', 'Feed-in Power Limit Warning', 'ACTIVE', 'Grid feed-in power limit is active. Export power is being limited.', 0, 0, 'grid-export', 'WARNING', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- W07: Phase imbalance warning
(gen_random_uuid(), 'INV-WAR-W07', 'Phase Imbalance Warning', 'ACTIVE', 'Three-phase imbalance detected. Check load distribution across phases.', 0, 0, 'phase', 'WARNING', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- =====================================================
-- INFORMATIONAL MESSAGES (INV-WAR-Ixx / BMS-WAR-Ixx)
-- Severity: WARNING - Informational status messages (low priority warnings)
-- =====================================================

-- I01: System startup
(gen_random_uuid(), 'INV-WAR-I01', 'System Startup', 'ACTIVE', 'Inverter system is starting up.', 0, 0, 'system', 'WARNING', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- I02: System shutdown
(gen_random_uuid(), 'INV-WAR-I02', 'System Shutdown', 'ACTIVE', 'Inverter system is shutting down.', 0, 0, 'system', 'WARNING', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- I03: Grid connected
(gen_random_uuid(), 'INV-WAR-I03', 'Grid Connected', 'ACTIVE', 'System has connected to the grid.', 0, 0, 'grid', 'WARNING', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- I04: Grid disconnected
(gen_random_uuid(), 'INV-WAR-I04', 'Grid Disconnected', 'ACTIVE', 'System has disconnected from the grid. Operating in off-grid mode.', 0, 0, 'grid', 'WARNING', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- I05: Battery charging
(gen_random_uuid(), 'BMS-WAR-I01', 'Battery Charging', 'ACTIVE', 'Battery is currently charging.', 0, 0, 'battery', 'WARNING', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- I06: Battery discharging
(gen_random_uuid(), 'BMS-WAR-I02', 'Battery Discharging', 'ACTIVE', 'Battery is currently discharging.', 0, 0, 'battery', 'WARNING', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- I07: Battery full
(gen_random_uuid(), 'BMS-WAR-I03', 'Battery Full', 'ACTIVE', 'Battery is fully charged.', 0, 0, 'battery', 'WARNING', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- I08: Force charge requested
(gen_random_uuid(), 'BMS-WAR-I04', 'Force Charge Requested', 'ACTIVE', 'BMS is requesting force charge due to low capacity. Immediately charge the battery system until flag is cleared.', 0, 0, 'battery', 'WARNING', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- I09: Full charge requested
(gen_random_uuid(), 'BMS-WAR-I05', 'Full Charge Requested', 'ACTIVE', 'BMS is requesting full charge for SOC recalculation and cell balancing. Charge battery to 100%.', 0, 0, 'battery', 'WARNING', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- =====================================================
-- GENERIC NOTIFICATION TYPES
-- Fallback types for unknown/unrecognized alarms
-- These capture alarms that don't match specific codes
-- =====================================================

-- Generic Inverter Alarm (Severity: ALARM)
(gen_random_uuid(), 'INV-ALM-GENERIC', 'Unknown Inverter Alarm', 'ACTIVE', 'An unrecognized inverter alarm has been detected. Please contact technical support for assistance.', 0, 0, 'inverter-unknown', 'ALARM', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- Generic Inverter Warning (Severity: WARNING)
(gen_random_uuid(), 'INV-WAR-GENERIC', 'Unknown Inverter Warning', 'ACTIVE', 'An unrecognized inverter warning has been detected. Please contact technical support for assistance.', 0, 0, 'inverter-unknown', 'WARNING', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- Generic Inverter Error (Severity: ERROR)
(gen_random_uuid(), 'INV-ERR-GENERIC', 'Unknown Inverter Error', 'ACTIVE', 'An unrecognized inverter error has been detected. Please contact technical support for assistance.', 0, 0, 'inverter-unknown', 'ERROR', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- Generic Inverter Info (Severity: WARNING - low priority)
(gen_random_uuid(), 'INV-WAR-INFO-GENERIC', 'Unknown Inverter Info', 'ACTIVE', 'An unrecognized inverter informational message has been detected.', 0, 0, 'inverter-unknown', 'WARNING', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- Generic BMS Alarm (Severity: ALARM)
(gen_random_uuid(), 'BMS-ALM-GENERIC', 'Unknown BMS Alarm', 'ACTIVE', 'An unrecognized battery management system alarm has been detected. Please contact technical support for assistance.', 0, 0, 'battery-unknown', 'ALARM', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- Generic BMS Warning (Severity: WARNING)
(gen_random_uuid(), 'BMS-WAR-GENERIC', 'Unknown BMS Warning', 'ACTIVE', 'An unrecognized battery management system warning has been detected. Please contact technical support for assistance.', 0, 0, 'battery-unknown', 'WARNING', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- Generic BMS Error (Severity: ERROR)
(gen_random_uuid(), 'BMS-ERR-GENERIC', 'Unknown BMS Error', 'ACTIVE', 'An unrecognized battery management system error has been detected. Please contact technical support for assistance.', 0, 0, 'battery-unknown', 'ERROR', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- Generic BMS Info (Severity: WARNING - low priority)
(gen_random_uuid(), 'BMS-WAR-INFO-GENERIC', 'Unknown BMS Info', 'ACTIVE', 'An unrecognized battery management system informational message has been detected.', 0, 0, 'battery-unknown', 'WARNING', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system');

-- =====================================================
-- Verification queries
-- =====================================================
SELECT
    "Severity",
    COUNT(*) as "Count"
FROM "DeviceNotificationTypes"
GROUP BY "Severity"
ORDER BY "Severity";

SELECT
    "Category",
    COUNT(*) as "Count"
FROM "DeviceNotificationTypes"
GROUP BY "Category"
ORDER BY "Category";

SELECT "Code", "Message", "Severity", "Category" FROM "DeviceNotificationTypes" ORDER BY "Code";
