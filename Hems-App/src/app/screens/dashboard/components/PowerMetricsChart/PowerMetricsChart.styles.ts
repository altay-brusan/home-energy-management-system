import { StyleSheet } from 'react-native';

export const styles = StyleSheet.create({
  container: {
    backgroundColor: '#1a1a2e',
    borderRadius: 12,
    padding: 16,
    marginVertical: 8,
  },
  loadingContainer: {
    height: 400,
    justifyContent: 'center',
    alignItems: 'center',
  },
  errorContainer: {
    height: 200,
    justifyContent: 'center',
    alignItems: 'center',
    padding: 20,
  },
  errorText: {
    color: '#ff6b6b',
    fontSize: 14,
    textAlign: 'center',
    marginTop: 8,
  },
  chartContainer: {
    marginVertical: 16,
  },
  svgContainer: {
    backgroundColor: 'rgba(255, 255, 255, 0.02)',
    borderRadius: 8,
  },
});

export const metricsHeaderStyles = StyleSheet.create({
  container: {
    backgroundColor: 'rgba(255, 255, 255, 0.08)',
    borderRadius: 8,
    padding: 12,
    marginBottom: 16,
  },
  timeLabel: {
    color: '#ffffff',
    fontSize: 18,
    fontWeight: '600',
    marginBottom: 12,
  },
  metricsGrid: {
    flexDirection: 'column',
    gap: 8,
  },
  metricRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 12,
  },
  metricDot: {
    width: 10,
    height: 10,
    borderRadius: 5,
  },
  metricLabel: {
    color: '#b0b0b0',
    fontSize: 13,
    flex: 1,
  },
  metricValue: {
    color: '#ffffff',
    fontSize: 14,
    fontWeight: '500',
  },
  loadingText: {
    color: '#666',
    fontSize: 14,
  },
});

export const chartLegendStyles = StyleSheet.create({
  container: {
    marginTop: 16,
    paddingTop: 12,
    borderTopWidth: 1,
    borderTopColor: 'rgba(255, 255, 255, 0.1)',
  },
  legendGrid: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 16,
  },
  legendItem: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
    minWidth: '45%',
  },
  legendCheckbox: {
    width: 18,
    height: 18,
    borderWidth: 2,
    borderRadius: 3,
    alignItems: 'center',
    justifyContent: 'center',
  },
  legendCheckmark: {
    width: 10,
    height: 10,
    borderRadius: 2,
  },
  legendDot: {
    width: 10,
    height: 10,
    borderRadius: 5,
  },
  legendLabel: {
    color: '#b0b0b0',
    fontSize: 12,
  },
});

export const sunInfoStyles = StyleSheet.create({
  container: {
    flexDirection: 'row',
    justifyContent: 'center',
    alignItems: 'center',
    gap: 24,
    paddingVertical: 12,
    marginTop: 8,
  },
  sunItem: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
  },
  sunText: {
    color: '#8c8c8c',
    fontSize: 12,
  },
  sunTime: {
    color: '#b0b0b0',
    fontWeight: '500',
  },
  sunDivider: {
    color: '#666',
  },
});

export const lineChartStyles = StyleSheet.create({
  svgContainer: {
    backgroundColor: 'rgba(255, 255, 255, 0.02)',
    borderRadius: 8,
  },
});

export const legendDynamicStyles = {
  legendDotInactive: { backgroundColor: '#444' },
  legendLabelInactive: { color: '#666' },
  legendLabelActive: { color: '#b0b0b0' },
};

export const dashboardChartStyles = StyleSheet.create({
  loadingText: {
    color: '#666',
    marginTop: 10,
  },
});