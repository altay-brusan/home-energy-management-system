import { PaginatedList, WarrantyInfoDto } from './common.types';
import { DeviceType, WarrantyUnit, CreateDeviceComponentCommand, UpdateDeviceComponentCommand, DeviceComponentDto } from './deviceComponents.types';

export interface DeviceDto {
  id: string;
  manufacturer: string;
  modelNumber?: string | null;
  serialNumber?: string | null;
  typeNumber?: string | null;
  qrCode?: string | null;
  specifications: any;
  isDeleted: boolean;
  installationDate?: string | null;
  calibrationDate?: string | null;
  nextCheckoutDate?: string | null;
  warrantyPeriod?: number | null;
  warrantyUnit?: WarrantyUnit | null;
  warrantyStartDate?: string | null;
  deviceComponents: DeviceComponentDto[];
  setupDevice?: any | null;
  created: string;
  createdBy?: string | null;
  lastModified?: string | null;
  lastModifiedBy?: string | null;
}


export interface CreateDeviceCommand {
  manufacturer: string;
  modelNumber?: string | null;
  serialNumber?: string | null;
  typeNumber?: string | null;
  qrCode?: string | null;
  specifications: any;
  installationDate?: string | null;
  calibrationDate?: string | null;
  nextCheckoutDate?: string | null;
  warrantyPeriod?: number | null;
  warrantyUnit?: WarrantyUnit | null;
  warrantyStartDate?: string | null;
  deviceComponents: CreateDeviceComponentCommand[];
}


export interface UpdateDeviceCommand {
  id: string;
  manufacturer: string;
  modelNumber?: string | null;
  serialNumber?: string | null;
  typeNumber?: string | null;
  qrCode?: string | null;
  specifications: any;
  installationDate?: string | null;
  calibrationDate?: string | null;
  nextCheckoutDate?: string | null;
  warrantyPeriod?: number | null;
  warrantyUnit?: WarrantyUnit | null;
  warrantyStartDate?: string | null;
}


export interface DeviceLoginCommand {
  serialNumber: string;
  qrCode?: string | null;
}

export interface DeviceLoginResult {
  isSuccess: boolean;
  deviceId?: string | null;
  deviceName?: string | null;
  accessToken?: string | null;
  refreshToken?: string | null;
  expiresAt?: string | null;
  errorMessage?: string | null;
}

export interface GetDevicesParams {
  manufacturer?: string | null;
  isDeleted?: boolean | null;
  pageNumber: number;
  pageSize: number;
}

export interface GetDeviceComponentsParams {
  deviceId: string;
  isDeleted?: boolean | null;
}

export type PaginatedDeviceList = PaginatedList<DeviceDto>;

