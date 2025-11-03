import { PaginatedList } from './common.types';

export interface SiteDto {
  id: string;
  userId: string;
  name: string;
  country?: string | null;
  city?: string | null;
  district?: string | null;
  addressLine?: string | null;
  latitude?: number | null;
  longitude?: number | null;
  timezone: string;
  isDeleted: boolean;
  hasDevice: boolean;
  created: string;
  createdBy?: string | null;
  lastModified?: string | null;
  lastModifiedBy?: string | null;
  power?: string | null;
}

export interface CreateSiteCommand {
  name: string;
  country?: string | null;
  city?: string | null;
  district?: string | null;
  addressLine?: string | null;
  latitude?: number | null;
  longitude?: number | null;
  timezone: string;
}

export interface UpdateSiteCommand {
  id: string;
  name: string;
  country?: string | null;
  city?: string | null;
  district?: string | null;
  addressLine?: string | null;
  latitude?: number | null;
  longitude?: number | null;
  timezone: string;
}

export interface GetSitesParams {
  UserId?: string | null;
  UserEmail?: string | null;
  userName?: string | null;
  pageNumber: number;
  pageSize: number;
  name?: string | null;
  country?: string | null;
  city?: string | null;
  addressLine?: string | null;
  district?: string | null;
}

export type PaginatedSiteList = PaginatedList<SiteDto>;

export interface SitesDashboardDto {
  totalHomes: number;
  totalDevices: number;
  totalPower: string;
  siteList: SiteWithPowerDto[];
}

export interface SiteWithPowerDto extends SiteDto {
  power: string;
}

