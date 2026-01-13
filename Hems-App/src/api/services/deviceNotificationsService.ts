import apiClient from '../client/apiClient';
import { ENDPOINTS } from '../client/config';
import {
  CreateDeviceNotificationCommand,
  PaginatedListOfDeviceNotificationDto,
  DeviceNotificationTypeDto,
  GetDeviceNotificationsParams,
  GetDeletedDeviceNotificationsParams,
  MarkDeviceNotificationAsReadParams,
  DeleteDeviceNotificationParams,
  RestoreDeviceNotificationParams,
  PermanentDeleteDeviceNotificationParams,
} from '../types/deviceNotifications.types';

export const deviceNotificationsService = {
  createDeviceNotification: async (
    command: CreateDeviceNotificationCommand
  ): Promise<void> => {
    await apiClient.post<void>(ENDPOINTS.DEVICE_NOTIFICATIONS.CREATE, command);
  },

  getDeviceNotifications: async (
    params: GetDeviceNotificationsParams
  ): Promise<PaginatedListOfDeviceNotificationDto> => {
    const queryParams = new URLSearchParams();
    queryParams.append('SiteId', params.SiteId);
    queryParams.append('PageNumber', params.PageNumber.toString());
    queryParams.append('PageSize', params.PageSize.toString());

    if (params.IsToday !== undefined && params.IsToday !== null) {
      queryParams.append('IsToday', params.IsToday.toString());
    }
    if (params.IsProcessed !== undefined && params.IsProcessed !== null) {
      queryParams.append('IsProcessed', params.IsProcessed.toString());
    }
    if (params.Category) {
      queryParams.append('Category', params.Category);
    }
    if (params.IncludeDeleted !== undefined && params.IncludeDeleted !== null) {
      queryParams.append('IncludeDeleted', params.IncludeDeleted.toString());
    }

    const url = `${ENDPOINTS.DEVICE_NOTIFICATIONS.GET_ALL}?${queryParams.toString()}`;
    const response = await apiClient.get<PaginatedListOfDeviceNotificationDto>(url);
    return response.data;
  },

  getDeletedDeviceNotifications: async (
    params: GetDeletedDeviceNotificationsParams
  ): Promise<PaginatedListOfDeviceNotificationDto> => {
    const queryParams = new URLSearchParams();
    queryParams.append('SiteId', params.SiteId);
    queryParams.append('PageNumber', params.PageNumber.toString());
    queryParams.append('PageSize', params.PageSize.toString());

    if (params.IsToday !== undefined && params.IsToday !== null) {
      queryParams.append('IsToday', params.IsToday.toString());
    }
    if (params.Category) {
      queryParams.append('Category', params.Category);
    }

    const url = `${ENDPOINTS.DEVICE_NOTIFICATIONS.GET_DELETED}?${queryParams.toString()}`;
    const response = await apiClient.get<PaginatedListOfDeviceNotificationDto>(url);
    return response.data;
  },

  getDeviceNotificationTypes: async (): Promise<DeviceNotificationTypeDto[]> => {
    const response = await apiClient.get<DeviceNotificationTypeDto[]>(ENDPOINTS.DEVICE_NOTIFICATIONS.GET_TYPES);
    return response.data;
  },

  markDeviceNotificationAsRead: async (params: MarkDeviceNotificationAsReadParams): Promise<void> => {
    const url = `${ENDPOINTS.DEVICE_NOTIFICATIONS.MARK_READ.replace(':id', params.id)}`;
    await apiClient.put<void>(url);
  },

  markAllNotificationsAsRead: async (siteId: string): Promise<void> => {
    const url = ENDPOINTS.DEVICE_NOTIFICATIONS.MARK_ALL_READ.replace(':siteId', siteId);
    await apiClient.put<void>(url);
  },

  deleteDeviceNotification: async (params: DeleteDeviceNotificationParams): Promise<void> => {
    const url = `${ENDPOINTS.DEVICE_NOTIFICATIONS.DELETE.replace(':id', params.id)}`;
    await apiClient.delete<void>(url);
  },

  restoreDeviceNotification: async (params: RestoreDeviceNotificationParams): Promise<void> => {
    const url = `${ENDPOINTS.DEVICE_NOTIFICATIONS.RESTORE.replace(':id', params.id)}`;
    await apiClient.put<void>(url);
  },

  permanentDeleteDeviceNotification: async (params: PermanentDeleteDeviceNotificationParams): Promise<void> => {
    const url = `${ENDPOINTS.DEVICE_NOTIFICATIONS.PERMANENT_DELETE.replace(':id', params.id)}`;
    await apiClient.delete<void>(url);
  },
};