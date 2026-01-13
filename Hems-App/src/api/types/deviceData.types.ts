// Device Data API Types
import { PaginatedList } from './common.types';

export interface CreateDeviceDataCommand {
  dataType: string;
  deviceId: string;
  requestDate: string;
  data: any; // JsonDocument
}

export interface CreateDeviceEnergyDataCommand {
  dataType: string;
  deviceId: string;
  dataDateTime: string;
  pvEnergyKWhToday?: number | null;
  gridImportEnergyKWhToday?: number | null;
  gridExportEnergyKWhToday?: number | null;
  generatorEnergyKWhToday?: number | null;
  batteryDischargeEnergyKWhToday?: number | null;
  batteryChargeEnergyKWhToday?: number | null;
  loadEnergyKWhToday?: number | null;
  pvEnergyKWhTotal?: number | null;
  gridImportEnergyKWhTotal?: number | null;
  gridExportEnergyKWhTotal?: number | null;
  generatorEnergyKWhTotal?: number | null;
  batteryDischargeEnergyKWhTotal?: number | null;
  batteryChargeEnergyKWhTotal?: number | null;
  loadEnergyKWhTotal?: number | null;
}

export interface BulkCreateDeviceDataCommand {
  deviceDataItems: DeviceDataItem[];
}

export interface DeviceDataItem {
  dataType: string;
  deviceId: string;
  requestDate: string;
  data: any; // JsonDocument
}

export interface BulkCreateDeviceDataResult {
  successCount: number;
  failureCount: number;
  errors: string[];
  processingTime: string;
}

export interface DeviceDataDto {
  id: number;
  dataType: string;
  deviceId: string;
  requestDate: string;
  data: any; // JsonDocument
  created: string;
  createdBy?: string | null;
}

export interface GetDeviceDataParams {
  deviceId?: string | null;
  dataType?: string | null;
  pageNumber: number;
  pageSize: number;
  userId?: string | null;
}

export type PaginatedDeviceDataList = PaginatedList<DeviceDataDto>;
