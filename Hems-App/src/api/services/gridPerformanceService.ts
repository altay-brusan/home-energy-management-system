import apiClient from '../client/apiClient';
import { ENDPOINTS } from '../client/config';
import {
  GridPerformanceResponseDto,
  GridPeriod,
  GridDataPoint,
  GridStatistics,
} from '../types/gridPerformance.types';

export const gridPerformanceService = {
  /**
   * Get grid performance data for a device
   * @param deviceId - Device ID
   * @param period - Time period: 'monthly' | 'yearly' | 'lifetime'
   * @returns Grid performance data with buy/sell metrics
   */
  async getGridPerformance(
    deviceId: string,
    period: GridPeriod
  ): Promise<GridPerformanceResponseDto> {
    // Backend endpoint'i henüz yok, direkt fallback kullan
    // TODO: Backend hazır olduğunda aşağıdaki kod bloğunu aktif et
    /*
    try {
      const url = `${ENDPOINTS.MOBILE_DASHBOARD.GRID_PERFORMANCE}?deviceId=${deviceId}&period=${period}`;
      const response = await apiClient.get<GridPerformanceResponseDto>(url);
      return response.data;
    } catch (error: any) {
      console.log('GridPerformance endpoint error, using PowerMetrics fallback');
      return this.getGridPerformanceFromPowerMetrics(deviceId, period);
    }
    */

    // Şimdilik direkt fallback kullan
    return this.getGridPerformanceFromPowerMetrics(deviceId, period);
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
};
