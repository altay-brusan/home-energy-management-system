import { SiteDto } from "./sites.types";

export interface AdminGetSitesParams {
  pageNumber: number;
    pageSize?: number;
  
  filters?: {
    country?: string | null;
    city?: string | null;
    district?: string | null;
      UserEmail?: string | null;
  };
}

export interface AdminSitesResponse {
  items: SiteDto[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  
}