// Device Components Types
export enum DeviceType {
  MPPS = 1,
  Invertor = 2,
  BMS = 3,
  Battery = 4,
}

export enum WarrantyUnit {
  Day = 1,
  Month = 2,
  Year = 3,
}

export interface JsonDocument {
  isDisposable: boolean;
  rootElement: any;
}

export interface DeviceComponentDto {
  id: string;
  deviceId: string;
  deviceType: DeviceType;
  name: string;
  manufacturer?: string;
  modelNumber?: string;
  serialNumber?: string;
  specifications: JsonDocument;
  warrantyPeriod?: number;
  warrantyUnit?: WarrantyUnit;
  warrantyStartDate?: string;
  isDeleted: boolean;
  created: string;
  createdBy?: string;
  lastModified: string;
  lastModifiedBy?: string;
}

export interface CreateDeviceComponentCommand {
  deviceId: string;
  deviceType: DeviceType;
  name: string;
  manufacturer?: string;
  modelNumber?: string;
  serialNumber?: string;
  specifications: JsonDocument;
  warrantyPeriod?: number;
  warrantyUnit?: WarrantyUnit;
  warrantyStartDate?: string;
}

export interface UpdateDeviceComponentCommand {
  id: string;
  deviceType: DeviceType;
  name: string;
  manufacturer?: string;
  modelNumber?: string;
  serialNumber?: string;
  specifications: JsonDocument;
  warrantyPeriod?: number;
  warrantyUnit?: WarrantyUnit;
  warrantyStartDate?: string;
}

export interface CreateDeviceComponentCommand2 {
  deviceType: DeviceType;
  name: string;
  manufacturer?: string;
  modelNumber?: string;
  serialNumber?: string;
  specifications: JsonDocument;
  warrantyPeriod?: number;
  warrantyUnit?: WarrantyUnit;
  warrantyStartDate?: string;
}

export interface GetDeviceComponentsByDeviceIdParams {
  deviceId: string;
  isDeleted?: boolean;
}

export interface GetDeviceComponentParams {
  deviceId: string;
  isDeleted?: boolean;
}

export interface GetDeviceComponentByIdParams {
  id: string;
}
