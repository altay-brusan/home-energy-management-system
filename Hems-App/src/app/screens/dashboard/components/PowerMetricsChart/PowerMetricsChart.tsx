import React, { useState, useEffect, useCallback } from 'react';
import { View, Text, ActivityIndicator, Dimensions } from 'react-native';
import { PowerMetricsChartProps, PowerMetric, MetricConfig, ChartDimensions } from './PowerMetricsChart.types';
import { MetricsHeader } from './MetricsHeader';
import { LineChart } from './LineChart';
import { ChartLegend } from './ChartLegend';
import { SunInfo } from './SunInfo';
import { styles, dashboardChartStyles } from './PowerMetricsChart.styles';
import { mobileDashboardService, PowerMetricsDataPoint } from '../../../../../api/services/mobileDashboardService';
import { useSitesStore } from '../../../../../store/sitesStore';

const CHART_HEIGHT = 300;
const CHART_MARGINS = {
  marginTop: 20,
  marginRight: 20,
  marginBottom: 40,
  marginLeft: 50,
};

export const PowerMetricsChart: React.FC<PowerMetricsChartProps> = ({
  deviceId,
  refreshTrigger,
}) => {
  const [data, setData] = useState<PowerMetric[]>([]);
  const [currentValues, setCurrentValues] = useState<PowerMetric | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [dimensions, setDimensions] = useState<ChartDimensions>({
    width: Dimensions.get('window').width - 32,
    height: CHART_HEIGHT,
    ...CHART_MARGINS,
  });

  // Get device from store if not provided
  const { selectedSiteDevice } = useSitesStore();
  const activeDeviceId = deviceId || selectedSiteDevice?.device?.id;

  // Metric configurations
  const [metrics, setMetrics] = useState<MetricConfig[]>([
    { key: 'solar', label: 'Solar Produced', color: '#FFD700', visible: true },
    { key: 'battery', label: 'Battery', color: '#00CED1', visible: true },
    { key: 'grid', label: 'Grid', color: '#9370DB', visible: true },
    { key: 'load', label: 'Load Consumed', color: '#BA55D3', visible: true },
  ]);

  // Transform API data to our format
  const transformData = useCallback((apiData: PowerMetricsDataPoint[]): PowerMetric[] => {
    if (!apiData || !Array.isArray(apiData)) {
      return [];
    }

    return apiData
      .filter(point => point && point.timestamp)
      .map((point) => ({
        timestamp: point.timestamp,
        solar: point.solarPower || 0,
        battery: point.batteryPower || 0,
        grid: point.gridPower || 0,
        load: point.loadPower || 0,
      }));
  }, []);

  // Fetch power metrics data
  const fetchData = useCallback(async () => {
    if (!activeDeviceId) {
      setError('No device selected');
      setIsLoading(false);
      return;
    }

    try {
      setIsLoading(true);
      setError(null);

      // Fetch daily data
      const response = await mobileDashboardService.getPowerMetrics(
        activeDeviceId,
        'today'
      );

      if (response?.data && Array.isArray(response.data)) {
        const transformedData = transformData(response.data);

        if (transformedData.length > 0) {
          setData(transformedData);
          // Set current values (last data point)
          setCurrentValues(transformedData[transformedData.length - 1]);
        } else {
          setData([]);
          setError('No valid data points');
        }
      } else {
        setData([]);
        setError('No data available');
      }
    } catch (err) {
      setError('Failed to load power metrics');
    } finally {
      setIsLoading(false);
    }
  }, [activeDeviceId, transformData]);

  // Toggle metric visibility
  const handleToggleMetric = useCallback((key: string) => {
    setMetrics((prev) =>
      prev.map((metric) =>
        metric.key === key ? { ...metric, visible: !metric.visible } : metric
      )
    );
  }, []);

  // Handle dimensions update on screen rotation
  useEffect(() => {
    const updateDimensions = () => {
      setDimensions({
        width: Dimensions.get('window').width - 32,
        height: CHART_HEIGHT,
        ...CHART_MARGINS,
      });
    };

    const subscription = Dimensions.addEventListener('change', updateDimensions);
    return () => subscription?.remove();
  }, []);

  // Fetch data on mount and when refresh triggers
  useEffect(() => {
    fetchData();
  }, [fetchData, refreshTrigger]);

  // Auto-refresh every 5 minutes
  useEffect(() => {
    const interval = setInterval(() => {
      fetchData();
    }, 5 * 60 * 1000);

    return () => clearInterval(interval);
  }, [fetchData]);

  if (isLoading && data.length === 0) {
    return (
      <View style={[styles.container, styles.loadingContainer]}>
        <ActivityIndicator size="large" color="#ffffff" />
        <Text style={dashboardChartStyles.loadingText}>Loading power metrics...</Text>
      </View>
    );
  }

  if (error && data.length === 0) {
    return (
      <View style={[styles.container, styles.errorContainer]}>
        <Text style={styles.errorText}>{error}</Text>
      </View>
    );
  }

  return (
    <View style={styles.container}>
      <MetricsHeader
        currentValues={currentValues}
        isLoading={isLoading}
      />

      {data.length > 0 && (
        <>
          <View style={styles.chartContainer}>
            <LineChart
              data={data}
              metrics={metrics}
              dimensions={dimensions}
            />
          </View>

          <SunInfo sunrise="07:35 AM" sunset="04:58 PM" />

          <ChartLegend
            metrics={metrics}
            onToggleMetric={handleToggleMetric}
          />
        </>
      )}
    </View>
  );
};