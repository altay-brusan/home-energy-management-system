// Device Notifications Types
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

export interface DeviceNotificationDto {
  id: string;
  deviceId: string;
  siteId: string;
  setupDeviceId: string;
  deviceNotificationTypeId: string;
  timestamp: string;
  additionalData: string;
  isProcessed: boolean;
  isDeleted: boolean;
  created: string;
  deviceNotificationType: DeviceNotificationTypeDto;
  deviceManufacturer: string;
  siteName: string;
}

export interface CreateDeviceNotificationCommand {
  deviceId: string;
  notificationTypeCode: string;
  timestamp: string;
  additionalData: string;
}

export interface PaginatedListOfDeviceNotificationDto {
  items: DeviceNotificationDto[];
  pageNumber: number;
  totalPages: number;
  totalCount: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface GetDeviceNotificationsParams {
  SiteId: string;
  PageNumber: number;
  PageSize: number;
  IsToday?: boolean | null;
  IsProcessed?: boolean | null;
  Category?: string | null;
  IncludeDeleted?: boolean | null;
}

export interface GetDeletedDeviceNotificationsParams {
  SiteId: string;
  PageNumber: number;
  PageSize: number;
  IsToday?: boolean | null;
  Category?: string | null;
}

export interface MarkDeviceNotificationAsReadParams {
  id: string;
}

export interface DeleteDeviceNotificationParams {
  id: string;
}

export interface RestoreDeviceNotificationParams {
  id: string;
}

export interface PermanentDeleteDeviceNotificationParams {
  id: string;
}