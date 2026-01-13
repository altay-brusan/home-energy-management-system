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

  // Generate realistic power values with energy balance
  // Load: Always consuming (negative value) - this is the demand
  const loadPowerAbs = Math.floor(Math.random() * 3000) + 500; // 500W to 3.5kW
  const loadPower = -loadPowerAbs; // Always negative (consuming)

  // PV: Solar generation (positive value or 0)
  const pvPower = Math.random() > 0.2 ? Math.floor(Math.random() * 5000) : 0; // 0-5kW

  // Calculate remaining energy need after PV
  const remainingNeed = loadPowerAbs - pvPower;

  let batteryPower = 0;
  let gridPower = 0;

  if (remainingNeed > 0) {
    // Load > PV: Need more energy
    // Battery can help (discharge) or grid must provide
    const batteryCanHelp = Math.random() > 0.3;

    if (batteryCanHelp) {
      // Battery discharges (positive value)
      const batteryContribution = Math.floor(Math.random() * Math.min(remainingNeed, 2000));
      batteryPower = batteryContribution;

      // Grid provides the rest (negative = importing)
      gridPower = -(remainingNeed - batteryContribution);
    } else {
      // Only grid provides (negative = importing)
      gridPower = -remainingNeed;
    }
  } else if (remainingNeed < 0) {
    // PV > Load: Excess energy
    const excess = -remainingNeed;

    // Battery can charge or excess goes to grid
    const batteryCanCharge = Math.random() > 0.4;

    if (batteryCanCharge) {
      // Battery charges (negative value)
      const batteryCharge = Math.floor(Math.random() * Math.min(excess, 2000));
      batteryPower = -batteryCharge;

      // Remaining excess goes to grid (positive = exporting)
      gridPower = excess - batteryCharge;
    } else {
      // All excess goes to grid (positive = exporting)
      gridPower = excess;
    }
  }
  // else: remainingNeed === 0, PV exactly matches load, battery and grid are 0

  // Verify energy balance: pvPower + batteryPower + gridPower + loadPower should = 0
  // pvPower (positive) + batteryPower (+/-) + gridPower (+/-) + loadPower (negative) = 0

  return {
    timestamp: new Date().toISOString(),
    pv: {
      power: pvPower, // Always >= 0 (output)
      active: pvPower > 0,
    },
    battery: {
      power: batteryPower, // Positive = discharging, Negative = charging, 0 = idle
      active: Math.abs(batteryPower) > 0,
    },
    grid: {
      power: gridPower, // Positive = exporting, Negative = importing, 0 = idle
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
