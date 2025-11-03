import AsyncStorage from '@react-native-async-storage/async-storage';
import { create } from 'zustand';
import { setupsService } from '../api/services/setupsService';
import { sitesService } from '../api/services/sitesService';
import { UserSetupDeviceDto } from '../api/types/setups.types';
import {
  CreateSiteCommand,
  GetSitesParams,
  PaginatedSiteList,
  SiteDto,
  UpdateSiteCommand,
  SitesDashboardDto,
} from '../api/types/sites.types';

interface SitesState {
  sites: SiteDto[];
  sitesDashboard: SitesDashboardDto | null;
  currentSite: SiteDto | null;
  selectedSite: SiteDto | null;
  selectedSiteDevice: UserSetupDeviceDto | null;
  paginatedSites: PaginatedSiteList | null;
  isLoading: boolean;
  error: string | null;

  getAllSites: (params: GetSitesParams) => Promise<PaginatedSiteList>;
  getUserSites: () => Promise<SitesDashboardDto>;
  getSiteById: (id: string) => Promise<SiteDto>;
  createSite: (data: CreateSiteCommand) => Promise<string>;
  updateSite: (id: string, data: UpdateSiteCommand) => Promise<void>;
  deleteSite: (id: string) => Promise<void>;
  setCurrentSite: (site: SiteDto | null) => void;
  setSelectedSite: (site: SiteDto | null) => void;
  selectSiteById: (id: string) => Promise<void>;
  loadSelectedSiteFromStorage: () => Promise<void>;
  getSiteDevice: (siteId: string) => Promise<UserSetupDeviceDto | null>;
  loadSelectedSiteDevice: () => Promise<void>;
  clearError: () => void;
  clearSites: () => Promise<void>;
}

