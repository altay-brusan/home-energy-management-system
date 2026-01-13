// import { useCallback, useEffect } from 'react';
// import { useDashboardStore } from '../store/dashboardStore';
// import { useSelectedSiteUtils } from './useSelectedSite';
// import { useSitesStore } from '../store/sitesStore';
// import { useAuthStore } from '../store/authStore';

// export function useDashboard() {
//   const {
//     heroData,
//     socData,
//     energyFlowData,
//     energyProductionData,
//     energyConsumptionData,
//     powerMetricsData,
//     batteryData,
//     chargeDischargeData,
//     batteryComponentData,
//     isLoading,
//     heroLoading,
//     socLoading,
//     energyFlowLoading,
//     energyProductionLoading,
//     energyConsumptionLoading,
//     powerMetricsLoading,
//     batteryLoading,
//     chargeDischargeLoading,
//     batteryComponentLoading,
//     error,
//     heroError,
//     socError,
//     energyFlowError,
//     energyProductionError,
//     energyConsumptionError,
//     powerMetricsError,
//     batteryError,
//     chargeDischargeError,
//     batteryComponentError,
//     loadAllDashboardData,
//     loadHeroData,
//     loadSOCData,
//     loadEnergyFlowData,
//     loadEnergyProductionData,
//     loadEnergyConsumptionData,
//     loadPowerMetricsData,
//     loadBatteryData,
//     loadChargeDischargeData,
//     loadBatteryComponentData,
//     clearAllData,
//     clearError,
//     clearSpecificError,
//   } = useDashboardStore();

//   const { selectedSite } = useSelectedSiteUtils();
//   const { sites } = useSitesStore();
//   const { isAuthenticated } = useAuthStore();

//   // Get device ID from selected site
//   const getDeviceId = useCallback(() => {
//     // This should be updated when device data is available
//     // For now, we'll use the site ID as device ID placeholder
//     return selectedSite?.id || '';
//   }, [selectedSite]);

//   // Load dashboard data when site changes
//   useEffect(() => {
//     // Only load dashboard data if user is authenticated
//     if (!isAuthenticated) {
////       clearAllData();
//       return;
//     }

//     if (selectedSite?.id) {
//       const deviceId = getDeviceId();
//       if (deviceId) {
////         loadAllDashboardData(deviceId);
//       }
//     } else {
////       clearAllData();
//     }
//   }, [selectedSite?.id, isAuthenticated, loadAllDashboardData, clearAllData, getDeviceId]);

//   // Refresh dashboard data
//   const refreshDashboard = useCallback((range: 'Day' | 'Week' | 'Month' | 'Year' = 'Day') => {
//     const deviceId = getDeviceId();
//     if (deviceId) {
//       loadAllDashboardData(deviceId, range);
//     }
//   }, [loadAllDashboardData, getDeviceId]);

//   // Load specific data with range
//   const loadDataWithRange = useCallback((range: 'Day' | 'Week' | 'Month' | 'Year' = 'Day') => {
//     const deviceId = getDeviceId();
//     if (!deviceId) return;

//     const today = new Date().toISOString().split('T')[0];
//     const tomorrow = new Date(Date.now() + 24 * 60 * 60 * 1000).toISOString().split('T')[0];

//     // Load all data with new range
//     Promise.allSettled([
//       loadSOCData({ deviceId }),
//       loadEnergyFlowData({ deviceId, startDate: today, endDate: tomorrow }),
//       loadEnergyProductionData({ deviceId, period: range }),
//       loadEnergyConsumptionData({ deviceId, period: range }),
//       loadPowerMetricsData({ deviceId, period: range }),
//       loadBatteryData({ deviceId }),
//       loadChargeDischargeData({ deviceId }),
//       loadBatteryComponentData({ deviceId }),
//     ]);
//   }, [
//     getDeviceId,
//     loadSOCData,
//     loadEnergyFlowData,
//     loadEnergyProductionData,
//     loadEnergyConsumptionData,
//     loadPowerMetricsData,
//     loadBatteryData,
//     loadChargeDischargeData,
//     loadBatteryComponentData,
//   ]);

