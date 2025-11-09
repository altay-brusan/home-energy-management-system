/**
 * Mock Power Service
 *
 * This service provides mock real-time power data for the Energy Flow Radial component.
 * Replace this with actual API integration when real-time power endpoint becomes available.
 *
 * Expected real API endpoint: GET /MobileDashboard/graphs/CurrentPower?deviceId={deviceId}
 */

export type FlowDirection = 'input' | 'output' | 'both';

export interface PowerComponentData {
  power: number;        // Power in Watts (W)
  active: boolean;      // Whether the component is currently active
  flow: FlowDirection;  // Direction of energy flow
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
  const pvPower = Math.random() > 0.2 ? Math.floor(Math.random() * 5000) : 0; // 0-5kW
  const loadPower = Math.floor(Math.random() * 3000) + 500; // 500W-3.5kW (always consuming)

  // Battery can charge or discharge
  const batteryCharging = Math.random() > 0.5;
  const batteryPower = Math.random() > 0.3 ? Math.floor(Math.random() * 2000) : 0; // 0-2kW

  // Grid compensates for energy balance
  // If PV > Load, export to grid; otherwise import from grid
  const energyBalance = pvPower + (batteryCharging ? -batteryPower : batteryPower) - loadPower;
  const gridPower = Math.abs(energyBalance);
  const gridExporting = energyBalance > 0;

  return {
    timestamp: new Date().toISOString(),
    pv: {
      power: pvPower,
      active: pvPower > 0,
      flow: 'output', // PV always outputs when active
    },
    battery: {
      power: batteryPower,
      active: batteryPower > 0,
      flow: batteryCharging ? 'input' : 'output',
    },
    grid: {
      power: gridPower,
      active: gridPower > 100, // Consider grid active if >100W
      flow: gridExporting ? 'output' : 'input',
    },
    load: {
      power: loadPower,
      active: true, // Load is always active (house always consuming)
      flow: 'input', // Load always inputs (consumes)
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
