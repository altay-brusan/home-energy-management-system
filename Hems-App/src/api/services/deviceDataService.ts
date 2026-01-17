import apiClient from '../client/apiClient';
import { ENDPOINTS } from '../client/config';
import {
  CreateDeviceDataCommand,
  CreateDeviceEnergyDataCommand,
  BulkCreateDeviceDataCommand,
  BulkCreateDeviceDataResult,
  DeviceDataDto,
  GetDeviceDataParams,
  PaginatedDeviceDataList,
} from '../types/deviceData.types';
import { DeviceStatusType } from '../types/signalR.types';

// Response from GET /api/DeviceData/Status/{deviceId}
export interface DeviceHeartbeatStatusDto {
  deviceId: string;
  status: DeviceStatusType;
  timestamp: string;
  lastHeartbeat?: string;
  lastPowerFlow?: string;
  missedHeartbeats: number;
}

export const deviceDataService = {
  async getAll(params: GetDeviceDataParams): Promise<PaginatedDeviceDataList> {
    const queryParams = new URLSearchParams();
    
    if (params.deviceId) queryParams.append('DeviceId', params.deviceId);
    if (params.dataType) queryParams.append('DataType', params.dataType);
    queryParams.append('PageNumber', params.pageNumber.toString());
    queryParams.append('PageSize', params.pageSize.toString());
    if (params.userId) queryParams.append('UserId', params.userId);

    const url = `${ENDPOINTS.DEVICE_DATA.GET_ALL}?${queryParams.toString()}`;
    const response = await apiClient.get<PaginatedDeviceDataList>(url);
    return response.data;
  },

  async create(data: CreateDeviceDataCommand): Promise<number> {
    const response = await apiClient.post<number>(ENDPOINTS.DEVICE_DATA.CREATE, data);
    return response.data;
  },

  async createEnergyData(data: CreateDeviceEnergyDataCommand): Promise<number> {
    const response = await apiClient.post<number>(ENDPOINTS.DEVICE_DATA.CREATE_ENERGY, data);
    return response.data;
  },

  async bulkCreate(data: BulkCreateDeviceDataCommand): Promise<BulkCreateDeviceDataResult> {
    const response = await apiClient.post<BulkCreateDeviceDataResult>(ENDPOINTS.DEVICE_DATA.BULK_CREATE, data);
    return response.data;
  },

  /**
   * Get device heartbeat status via REST API.
   * Use this for immediate status on page load, then use SignalR for live updates.
   */
  async getStatus(deviceId: string): Promise<DeviceHeartbeatStatusDto> {
    const url = ENDPOINTS.DEVICE_DATA.GET_STATUS.replace(':deviceId', deviceId);
    const response = await apiClient.get<DeviceHeartbeatStatusDto>(url);
    return response.data;
  },
};
