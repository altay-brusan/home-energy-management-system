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

// Device Summary Types (for DeviceSummary endpoint)
export type ComponentType = 'inverter' | 'battery';

export interface ComponentSummaryBaseDto {
  id: string;
  type: ComponentType;
  name: string;
  model: string;
  manufacturer: string;
  serialNumber: string;
}

export interface InverterSummaryDto extends ComponentSummaryBaseDto {
  type: 'inverter';
  activePowerKw: number | null;
}

export interface BatteryPackDto {
  id: string;
  type: 'battery';
  name: string;
  model: string;
  manufacturer: string;
  serialNumber: string;
  componentId: string;
  bmsAddress: number;
  stateOfCharge: number | null;
  stateOfHealth: number | null;
  voltage: number | null;
  current: number | null;
  temperature: number | null;
  firmwareVersion: string | null;
}

export interface BatterySummaryDto extends ComponentSummaryBaseDto {
  type: 'battery';
  stateOfCharge: number | null;
  packs: BatteryPackDto[];
}

export type ComponentSummaryDto = InverterSummaryDto | BatterySummaryDto;

export interface DeviceSummaryResponseDto {
  components: ComponentSummaryDto[];
}

// Type guards
export function isInverterSummary(component: ComponentSummaryDto): component is InverterSummaryDto {
  return component.type === 'inverter';
}

export function isBatterySummary(component: ComponentSummaryDto): component is BatterySummaryDto {
  return component.type === 'battery';
}

// Device Component Details Types
export interface DeviceDetailItemDto {
  key: string;
  value: any;
  unit: string | null;
  labelKey: string;
}

export interface DeviceDetailsResponseDto {
  details: DeviceDetailItemDto[];
}

export type DetailGroup = 'realtime' | 'parameter';

export interface GroupedDetails {
  realtime: DeviceDetailItemDto[];
  parameter: DeviceDetailItemDto[];
}
