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
export interface PowerFlowDto {
  deviceId: string;
  timestamp: string;
  gridPower: number;          // Watts (+ = importing, - = exporting)
  solarPower: number;         // Watts
  batteryPower: number;       // Watts (+ = charging, - = discharging)
  loadPower: number;          // Watts
  batterySOC: number;         // Percentage (0-100)
  batteryStatus?: 'charging' | 'discharging' | 'idle';
  gridStatus?: 'importing' | 'exporting' | 'idle';
}

export interface PowerFlowStreamOptions {
  deviceId: string;
  intervalSeconds?: number;   // Default: 5 seconds
  onData: (data: PowerFlowDto) => void;
  onError?: (error: Error) => void;
  onComplete?: () => void;
}

export type PowerFlowStreamState = 'idle' | 'streaming' | 'error' | 'completed';

