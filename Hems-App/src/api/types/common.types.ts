import { WarrantyUnit } from "./deviceComponents.types";

export interface PaginatedList<T> {
  items: T[];
  pageNumber: number;
  totalPages: number;
   pageSize: number; 
  totalCount: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface PaginationParams {
  pageNumber: number;
  pageSize: number;
}

export interface ApiError {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  message?: string;
  instance?: string;
  errors?: Record<string, string[]>;
  url?: string;
}

export interface HttpValidationProblemDetails extends ApiError {
  errors: Record<string, string[]>;
}


export enum PriorityLevel {
  None = 0,
  Low = 1,
  Medium = 2,
  High = 3,
}

export interface WarrantyInfoDto {
  warrantyPeriod?: number | null;
  warrantyUnit?: WarrantyUnit | null;
  warrantyStartDate?: string | null;
  warrantyEndDate?: string | null;
  daysRemaining: number;
  isExpired: boolean;
}

