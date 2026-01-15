-- SQL Script to populate DeviceNotificationTypes with Deye Three Phase Hybrid Inverter fault codes
-- Source: Deye SUN-5-12KWSG04LP3 Fault List
-- This script should be run AFTER the migration RemoveStatusRegisterValueFromDeviceNotificationType
-- Compatible with PostgreSQL 12+

-- Ensure uuid-ossp extension is available (required for uuid_generate_v4())
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- First, truncate existing notification types (WARNING: This will delete all existing data!)
TRUNCATE TABLE "DeviceNotificationTypes" CASCADE;

-- ============================================================================
-- ACTIVE WARNING CODES (Severity = 'Warning')
-- These warnings have known descriptions from the manufacturer documentation
-- ============================================================================
INSERT INTO "DeviceNotificationTypes" ("Id", "Code", "Message", "Description", "Category", "Severity", "Version", "Manufacturer", "IsActive", "Created", "CreatedBy", "LastModified", "LastModifiedBy")
VALUES
-- W2: FAN WARN
(uuid_generate_v4(), 'W2', 'Fan warning', 'Fan abnormal alarm. 1) Check the operating status of the fan. 2) If the fan is running abnormally, open the cover of the inverter to check the connection of the fan. 3) Seek help from us, if can not go back to normal state.', 'Inverter', 'Warning', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- W3: Grid Phase Wrong
(uuid_generate_v4(), 'W3', 'Grid phase wrong', 'Grid Phase Wrong. 1) Check the phase sequence connection of the power grid. 2) Try to change the grid type, 0, 240/120. 3) If there is still no solution to check the wiring at the grid end. 4) Seek help from us, if can not go back to normal state.', 'Inverter', 'Warning', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- W4: Meter Comm Fail
(uuid_generate_v4(), 'W4', 'Meter communication failure', 'Meter communication failure. 1) Check whether the meter has successful communication and whether the wiring is normal. 2) If the wiring is correct, the meter still has no communication, Seek help from us.', 'Inverter', 'Warning', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- W11: Battery_lowSoc
(uuid_generate_v4(), 'W11', 'Battery low SOC warning', 'Battery state of charge is low. 1) Check the battery charge level. 2) Allow the system to charge the battery from PV or grid. 3) If the warning persists, check battery health and connections.', 'Inverter', 'Warning', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- W31: Battery_comm_warn
(uuid_generate_v4(), 'W31', 'Battery communication warning', 'Abnormal battery communication. 1) Check whether the BMS connection is stable. 2) Check whether the BMS data is abnormal. 3) Seek help from us, if can not go back to normal state.', 'Inverter', 'Warning', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- W32: parallel-comm-warn
(uuid_generate_v4(), 'W32', 'Parallel communication warning', 'Unstable parallel communication. 1) Check the connection of the parallel communication line. Please do not wind the parallel communication line with other cables. 2) Check whether the parallel dip switch is on. 3) Seek help from us, if can not go back to normal state.', 'Inverter', 'Warning', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system');

-- ============================================================================
-- PLACEHOLDER WARNING CODES (IsActive = false)
-- Reserved for future use when manufacturer provides documentation
-- ============================================================================
INSERT INTO "DeviceNotificationTypes" ("Id", "Code", "Message", "Description", "Category", "Severity", "Version", "Manufacturer", "IsActive", "Created", "CreatedBy", "LastModified", "LastModifiedBy")
VALUES
-- W1: Reserved
(uuid_generate_v4(), 'W1', '', '', 'Inverter', 'Warning', '1.0', 'LOFTTY', false, NOW(), 'system', NOW(), 'system'),
-- W5: Reserved
(uuid_generate_v4(), 'W5', '', '', 'Inverter', 'Warning', '1.0', 'LOFTTY', false, NOW(), 'system', NOW(), 'system'),
-- W6: Reserved
(uuid_generate_v4(), 'W6', '', '', 'Inverter', 'Warning', '1.0', 'LOFTTY', false, NOW(), 'system', NOW(), 'system'),
-- W7: Reserved
(uuid_generate_v4(), 'W7', '', '', 'Inverter', 'Warning', '1.0', 'LOFTTY', false, NOW(), 'system', NOW(), 'system'),
-- W8: Reserved
(uuid_generate_v4(), 'W8', '', '', 'Inverter', 'Warning', '1.0', 'LOFTTY', false, NOW(), 'system', NOW(), 'system'),
-- W9: Reserved
(uuid_generate_v4(), 'W9', '', '', 'Inverter', 'Warning', '1.0', 'LOFTTY', false, NOW(), 'system', NOW(), 'system'),
-- W10: Reserved
(uuid_generate_v4(), 'W10', '', '', 'Inverter', 'Warning', '1.0', 'LOFTTY', false, NOW(), 'system', NOW(), 'system'),
-- W12: Reserved
(uuid_generate_v4(), 'W12', '', '', 'Inverter', 'Warning', '1.0', 'LOFTTY', false, NOW(), 'system', NOW(), 'system'),
-- W13: Reserved
(uuid_generate_v4(), 'W13', '', '', 'Inverter', 'Warning', '1.0', 'LOFTTY', false, NOW(), 'system', NOW(), 'system'),
-- W14: Reserved
(uuid_generate_v4(), 'W14', '', '', 'Inverter', 'Warning', '1.0', 'LOFTTY', false, NOW(), 'system', NOW(), 'system'),
-- W15: Reserved
(uuid_generate_v4(), 'W15', '', '', 'Inverter', 'Warning', '1.0', 'LOFTTY', false, NOW(), 'system', NOW(), 'system'),
-- W16: Reserved
(uuid_generate_v4(), 'W16', '', '', 'Inverter', 'Warning', '1.0', 'LOFTTY', false, NOW(), 'system', NOW(), 'system'),
-- W17: Reserved
(uuid_generate_v4(), 'W17', '', '', 'Inverter', 'Warning', '1.0', 'LOFTTY', false, NOW(), 'system', NOW(), 'system'),
-- W18: Reserved
(uuid_generate_v4(), 'W18', '', '', 'Inverter', 'Warning', '1.0', 'LOFTTY', false, NOW(), 'system', NOW(), 'system'),
-- W19: Reserved
(uuid_generate_v4(), 'W19', '', '', 'Inverter', 'Warning', '1.0', 'LOFTTY', false, NOW(), 'system', NOW(), 'system'),
-- W20: Reserved
(uuid_generate_v4(), 'W20', '', '', 'Inverter', 'Warning', '1.0', 'LOFTTY', false, NOW(), 'system', NOW(), 'system'),
-- W21: Reserved
(uuid_generate_v4(), 'W21', '', '', 'Inverter', 'Warning', '1.0', 'LOFTTY', false, NOW(), 'system', NOW(), 'system'),
-- W22: Reserved
(uuid_generate_v4(), 'W22', '', '', 'Inverter', 'Warning', '1.0', 'LOFTTY', false, NOW(), 'system', NOW(), 'system'),
-- W23: Reserved
(uuid_generate_v4(), 'W23', '', '', 'Inverter', 'Warning', '1.0', 'LOFTTY', false, NOW(), 'system', NOW(), 'system'),
-- W24: Reserved
(uuid_generate_v4(), 'W24', '', '', 'Inverter', 'Warning', '1.0', 'LOFTTY', false, NOW(), 'system', NOW(), 'system'),
-- W25: Reserved
(uuid_generate_v4(), 'W25', '', '', 'Inverter', 'Warning', '1.0', 'LOFTTY', false, NOW(), 'system', NOW(), 'system'),
-- W26: Reserved
(uuid_generate_v4(), 'W26', '', '', 'Inverter', 'Warning', '1.0', 'LOFTTY', false, NOW(), 'system', NOW(), 'system'),
-- W27: Reserved
(uuid_generate_v4(), 'W27', '', '', 'Inverter', 'Warning', '1.0', 'LOFTTY', false, NOW(), 'system', NOW(), 'system'),
-- W28: Reserved
(uuid_generate_v4(), 'W28', '', '', 'Inverter', 'Warning', '1.0', 'LOFTTY', false, NOW(), 'system', NOW(), 'system'),
-- W29: Reserved
(uuid_generate_v4(), 'W29', '', '', 'Inverter', 'Warning', '1.0', 'LOFTTY', false, NOW(), 'system', NOW(), 'system'),
-- W30: Reserved
(uuid_generate_v4(), 'W30', '', '', 'Inverter', 'Warning', '1.0', 'LOFTTY', false, NOW(), 'system', NOW(), 'system');

-- Insert Fault Codes (Severity = 'Error')
INSERT INTO "DeviceNotificationTypes" ("Id", "Code", "Message", "Description", "Category", "Severity", "Version", "Manufacturer", "IsActive", "Created", "CreatedBy", "LastModified", "LastModifiedBy")
VALUES
-- F1: DC_Inversed Failure
(uuid_generate_v4(), 'F1', 'DC inversed failure', '1) Check the PV input polarity. 2) Seek help from us, if can not go back to normal state.', 'Inverter', 'Error', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F2: DC_Insulation_Failure
(uuid_generate_v4(), 'F2', 'DC insulation failure', '1) Check whether the PV is grounded, secondly, check whether the impedance of the PV to the ground is normal. 2) Seek help from us, if can not go back to normal state.', 'Inverter', 'Error', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F3: GFDI_Failure
(uuid_generate_v4(), 'F3', 'GFDI failure', '1) Check whether the PV is grounded, secondly, check whether the impedance of the PV to the ground is normal, whether the leakage current. 2) Seek help from us, if can not go back to normal state.', 'Inverter', 'Error', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F4: GFDI_Ground_Failure
(uuid_generate_v4(), 'F4', 'GFDI ground failure', '1) Check whether the PV is grounded. 2) Seek help from us, if can not go back to normal state.', 'Inverter', 'Error', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F5: EEPROM_Read_Failure
(uuid_generate_v4(), 'F5', 'EEPROM read failure', '1) Restart the inverter 3 times and restore the factory settings. 2) Seek help from us, if can not go back to normal state.', 'Inverter', 'Error', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F6: EEPROM_Write_Failure
(uuid_generate_v4(), 'F6', 'EEPROM write failure', '1) Restart the inverter 3 times and restore the factory settings. 2) Seek help from us, if can not go back to normal state.', 'Inverter', 'Error', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F7: DC/DC_Softstart_Fault
(uuid_generate_v4(), 'F7', 'DC/DC soft-start fault', '1) The BUS voltage can not be built from PV or battery. 2) Restart the inverter, If the fault still exists, please contact us for help.', 'Inverter', 'Error', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F8: GFDI_Relay_Failure
(uuid_generate_v4(), 'F8', 'GFDI relay failure', '1) Restart the inverter 3 times and restore the factory settings. 2) Seek help from us, if can not go back to normal state.', 'Inverter', 'Error', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F9: IGBT_Failure
(uuid_generate_v4(), 'F9', 'IGBT failure', '1) Restart the inverter 3 times and restore the factory settings. 2) Seek help from us, if can not go back to normal state.', 'Inverter', 'Error', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F10: AuxPowerBoard_Failure
(uuid_generate_v4(), 'F10', 'AuxPowerBoard failure', '1) First check whether the inverter switch is open. 2) Restart the inverter 3 times and restore the factory settings. 3) Seek help from us, if can not go back to normal state.', 'Inverter', 'Error', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F11: AC_MainContactor_Failure
(uuid_generate_v4(), 'F11', 'AC main contactor failure', '1) Restart the inverter 3 times and restore the factory settings. 2) Seek help from us, if can not go back to normal state.', 'Inverter', 'Error', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F12: AC_SlaveContactor_Failure
(uuid_generate_v4(), 'F12', 'AC slave contactor failure', '1) Restart the inverter 3 times and restore the factory settings. 2) Seek help from us, if can not go back to normal state.', 'Inverter', 'Error', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F13: Grid_Mode_changed
(uuid_generate_v4(), 'F13', 'Grid-Mode changed', '1) When the grid type and frequency changed it will report F13. 2) When the battery mode was changed to "No battery" mode, it will report F13. 3) For some old FW version, it will report F13 when the system work mode changed. 4) Generally, it will disappear automatically when shows F13. 5) If still same, turn off the DC switch and AC switch and wait for one minute and then turn on the DC/AC switch. 6) Seek help from us, if can not go back to normal state.', 'Inverter', 'Error', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F14: DC_OverCurr_Fault
(uuid_generate_v4(), 'F14', 'DC over current fault', '1) Restart the inverter 3 times and restore the factory settings. 2) Seek help from us, if can not go back to normal state.', 'Inverter', 'Error', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F15: SW_AC_OverCurr_Fault
(uuid_generate_v4(), 'F15', 'SW_AC over current fault', 'AC side over current fault. 1) Please check whether the backup load power and common load power are within the range. 2) Restart and check whether it is in normal. 3) Seek help from us, if can not go back to normal state.', 'Inverter', 'Error', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F16: GFCI_Failure
(uuid_generate_v4(), 'F16', 'GFCI failure', 'Leakage current fault. 1) Check the PV side cable ground connection. 2) Restart the system 2-3 times. 3) If the fault still existing, please contact us for help.', 'Inverter', 'Error', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F17: Tz_PV_OverCurr_Fault
(uuid_generate_v4(), 'F17', 'Tz_PV over current fault', '1) Check the PV connection and whether the PV is unstable. 2) Restart the inverter 3 times. 3) Seek help from us, if can not go back to normal state.', 'Inverter', 'Error', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F18: HW_Ac_OverCurr_Fault
(uuid_generate_v4(), 'F18', 'HW_Ac Over current fault', 'AC side over current fault. 1) Please check whether the backup load power and common load power are within the range. 2) Restart and check whether it is in normal. 3) Seek help from us, if cannot go back to normal state.', 'Inverter', 'Error', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F19: Tz_Integ_Fault
(uuid_generate_v4(), 'F19', 'Tz_Integ fault', '1) Restart the inverter 3 times and restore the factory settings. 2) Seek help from us, if can not go back to normal state.', 'Inverter', 'Error', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F20: Tz_Dc_OverCurr_Fault
(uuid_generate_v4(), 'F20', 'Tz_Dc over Current fault', 'DC side over current fault. 1) Check PV module connect and battery connect. 2) When in the off-grid mode, the inverter startup with big power load, it may report F20. Please reduce the load power connected. 3) Turn off the DC switch and AC switch and then wait one minute, then turn on the DC/AC switch again. 4) Seek help from us, if can not go back to normal state.', 'Inverter', 'Error', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F21: Tz_HV_OverCurr_Fault
(uuid_generate_v4(), 'F21', 'Tz_HV over current fault', 'BUS over current. 1) Check the PV input current and battery current setting. 2) Restart the system 2-3 times. 3) If the fault still exists, please contact us for help.', 'Inverter', 'Error', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F22: Tz_EmergStop_Fault
(uuid_generate_v4(), 'F22', 'Tz_Emergency stop fault', 'Remotely shutdown. 1) It tells the inverter is remotely controlled.', 'Inverter', 'Error', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F23: Tz_GFCI_OC_Fault
(uuid_generate_v4(), 'F23', 'Tz_GFCI_OC_Fault', 'Leakage current fault. 1) Check PV side cable ground connection. 2) Restart the system 2-3 times. 3) If the fault still exists, please contact us for help.', 'Inverter', 'Error', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F24: DC_Insulation_Fault
(uuid_generate_v4(), 'F24', 'DC insulation fault', 'PV isolation resistance is too low. 1) Check the connection of PV panels and inverter is firmly and correctly. 2) Check whether the PE cable of inverter is connected to ground. 3) Seek help from us, if can not go back to normal state.', 'Inverter', 'Error', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F25: DC_Feedback_Fault
(uuid_generate_v4(), 'F25', 'DC feedback fault', '1) Restart the inverter 3 times and restore the factory settings. 2) Seek help from us, if can not go back to normal state.', 'Inverter', 'Error', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F26: BusUnbalance_Fault
(uuid_generate_v4(), 'F26', 'Bus unbalance fault', '1) Please wait for a while and check whether it is normal. 2) When the load power of 3 phases is big different, it will report the F26. 3) When there is DC leakage current, it will report F26. 4) Restart the system 2-3 times. 5) Seek help from us, if can not go back to normal state.', 'Inverter', 'Error', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F27: DC_Insulation_Fault
(uuid_generate_v4(), 'F27', 'DC insulation fault', '1) Restart the inverter 3 times and restore the factory settings. 2) Seek help from us, if can not go back to normal state.', 'Inverter', 'Error', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F28: DCIOver_M1_Fault
(uuid_generate_v4(), 'F28', 'DCI Over M1 fault', '1) Restart the inverter 3 times and restore the factory settings. 2) Seek help from us, if can not go back to normal state.', 'Inverter', 'Error', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F29: Parallel_CANBus_Fault
(uuid_generate_v4(), 'F29', 'Parallel CANBus fault', '1) When in parallel mode, check the parallel communication cable connection and hybrid inverter communication address setting. 2) During the parallel system startup period, inverters will report F29. But when all inverters are in ON status, it will disappear automatically. 3) If the fault still exists, please contact us for help.', 'Inverter', 'Error', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F30: AC_MainContactor_Fault
(uuid_generate_v4(), 'F30', 'AC main contactor fault', '1) Restart the inverter 3 times and restore the factory settings. 2) Seek help from us, if can not go back to normal state.', 'Inverter', 'Error', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F31: AC_SlaveContactor_Fault
(uuid_generate_v4(), 'F31', 'AC slave contactor fault', '1) Check whether the grid orientation is correct. 2) Restart the inverter 3 times and restore the factory settings. 3) Seek help from us, if can not go back to normal state.', 'Inverter', 'Error', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F32: DCIOver_M2_Fault
(uuid_generate_v4(), 'F32', 'DCIOver M2 fault', '1) Restart the inverter 3 times and restore the factory settings. 2) Seek help from us, if can not go back to normal state.', 'Inverter', 'Error', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F33: AC_OverCurr_Fault
(uuid_generate_v4(), 'F33', 'AC over current fault', '1) Check whether the grid current is too large. 2) Restart the inverter 3 times and restore the factory settings. 3) Seek help from us, if can not go back to normal state.', 'Inverter', 'Error', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F34: AC_Overload_Fault
(uuid_generate_v4(), 'F34', 'AC over load fault', '1) Check the backup load connected, make sure it is in allowed power range. 2) If the fault still exists, please contact us for help.', 'Inverter', 'Error', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F35: AC_NoUtility_Fault
(uuid_generate_v4(), 'F35', 'AC no utility fault', '1) Check the grid voltage and frequency, whether the connection of the power grid is normal. 2) Contact the technical staff of the industry to solve.', 'Inverter', 'Error', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F36: AC_GridPhaseSeque_Fault
(uuid_generate_v4(), 'F36', 'AC Grid phase sequence fault', '1) Check the Grid Phase. 2) If the fault still exists, please contact us for help.', 'Inverter', 'Error', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F37: AC_Volt_Unbalance_Fault
(uuid_generate_v4(), 'F37', 'AC volt unbalance fault', '1) Check whether the inverter is AC overcurrent, restart the inverter. 2) If the fault still exists, please contact us for help.', 'Inverter', 'Error', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F38: Parallel_system_Stop1
(uuid_generate_v4(), 'F38', 'Parallel system Stop1', '1) Check the parallel connect. 2) If the fault still exists, please contact us for help.', 'Inverter', 'Error', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F39: INT_AC_OverCurr_Fault
(uuid_generate_v4(), 'F39', 'Inverter AC over current fault', '1) Inverter AC overcurrent, restart the inverter. 2) If the fault still exists, please contact us for help.', 'Inverter', 'Error', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F40: INT_DC_OverCurr_Fault
(uuid_generate_v4(), 'F40', 'Inverter DC over current fault', '1) Inverter DC overcurrent, restart the inverter. 2) If the fault still exists, please contact us for help.', 'Inverter', 'Error', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F41: Parallel_System_Stop
(uuid_generate_v4(), 'F41', 'parallel system stop', '1) Check the hybrid inverter work status. If there is 1pcs hybrid inverter shutdown, all hybrid inverters will report F41 fault. 2) If the fault still exists, please contact us for help.', 'Inverter', 'Error', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F42: Parallel_Version_Fault
(uuid_generate_v4(), 'F42', 'parallel version fault', '1) Check whether the inverter version is consistent. 2) Please contact us to upgrade the software version.', 'Inverter', 'Error', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F43: AC_VW_OverVolt_Fault
(uuid_generate_v4(), 'F43', 'AC VW over voltage fault', 'Grid voltage out of range. 1) Check the voltage is in the range of specification or not. 2) Check whether AC cables are firmly and correctly connected. 3) Seek help from us, if can not go back to normal state.', 'Inverter', 'Error', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F44: AC_VW_UnderVolt_Fault
(uuid_generate_v4(), 'F44', 'AC VW under Voltage fault', 'Grid voltage out of range. 1) Check the voltage is in the range of specification or not. 2) Check whether AC cables are firmly and correctly connected. 3) Seek help from us, if can not go back to normal state.', 'Inverter', 'Error', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F45: AC_UV_OverVolt_Fault
(uuid_generate_v4(), 'F45', 'AC UV over voltage fault', 'Grid voltage out of range. 1) Check the voltage is in the range of specification or not. 2) Check whether AC cables are firmly and correctly connected. 3) Seek help from us, if can not go back to normal state.', 'Inverter', 'Error', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F46: AC_UV_UnderVolt_Fault
(uuid_generate_v4(), 'F46', 'AC uv under voltage fault', 'Grid voltage out of range. 1) Check the voltage is in the range of specification or not. 2) Check whether AC cables are firmly and correctly connected. 3) Seek help from us, if can not go back to normal state.', 'Inverter', 'Error', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F47: AC_OverFreq_Fault
(uuid_generate_v4(), 'F47', 'AC over frequency fault', 'Grid frequency out of range. 1) Check the frequency is in the range of specification or not. 2) Check whether AC cables are firmly and correctly connected. 3) Seek help from us, if can not go back to normal state.', 'Inverter', 'Error', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F48: AC_UnderFreq_Fault
(uuid_generate_v4(), 'F48', 'AC under frequency fault', 'Grid frequency out of range. 1) Check the frequency is in the range of specification or not. 2) Check whether AC cables are firmly and correctly connected. 3) Seek help from us, if can not go back to normal state.', 'Inverter', 'Error', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F49: AC_U_GridCurr_DcHigh_Fault
(uuid_generate_v4(), 'F49', 'AC U grid Current DC high fault', '1) Restart the inverter 3 times and restore the factory settings. 2) Seek help from us, if can not go back to normal state.', 'Inverter', 'Error', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F50: AC_V_GridCurr_DcHigh_Fault
(uuid_generate_v4(), 'F50', 'AC V grid Current DC high fault', '1) Restart the inverter 3 times and restore the factory settings. 2) Seek help from us, if can not go back to normal state.', 'Inverter', 'Error', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F51: AC_W_GridCurr_DcHigh_Fault
(uuid_generate_v4(), 'F51', 'AC W grid Current DC high fault', '1) Restart the inverter 3 times and restore the factory settings. 2) Seek help from us, if can not go back to normal state.', 'Inverter', 'Error', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F52: AC_A_InductCurr_DcHigh_Fault
(uuid_generate_v4(), 'F52', 'AC A inductor current DC high fault', '1) Restart the inverter 3 times and restore the factory settings. 2) Seek help from us, if can not go back to normal state.', 'Inverter', 'Error', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F53: AC_B_InductCurr_DcHigh_Fault
(uuid_generate_v4(), 'F53', 'AC B inductor current DC high fault', '1) Restart the inverter 3 times and restore the factory settings. 2) Seek help from us, if can not go back to normal state.', 'Inverter', 'Error', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F54: BAT_VoltHigh_Fault
(uuid_generate_v4(), 'F54', 'Battery voltage high fault', '1) Check the battery terminal voltage. 2) Restart the inverter 3 times and restore the factory settings. 3) Seek help from us, if can not go back to normal state.', 'Inverter', 'Error', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F55: DC_VoltHigh_Fault
(uuid_generate_v4(), 'F55', 'DC voltage high fault', 'BUS voltage is too high. 1) Check whether battery voltage is too high. 2) Check the PV input voltage, make sure it is within the allowed range. 3) Seek help from us, if can not go back to normal state.', 'Inverter', 'Error', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F56: DC_VoltLow_Fault
(uuid_generate_v4(), 'F56', 'DC voltage Low fault', 'Battery voltage low. 1) Check whether battery voltage is too low. 2) If the battery voltage is too low, using PV or grid to charge the battery. 3) Seek help from us, if can not go back to normal state.', 'Inverter', 'Error', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F57: AC_BackFeed_Fault
(uuid_generate_v4(), 'F57', 'AC back feed fault', '1) Restart the inverter 3 times and restore the factory settings. 2) Seek help from us, if can not go back to normal state.', 'Inverter', 'Error', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F58: BMS_Communication_Fault
(uuid_generate_v4(), 'F58', 'BMS communication fault', '1) It tells the communication between hybrid inverter and battery BMS disconnected when "BMS_Err-Stop" is active. 2) If you do not want to see this happen, you can disable "BMS_Err-Stop" item on the LCD. 3) If the fault still exists, please contact us for help.', 'Inverter', 'Error', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F59: AC_V_GridCurr_High_Fault
(uuid_generate_v4(), 'F59', 'AC V grid current high fault', '1) Check whether the inverter current is too high, restart the inverter, restore the factory settings. 2) Seek help from us, if can not go back to normal state.', 'Inverter', 'Error', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F60: Gen_Volt_or_Fre_Fault
(uuid_generate_v4(), 'F60', 'Generator voltage or frequency fault', '1) Check whether the voltage and frequency of the generator are normal, and then restart. 2) Seek help from us, if can not go back to normal state.', 'Inverter', 'Error', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F61: Button_Manual_OFF
(uuid_generate_v4(), 'F61', 'Button manual OFF', '1) Check whether the switch of the inverter is turned on, restart the inverter, and restore the factory settings. 2) Seek help from us, if can not go back to normal state.', 'Inverter', 'Error', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F62: DRMs0_Stop
(uuid_generate_v4(), 'F62', 'DRMs0 stop', '1) The DRM function is for Australia market only. 2) Check the DRM function is active or not. 3) Seek help from us, if can not go back to normal state after restart the system.', 'Inverter', 'Error', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F63: Arc_Fault
(uuid_generate_v4(), 'F63', 'Arc fault', '1) ARC fault detection is only for US market. 2) Check PV module cable connection and clear the fault. 3) Seek help from us, if can not go back to normal state.', 'Inverter', 'Error', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F64: Heatsink_HighTemp_Fault
(uuid_generate_v4(), 'F64', 'Heatsink high temperature fault', 'Heat sink temperature is too high. 1) Check whether the work environment temperature is too high. 2) Turn off the inverter for 10mins and restart. 3) Seek help from us, if can not go back to normal state.', 'Inverter', 'Error', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- ============================================================================
-- COMM BOARD FAULT CODES (F65-F67)
-- These are derived from commBoardFaultRaw register (Bit0, Bit1, Bit2)
-- ============================================================================
-- F65: Comm Board Flash Chip Error (commBoardFaultRaw Bit0)
(uuid_generate_v4(), 'F65', 'Communication board flash chip error', 'The communication board has detected a flash chip error. This is an internal circuit board issue. 1) Restart the inverter. 2) If the fault persists, the communication board may need replacement. 3) Please contact technical support for assistance.', 'Inverter', 'Error', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F66: Comm Board Time Error (commBoardFaultRaw Bit1)
(uuid_generate_v4(), 'F66', 'Communication board time error', 'The communication board has detected a time synchronization error. This is an internal circuit board issue. 1) Restart the inverter. 2) Check if the system time is correct. 3) If the fault persists, please contact technical support for assistance.', 'Inverter', 'Error', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system'),

-- F67: Comm Board EEPROM Error (commBoardFaultRaw Bit2)
(uuid_generate_v4(), 'F67', 'Communication board EEPROM error', 'The communication board has detected an EEPROM memory error. This is an internal circuit board issue. 1) Restart the inverter 3 times and restore the factory settings. 2) If the fault persists, the communication board may need replacement. 3) Please contact technical support for assistance.', 'Inverter', 'Error', '1.0', 'LOFTTY', true, NOW(), 'system', NOW(), 'system');

-- Add generic fallback types for backward compatibility
INSERT INTO "DeviceNotificationTypes" ("Id", "Code", "Message", "Description", "Category", "Severity", "Version", "Manufacturer", "IsActive", "Created", "CreatedBy", "LastModified", "LastModifiedBy")
VALUES
(uuid_generate_v4(), 'INV-ALM-GENERIC', 'Inverter Alarm', 'A generic inverter alarm was detected. Please contact technical support for details.', 'Inverter', 'ALARM', '1.0', 'Generic', true, NOW(), 'system', NOW(), 'system'),
(uuid_generate_v4(), 'INV-WAR-GENERIC', 'Inverter Warning', 'A generic inverter warning was detected. Please contact technical support for details.', 'Inverter', 'WARNING', '1.0', 'Generic', true, NOW(), 'system', NOW(), 'system'),
(uuid_generate_v4(), 'INV-ERR-GENERIC', 'Inverter Error', 'A generic inverter error was detected. Please contact technical support for details.', 'Inverter', 'ERROR', '1.0', 'Generic', true, NOW(), 'system', NOW(), 'system'),
(uuid_generate_v4(), 'BMS-ALM-GENERIC', 'BMS Alarm', 'A generic BMS alarm was detected. Please contact technical support for details.', 'Battery', 'ALARM', '1.0', 'Generic', true, NOW(), 'system', NOW(), 'system'),
(uuid_generate_v4(), 'BMS-WAR-GENERIC', 'BMS Warning', 'A generic BMS warning was detected. Please contact technical support for details.', 'Battery', 'WARNING', '1.0', 'Generic', true, NOW(), 'system', NOW(), 'system'),
(uuid_generate_v4(), 'BMS-ERR-GENERIC', 'BMS Error', 'A generic BMS error was detected. Please contact technical support for details.', 'Battery', 'ERROR', '1.0', 'Generic', true, NOW(), 'system', NOW(), 'system');

-- Verify the insert count
SELECT COUNT(*) AS total_notification_types FROM "DeviceNotificationTypes";
