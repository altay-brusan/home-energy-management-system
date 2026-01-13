import apiClient from '../client/apiClient';
import { ENDPOINTS } from '../client/config';
import {
  GridPerformanceResponseDto,
  GridPeriod,
  GridDataPoint,
  GridStatistics,
  GridPerformanceSummaryResponseDto,
} from '../types/gridPerformance.types';

// Mock veri kontrolü için flag - production'da false olacak
const USE_MOCK_DATA = true; // TODO: Env variable veya config'den oku

export const gridPerformanceService = {
  /**
   * Generate mock grid performance data
   * @param period - Time period for which to generate data
   * @returns Mock grid performance response
   */
  generateMockGridPerformance(period: GridPeriod): GridPerformanceResponseDto {
    // Base values for monthly
    let baseBuy = 450; // kWh
    let baseSell = 250; // kWh
    let dataPoints = 30; // days for monthly

    // Adjust based on period
    switch (period) {
      case 'yearly':
        baseBuy = 450 * 12; // ~5400 kWh
        baseSell = 250 * 12; // ~3000 kWh
        dataPoints = 12; // months
        break;
      case 'lifetime':
        baseBuy = 450 * 12 * 4; // ~21600 kWh (4 years)
        baseSell = 250 * 12 * 4; // ~12000 kWh (4 years)
        dataPoints = 48; // months over 4 years
        break;
    }

    // Generate data points with some variation
    const data: GridDataPoint[] = [];
    const now = new Date();

    for (let i = 0; i < dataPoints; i++) {
      const timestamp = new Date(now);
      timestamp.setDate(timestamp.getDate() - i);

      // Add random variation ±20%
      const buyVariation = 0.8 + Math.random() * 0.4;
      const sellVariation = 0.8 + Math.random() * 0.4;

      data.push({
        timestamp: timestamp.toISOString(),
        buyEnergy: (baseBuy / dataPoints) * buyVariation,
        sellEnergy: (baseSell / dataPoints) * sellVariation,
        unit: 'kWh'
      });
    }

    // Calculate statistics
    const totalBuy = data.reduce((sum, p) => sum + p.buyEnergy, 0);
    const totalSell = data.reduce((sum, p) => sum + p.sellEnergy, 0);
    const buyValues = data.map(p => p.buyEnergy);
    const sellValues = data.map(p => p.sellEnergy);

    const statistics: GridStatistics = {
      totalBuy,
      totalSell,
      netBalance: totalSell - totalBuy,
      averageBuy: totalBuy / dataPoints,
      averageSell: totalSell / dataPoints,
      peakBuy: Math.max(...buyValues),
      peakSell: Math.max(...sellValues),
      unit: 'kWh'
    };

    return {
      period,
      data,
      statistics
    };
  },

  /**
   * Get grid performance data for a device
   * @param deviceId - Device ID
   * @param period - Time period: 'monthly' | 'yearly' | 'custom'
   * @param startDate - Start date for custom period (YYYY-MM-DD)
   * @param endDate - End date for custom period (YYYY-MM-DD)
   * @returns Grid performance data with buy/sell metrics
   */
  async getGridPerformance(
    deviceId: string,
    period: 'monthly' | 'yearly' | 'custom',
    startDate?: string,
    endDate?: string
  ): Promise<GridPerformanceResponseDto> {
    try {
      let url = `${ENDPOINTS.DASHBOARD.GRID_PERFORMANCE}?deviceId=${deviceId}&period=${period}`;

      // Custom period için tarih parametrelerini ekle
      if (period === 'custom' && startDate && endDate) {
        url += `&startDate=${startDate}&endDate=${endDate}`;
      }

      const response = await apiClient.get<GridPerformanceResponseDto>(url);

      // Normalize response - convert gridImport/gridExport to buyEnergy/sellEnergy for backward compatibility
      const normalizedData = response.data.data.map(point => ({
        ...point,
        buyEnergy: point.gridImport ?? point.buyEnergy ?? 0,
        sellEnergy: point.gridExport ?? point.sellEnergy ?? 0,
      }));

      // Use total values if available, otherwise calculate from statistics or data
      const totalBuy = response.data.total?.totalGridImport ??
                       response.data.statistics?.totalBuy ??
                       normalizedData.reduce((sum, p) => sum + (p.buyEnergy ?? 0), 0);

      const totalSell = response.data.total?.totalGridExport ??
                        response.data.statistics?.totalSell ??
                        normalizedData.reduce((sum, p) => sum + (p.sellEnergy ?? 0), 0);

      return {
        ...response.data,
        data: normalizedData,
        statistics: {
          totalBuy,
          totalSell,
          netBalance: totalSell - totalBuy,
          averageBuy: normalizedData.length > 0 ? totalBuy / normalizedData.length : 0,
          averageSell: normalizedData.length > 0 ? totalSell / normalizedData.length : 0,
          peakBuy: Math.max(...normalizedData.map(p => p.buyEnergy ?? 0)),
          peakSell: Math.max(...normalizedData.map(p => p.sellEnergy ?? 0)),
          unit: normalizedData[0]?.unit || 'kWh',
        }
      };
    } catch (error: any) {
      console.error('GridPerformance endpoint error:', error);
      // Hata durumunda mock data döndür
      await new Promise(resolve => setTimeout(resolve, 500));
      return this.generateMockGridPerformance(period as GridPeriod);
    }
  },

  /**
   * Fallback: PowerMetrics'ten grid performance verilerini çıkar
   * @param deviceId - Device ID
   * @param period - Time period
   * @returns Transformed grid performance data
   */
  async getGridPerformanceFromPowerMetrics(
    deviceId: string,
    period: GridPeriod
  ): Promise<GridPerformanceResponseDto> {
    const { mobileDashboardService } = await import('./mobileDashboardService');

    // Map our period to PowerMetrics API period
    // PowerMetrics accepts: 'daily', 'weekly', 'monthly', 'yearly'
    const periodMap: Record<GridPeriod, string> = {
      monthly: 'monthly',
      yearly: 'yearly',
      lifetime: 'yearly', // Fallback to yearly for now, backend should provide lifetime endpoint
    };

    const apiPeriod = periodMap[period] || 'monthly';

    // PowerMetrics API'sini çağır
    const powerMetrics = await mobileDashboardService.getPowerMetrics(
      deviceId,
      apiPeriod
    );

    // Grid data points'leri ayır (gridPower > 0 = buy, gridPower < 0 = sell)
    const gridDataPoints: GridDataPoint[] = powerMetrics.data.map((point) => ({
      timestamp: point.timestamp,
      buyEnergy: point.gridPower > 0 ? point.gridPower : 0, // Pozitif = grid'den alım
      sellEnergy: point.gridPower < 0 ? Math.abs(point.gridPower) : 0, // Negatif = grid'e satım
      unit: point.unit,
    }));

    // İstatistikleri hesapla
    const totalBuy = gridDataPoints.reduce(
      (sum, point) => sum + point.buyEnergy,
      0
    );
    const totalSell = gridDataPoints.reduce(
      (sum, point) => sum + point.sellEnergy,
      0
    );
    const netBalance = totalSell - totalBuy;

    const buyValues = gridDataPoints
      .map((p) => p.buyEnergy)
      .filter((v) => v > 0);
    const sellValues = gridDataPoints
      .map((p) => p.sellEnergy)
      .filter((v) => v > 0);

    const averageBuy = buyValues.length > 0 ? totalBuy / buyValues.length : 0;
    const averageSell =
      sellValues.length > 0 ? totalSell / sellValues.length : 0;

    const peakBuy = buyValues.length > 0 ? Math.max(...buyValues) : 0;
    const peakSell = sellValues.length > 0 ? Math.max(...sellValues) : 0;

    const statistics: GridStatistics = {
      totalBuy,
      totalSell,
      netBalance,
      averageBuy,
      averageSell,
      peakBuy,
      peakSell,
      unit: powerMetrics.data[0]?.unit || 'kWh',
    };

    return {
      period,
      data: gridDataPoints,
      statistics,
    };
  },

  /**
   * Get grid performance summary for a device (month, year, lifetime)
   * @param deviceId - Device ID
   * @returns Grid performance summary data with gridImport and gridExport for each period
   */
  async getGridPerformanceSummary(
    deviceId: string
  ): Promise<GridPerformanceSummaryResponseDto> {
    try {
      const url = `${ENDPOINTS.DASHBOARD.GRID_PERFORMANCE_SUMMARY}?deviceId=${deviceId}`;
      const response = await apiClient.get<GridPerformanceSummaryResponseDto>(url);
      return response.data;
    } catch (error: any) {
      console.error('GridPerformanceSummary endpoint error:', error);
      throw error;
    }
  },
};
