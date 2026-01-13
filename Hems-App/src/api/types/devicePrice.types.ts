/**
 * Device Price API Types
 * Endpoint: /DeviceSettings/device/{deviceId}/price
 */

export interface DevicePriceDto {
  id: string;
  sellPrice: number;
  buyPrice: number;
  beginHour: number; // 0-24
  endHour: number; // 0-24
  created: string;
}

export interface DevicePriceResponse {
  success: boolean;
  errorMessage: string | null;
  deviceId: string;
  unit: string;
  currency: string;
  prices: DevicePriceDto[];
}

export interface UpdateDevicePriceItem {
  sellPrice: number;
  buyPrice: number;
  beginHour: number; // 0-24
  endHour: number; // 0-24
}

export interface UpdateDevicePriceRequest {
  unit: string;
  currency: string;
  prices: UpdateDevicePriceItem[];
}