export const useSitesStore = create<SitesState>((set, get) => ({
  sites: [],
  sitesDashboard: null,
  currentSite: null,
  selectedSite: null,
  selectedSiteDevice: null,
  paginatedSites: null,
  isLoading: false,
  error: null,

  getAllSites: async (params: GetSitesParams) => {
    set({ isLoading: true, error: null });
    try {
      const result = await sitesService.getAll(params);
      set({ paginatedSites: result, isLoading: false });
      return result;
    } catch (error: any) {
      set({
        error: error.detail || error.message || 'Failed to fetch sites',
        isLoading: false,
      });
      throw error;
    }
  },

  getUserSites: async () => {
    set({ isLoading: true, error: null });
    try {
      const sitesDashboard = await sitesService.getUserSites();
      const sites = sitesDashboard.siteList || [];
      set({ sitesDashboard, sites, isLoading: false });

      const { loadSelectedSiteFromStorage } = get();
      await loadSelectedSiteFromStorage();

      return sitesDashboard;
    } catch (error: any) {
      // Canceled request'leri sessizce handle et - normal akış bu
      if (error?.message?.includes('canceled') || error?.name === 'CanceledError' || error?.code === 'ERR_CANCELED') {
        set({ isLoading: false });
        return undefined as any;
      }

      if (error.response?.status === 401) {
        set({ isLoading: false });
        throw error;
      }

      set({
        error: error.detail || error.message || 'Failed to fetch user sites',
        isLoading: false,
      });
      throw error;
    }
  },

  getSiteById: async (id: string) => {
    set({ isLoading: true, error: null });
    try {
      const site = await sitesService.getById(id);
      set({ currentSite: site, isLoading: false });
      return site;
    } catch (error: any) {
      set({
        error: error.detail || error.message || 'Failed to fetch site',
        isLoading: false,
      });
      throw error;
    }
  },

  createSite: async (data: CreateSiteCommand) => {
    set({ isLoading: true, error: null });
    try {
      const siteId = await sitesService.create(data);
      await get().getUserSites();
      set({ isLoading: false });
      return siteId;
    } catch (error: any) {
      set({
        error: error.detail || error.message || 'Failed to create site',
        isLoading: false,
      });
      throw error;
    }
  },

  updateSite: async (id: string, data: UpdateSiteCommand) => {
    set({ isLoading: true, error: null });
    try {
      await sitesService.update(id, data);
      await get().getUserSites();
      set({ isLoading: false });
    } catch (error: any) {
      set({
        error: error.detail || error.message || 'Failed to update site',
        isLoading: false,
      });
      throw error;
    }
  },

  deleteSite: async (id: string) => {
    set({ isLoading: true, error: null });
    try {
      await sitesService.deleteMobile(id);
      const currentSites = get().sites;
      if (!currentSites || !Array.isArray(currentSites)) {
        set({ isLoading: false });
        return;
      }

      const updatedSites = currentSites.filter((s) => s.id !== id);
      set({ sites: updatedSites, isLoading: false });

      try {
        const sitesForStorage = updatedSites.map(site => ({
          id: site.id,
          name: site.name,
          address: `${site.city || ''}, ${site.country || ''}`.replace(/^,\s*|,\s*$/g, ''),
          hasDevice: true
        }));
        await AsyncStorage.setItem('@user_homes', JSON.stringify(sitesForStorage));
      } catch (storageError) {
        // Storage error - silent
      }
    } catch (error: any) {

      set({
        error: error.detail || error.message || 'Failed to delete site',
        isLoading: false,
      });
      throw error;
    }
  },

  setCurrentSite: (site: SiteDto | null) => {
    set({ currentSite: site });
  },

  setSelectedSite: (site: SiteDto | null) => {
    set({ selectedSite: site, selectedSiteDevice: null });
    // Also save to AsyncStorage
    if (site) {
      AsyncStorage.setItem('@selected_home', site.id).catch(() => {});
    } else {
      AsyncStorage.removeItem('@selected_home').catch(() => {});
    }
  },

  selectSiteById: async (id: string) => {
    const { sites } = get();
    if (!sites || !Array.isArray(sites)) {
      return;
    }

    const site = sites.find(s => s.id === id);
    if (site) {
      set({ selectedSite: site });
      await AsyncStorage.setItem('@selected_home', site.id);
      
      // loadSelectedSiteDevice çağrısını kaldır - gereksiz API çağrısı ve token refresh tetikliyor
      // Device bilgisi lazy loading ile alınacak (dashboard, etc.)
      // const { loadSelectedSiteDevice } = get();
    }
  },

  loadSelectedSiteFromStorage: async () => {
    try {
      const selectedHomeId = await AsyncStorage.getItem('@selected_home');
      if (selectedHomeId) {
        const { sites } = get();
        if (!sites || !Array.isArray(sites)) {
          await AsyncStorage.removeItem('@selected_home');
          set({ selectedSite: null, selectedSiteDevice: null });
          return;
        }

        const site = sites.find(s => s.id === selectedHomeId);
        if (site) {
          set({ selectedSite: site });

          // loadSelectedSiteDevice çağrısını kaldır - gereksiz API çağrısı
          // const { loadSelectedSiteDevice } = get();
          // await loadSelectedSiteDevice();
        } else {
          await AsyncStorage.removeItem('@selected_home');
          set({ selectedSite: null, selectedSiteDevice: null });
        }
      }
    } catch (error) {
      // Silent error handling
    }
  },

  getSiteDevice: async (siteId: string) => {
    try {
      const { sites } = get();
      if (!sites || !Array.isArray(sites)) {
        return null;
      }

      const site = sites.find(s => s.id === siteId);
      if (!site || !site.hasDevice) {
        return null;
      }

      const deviceInfo = await setupsService.getSetupDevice({ siteId });
      return deviceInfo;
    } catch (error: any) {
      return null;
    }
  },

  loadSelectedSiteDevice: async () => {
    try {
      const { selectedSite } = get();
      if (selectedSite && selectedSite.hasDevice) {
        const deviceInfo = await setupsService.getSetupDevice({ siteId: selectedSite.id });
        set({ selectedSiteDevice: deviceInfo });
      } else {
        set({ selectedSiteDevice: null });
      }
    } catch (error: any) {
      // 401 hatasını silent handle et - token yenileme sonrası tekrar denenecek
      if (error?.response?.status === 401) {
        set({ selectedSiteDevice: null });
        throw error;
      }
      
      set({ selectedSiteDevice: null });
    }
  },

  clearError: () => {
    set({ error: null });
  },

  clearSites: async () => {
    try {
      // AsyncStorage'dan site verilerini de temizle
      await AsyncStorage.multiRemove(['@user_homes', '@selected_home']);
    } catch (error) {
      // Silent error handling
    }
    
    set({
      sites: [],
      sitesDashboard: null,
      currentSite: null,
      selectedSite: null,
      selectedSiteDevice: null,
      paginatedSites: null,
      isLoading: false,
      error: null,
    });
  },
}));

