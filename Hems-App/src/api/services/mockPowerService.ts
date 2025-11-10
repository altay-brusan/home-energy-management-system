/**
 * Mock Power Service
 *
 * This service provides mock real-time power data for the Energy Flow Radial component.
 * Replace this with actual API integration when real-time power endpoint becomes available.
 *
 * Expected real API endpoint: GET /MobileDashboard/graphs/CurrentPower?deviceId={deviceId}
 */

export interface PowerComponentData {
  power: number;        // Power in Watts (W) - Positive: output/discharge, Negative: input/charge
  active: boolean;      // Whether the component is currently active
}

export interface RealTimePowerResponseDto {
  timestamp: string;
  pv: PowerComponentData;
  battery: PowerComponentData;
  grid: PowerComponentData;
  load: PowerComponentData;
}

/**
 * Mock function to simulate real-time power data
 *
 * @param deviceId - The device ID (currently unused in mock)
 * @returns Promise with mock power data
 */
export const getCurrentPower = async (deviceId: string): Promise<RealTimePowerResponseDto> => {
  // Simulate API delay
  await new Promise<void>(resolve => setTimeout(() => resolve(), 300));

  // Generate realistic power values
  // PV: Always positive (output/generating) or 0
  const pvPower = Math.random() > 0.2 ? Math.floor(Math.random() * 5000) : 0; // 0-5kW

  // Load: Always negative (input/consuming)
  const loadPower = -(Math.floor(Math.random() * 3000) + 500); // -500W to -3.5kW

  // Battery: Can be positive (discharging) or negative (charging)
  const batteryCharging = Math.random() > 0.5;
  const batteryPowerAbs = Math.random() > 0.3 ? Math.floor(Math.random() * 2000) : 0; // 0-2kW
  const batteryPower = batteryCharging ? -batteryPowerAbs : batteryPowerAbs; // Negative = charging, Positive = discharging

  // Grid: Compensates for energy balance
  // Positive = exporting to grid, Negative = importing from grid
  const energyBalance = pvPower + batteryPower + loadPower;
  const gridPower = -energyBalance; // Grid compensates the balance

  return {
    timestamp: new Date().toISOString(),
    pv: {
      power: pvPower, // Always >= 0 (output)
      active: pvPower > 0,
    },
    battery: {
      power: batteryPower, // Positive = discharging, Negative = charging
      active: Math.abs(batteryPower) > 0,
    },
    grid: {
      power: gridPower, // Positive = exporting, Negative = importing
      active: Math.abs(gridPower) > 100, // Consider grid active if >100W
    },
    load: {
      power: loadPower, // Always < 0 (consuming)
      active: true, // Load is always active
    },
  };
};

/**
 * Mock service object matching the pattern of other services
 */
const mockPowerService = {
  getCurrentPower,
};

export default mockPowerService;
