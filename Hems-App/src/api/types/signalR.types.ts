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

