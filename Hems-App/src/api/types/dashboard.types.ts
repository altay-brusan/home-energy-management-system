import { WarrantyInfoDto } from './common.types';
import { DeviceType } from './deviceComponents.types';

export enum AlertType {
  Info = 1,
  Warning = 2,
  Error = 3,
  Maintenance = 4,
}

export enum AlertPriority {
  Low = 1,
  Medium = 2,
  High = 3,
  Critical = 4,
}

export interface AlertDto {
  id: string;
  title: string;
  message: string;
  type: AlertType;
  priority: AlertPriority;
  createdAt: string;
  isRead: boolean;
}

export interface SystemStatusDto {
  overallStatus: string;
  batteryLevel: number;
  powerGeneration: number;
  powerConsumption: number;
  gridImport: number;
  gridExport: number;
  operatingMode: string;
  batteryTemperatures: number[];
  ambientTemperature: number;
  fanState: string;
  lastUpdate: string;
}

export interface DeviceComponentOverviewDto {
  id: string;
  deviceType: DeviceType;
  name: string;
  manufacturer: string;
  modelNumber: string;
  serialNumber: string;
  specifications: any;
  isActive: boolean;
  warranty: WarrantyInfoDto;
}

export interface DeviceOverviewDto {
  id: string;
  name: string;
  manufacturer: string;
  modelNumber: string;
  serialNumber: string;
  qrCode: string;
  isOnline: boolean;
  lastSeen?: string | null;
  specifications: any;
  components: DeviceComponentOverviewDto[];
  warranty: WarrantyInfoDto;
}

export interface ExternalDeviceOverviewDto {
  id: string;
  name: string;
  deviceType: string;
  specifications: any;
  isActive: boolean;
  created: string;
}

export interface MobileDashboardDto {
  siteId: string;
  siteName: string;
  setupId: string;
  setupName: string;
  device: DeviceOverviewDto;
  externalDevices: ExternalDeviceOverviewDto[];
  activeAlerts: AlertDto[];
  systemStatus: SystemStatusDto;
}
export interface WeatherForecastDto {
  date: string;
  temperature: number;
  feelsLike: number;
  humidity: number;
  pressure: number;
  windSpeed: number;
  description: string;
  icon: string;
}

export interface InverterBatChannelSocDto {
  channelId: number;
  socPercent: number;
}

export interface SocResponseDto {
  timestampUtc: string;
  version: number;
  deviceId: string;
  inverterBatChannelSoc: InverterBatChannelSocDto[];
  bmsSocDetails: any[];
}
