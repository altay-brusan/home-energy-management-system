import { useSitesStore } from '../store/sitesStore';

/**
 * Hook to get the currently selected site
 * @returns The selected site object or null if no site is selected
 */
export const useSelectedSite = () => {
  const selectedSite = useSitesStore(state => state.selectedSite);
  return selectedSite;
};

/**
 * Hook to get selected site utilities
 * @returns Object with selected site and utility functions
 */
export const useSelectedSiteUtils = () => {
  const selectedSite = useSitesStore(state => state.selectedSite);
  const selectSiteById = useSitesStore(state => state.selectSiteById);
  const setSelectedSite = useSitesStore(state => state.setSelectedSite);
  const loadSelectedSiteFromStorage = useSitesStore(state => state.loadSelectedSiteFromStorage);

  return {
    selectedSite,
    selectSiteById,
    setSelectedSite,
    loadSelectedSiteFromStorage,
    // Utility functions
    isSiteSelected: (siteId: string) => selectedSite?.id === siteId,
    getSelectedSiteName: () => selectedSite?.name || 'No site selected',
    getSelectedSiteAddress: () => {
      if (!selectedSite) return 'No site selected';
      const { city, country } = selectedSite;
      return `${city || ''}, ${country || ''}`.replace(/^,\s*|,\s*$/g, '');
    },
  };
};

export default useSelectedSite;
