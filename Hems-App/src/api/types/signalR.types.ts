// SignalR Device Notification Types
export interface DeviceNotificationDto {
  id: string;
  deviceId: string;
  siteId: string;
  setupDeviceId: string;
  deviceNotificationTypeId: string;
  timestamp: string;
  additionalData: string;
  isProcessed: boolean;
  created: string;
  deviceNotificationType: DeviceNotificationTypeDto;
  deviceManufacturer: string;
  siteName: string;
}

export interface DeviceNotificationTypeDto {
  id: string;
  code: string;
  message: string;
  status: string;
  description: string;
  register: number;
  value: number;
  category: string;
  version: string;
  isActive: boolean;
}

// SignalR Hub Events
export interface SignalRHubEvents {
  ReceiveDeviceNotification: (notification: DeviceNotificationDto) => void;
  ReceiveDeviceNotificationUpdate: (notification: DeviceNotificationDto) => void;
}

// SignalR Connection State
export type SignalRConnectionState = 'Disconnected' | 'Connecting' | 'Connected' | 'Reconnecting' | 'Failed';

// Critical Notification for BottomSheet
export interface CriticalNotification {
  id: string;
  title: string;
  message: string;
  description: string;
  category: 'critical' | 'warning' | 'info';
  deviceManufacturer: string;
  siteName: string;
  timestamp: string;
  additionalData?: any;
  isProcessed: boolean;
}

// PowerFlow Stream Types
// Must match backend PowerFlowDto exactly!
export interface PowerFlowDto {
  deviceId: string;              // Backend: Guid (serialized as string in JSON)
  solarPowerKw: number;          // Backend: SolarPowerKw (double) - in kilowatts
  gridPowerKw: number;           // Backend: GridPowerKw (double) - in kilowatts
  loadPowerKw: number;           // Backend: LoadPowerKw (double) - in kilowatts
  batteryPowerKw: number;        // Backend: BatteryPowerKw (double) - in kilowatts
  generatorPowerKw: number;      // Backend: GeneratorPowerKw (double) - in kilowatts
}

export interface PowerFlowStreamOptions {
  deviceId: string;
  intervalSeconds?: number;   // Default: 5 seconds
  onData: (data: PowerFlowDto) => void;
  onError?: (error: Error) => void;
  onComplete?: () => void;
}

export type PowerFlowStreamState = 'idle' | 'streaming' | 'error' | 'completed';

