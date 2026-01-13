import { useSitesStore } from '../store/sitesStore';

/**
 * Hook to access the selected site's device information
 * This provides easy access to device data throughout the app
 */
export const useSelectedSiteDevice = () => {
  const { selectedSite, selectedSiteDevice, loadSelectedSiteDevice } = useSitesStore();

  return {
    selectedSite,
    selectedSiteDevice,
    loadSelectedSiteDevice,
    hasDevice: selectedSite?.hasDevice || false,
    device: selectedSiteDevice?.device,
    externalDevices: selectedSiteDevice?.externalDevices || [],
    setupInfo: selectedSiteDevice ? {
      setupDeviceId: selectedSiteDevice.setupDeviceId,
      setupId: selectedSiteDevice.setupId,
      setupName: selectedSiteDevice.setupName,
      siteId: selectedSiteDevice.siteId,
      siteName: selectedSiteDevice.siteName,
    } : null,
  };
};
