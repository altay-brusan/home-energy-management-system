import { PaginatedList } from './common.types';
import { DeviceDto } from './devices.types';

export interface SetupDto {
  id: string;
  siteId: string;
  name: string;
  setupDevice?: SetupDeviceDto | null;
  created: string;
  createdBy?: string | null;
  lastModified?: string | null;
  lastModifiedBy?: string | null;
}

export interface SetupDeviceDto {
  id: string;
  setupId: string;
  deviceId: string;
  device: DeviceDto;
  externalDevices: ExternalDeviceDto[];
  created: string;
  createdBy?: string | null;
  lastModified: string;
  lastModifiedBy?: string | null;
}

export interface ExternalDeviceDto {
  id: string;
  setupDeviceId: string;
  name: string;
  deviceType: string;
  specifications: any;
  created: string;
  createdBy?: string | null;
  lastModified: string;
  lastModifiedBy?: string | null;
}

export interface UserSetupDeviceDto {
  setupDeviceId: string;
  setupId: string;
  setupName: string;
  siteId: string;
  siteName: string;
  device: DeviceDto;
  externalDevices: ExternalDeviceDto[];
}

export interface CreateSetupCommand {
  siteId: string;
  name: string;
  deviceQRCode?: string | null;
  deviceSerialNumber?: string | null;
}

export interface UpdateSetupCommand {
  id: string;
  siteId: string;
  name: string;
}

export interface GetSetupsParams {
  siteId?: string | null;
  pageNumber: number;
  pageSize: number;
}

export interface GetSetupDeviceParams {
  siteId: string;
}

export type PaginatedSetupList = PaginatedList<SetupDto>;