//   // Check if any data is loading
//   const isAnyLoading = isLoading || 
//     heroLoading || 
//     socLoading || 
//     energyFlowLoading || 
//     energyProductionLoading || 
//     energyConsumptionLoading || 
//     powerMetricsLoading || 
//     batteryLoading || 
//     chargeDischargeLoading || 
//     batteryComponentLoading;

//   // Check if any error exists
//   const hasAnyError = error || 
//     heroError || 
//     socError || 
//     energyFlowError || 
//     energyProductionError || 
//     energyConsumptionError || 
//     powerMetricsError || 
//     batteryError || 
//     chargeDischargeError || 
//     batteryComponentError;

//   return {
//     // Data
//     heroData,
//     socData,
//     energyFlowData,
//     energyProductionData,
//     energyConsumptionData,
//     powerMetricsData,
//     batteryData,
//     chargeDischargeData,
//     batteryComponentData,
    
//     // Loading states
//     isLoading,
//     isAnyLoading,
//     heroLoading,
//     socLoading,
//     energyFlowLoading,
//     energyProductionLoading,
//     energyConsumptionLoading,
//     powerMetricsLoading,
//     batteryLoading,
//     chargeDischargeLoading,
//     batteryComponentLoading,
    
//     // Error states
//     error,
//     hasAnyError,
//     heroError,
//     socError,
//     energyFlowError,
//     energyProductionError,
//     energyConsumptionError,
//     powerMetricsError,
//     batteryError,
//     chargeDischargeError,
//     batteryComponentError,
    
//     // Actions
//     refreshDashboard,
//     loadDataWithRange,
//     loadHeroData: () => {
//       const deviceId = getDeviceId();
//       if (deviceId) loadHeroData({ deviceId });
//     },
//     loadSOCData: (range: 'Day' | 'Week' | 'Month' | 'Year' = 'Day') => {
//       const deviceId = getDeviceId();
//       if (deviceId) {
//         loadSOCData({ deviceId });
//       }
//     },
//     loadEnergyFlowData: (range: 'Day' | 'Week' | 'Month' | 'Year' = 'Day') => {
//       const deviceId = getDeviceId();
//       if (deviceId) {
//         const today = new Date().toISOString().split('T')[0];
//         const tomorrow = new Date(Date.now() + 24 * 60 * 60 * 1000).toISOString().split('T')[0];
//         loadEnergyFlowData({
//           deviceId,
//           startDate: today,
//           endDate: tomorrow,
//         });
//       }
//     },
//     loadEnergyProductionData: (range: 'Day' | 'Week' | 'Month' | 'Year' = 'Day') => {
//       const deviceId = getDeviceId();
//       if (deviceId) {
//         loadEnergyProductionData({
//           deviceId,
//           period: range,
//         });
//       }
//     },
//     loadEnergyConsumptionData: (range: 'Day' | 'Week' | 'Month' | 'Year' = 'Day') => {
//       const deviceId = getDeviceId();
//       if (deviceId) {
//         loadEnergyConsumptionData({
//           deviceId,
//           period: range,
//         });
//       }
//     },
//     loadPowerMetricsData: (range: 'Day' | 'Week' | 'Month' | 'Year' = 'Day') => {
//       const deviceId = getDeviceId();
//       if (deviceId) {
//         loadPowerMetricsData({
//           deviceId,
//           period: range,
//         });
//       }
//     },
//     loadBatteryData: (range: 'Day' | 'Week' | 'Month' | 'Year' = 'Day') => {
//       const deviceId = getDeviceId();
//       if (deviceId) {
//         loadBatteryData({ deviceId });
//       }
//     },
//     loadChargeDischargeData: (range: 'Day' | 'Week' | 'Month' | 'Year' = 'Day') => {
//       const deviceId = getDeviceId();
//       if (deviceId) {
//         loadChargeDischargeData({ deviceId });
//       }
//     },
//     loadBatteryComponentData: (range: 'Day' | 'Week' | 'Month' | 'Year' = 'Day') => {
//       const deviceId = getDeviceId();
//       if (deviceId) {
//         loadBatteryComponentData({ deviceId });
//       }
//     },
    
//     // Utility functions
//     clearAllData,
//     clearError,
//     clearSpecificError,
//     getDeviceId,
//   };
// }
