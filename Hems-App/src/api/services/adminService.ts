import { sitesService } from "./sitesService";
import { AdminGetSitesParams, AdminSitesResponse } from "../types/admin.types";

export const adminService = {
  async getAdminSites(params: AdminGetSitesParams): Promise<AdminSitesResponse> {
    const { pageNumber, pageSize: requestedPageSize, filters } = params;
    const hasFilters =
      filters?.country || filters?.city || filters?.district || filters?.UserEmail;
    const pageSize = requestedPageSize || (hasFilters ? 10 : 100);

    const apiParams = {
      PageNumber: pageNumber,
      PageSize: pageSize,
      Country: filters?.country || undefined,
      City: filters?.city || undefined,
      District: filters?.district || undefined,
     UserEmail: filters?.UserEmail || undefined,
    };

    console.log('🔵 [AdminService] Calling sitesService.getAll with params:', apiParams);

    const raw: any = await sitesService.getAll(apiParams as any);

    console.log('🔵 [AdminService] Raw API response:', {
      type: typeof raw,
      hasData: raw && 'data' in raw,
      raw: raw,
    });

    const paginatedData = (raw && typeof raw === 'object' && 'data' in raw && raw.data) ? raw.data : raw;const response: AdminSitesResponse = {
      items: paginatedData?.items || [],
      pageNumber: paginatedData?.pageNumber ?? pageNumber,
      pageSize: paginatedData?.pageSize ?? pageSize,
      totalCount: paginatedData?.totalCount ?? 0,
    };

    console.log('🔵 [AdminService] Formatted response:', response);

    return response;
  },
};
