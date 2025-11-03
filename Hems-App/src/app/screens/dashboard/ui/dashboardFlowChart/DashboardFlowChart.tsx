import React, { useState, useEffect, useCallback } from 'react';
import { View, Text, Dimensions, TouchableOpacity, Modal, ActivityIndicator } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import Animated, { FadeInUp, FadeInDown, FadeOut } from 'react-native-reanimated';
import { Calendar } from 'react-native-calendars';
import { SPACING, TYPOGRAPHY } from '../../../../../theme/constants';
import FlowSankey, { SankeyNode, SankeyLink } from '../../../../../components/FlowSankey';
import { useI18n } from '../../../../../hooks/useI18n';
import { useSelectedSiteDevice } from '../../../../../hooks/useSelectedSiteDevice';
import { useDashboardRefresh } from '../../../../../hooks/useDashboardRefresh';
import { mobileDashboardService } from '../../../../../api/services/mobileDashboardService';
import { getDashboardFlowChartStyles } from './DashboardFlowChart.styles';

type Props = {
  COLORS: Record<string, string>;
  isDark: boolean;
};


const { width: screenWidth } = Dimensions.get('window');
const chartWidth = screenWidth - (SPACING.lg * 2) - (SPACING.md * 2);

export default function DashboardFlowChart ({ COLORS, isDark }: Props) {
  const styles = getDashboardFlowChartStyles(COLORS);
  const { t } = useI18n();
  const { device, hasDevice } = useSelectedSiteDevice();
  const { registerRefreshCallback } = useDashboardRefresh();
  const insets = useSafeAreaInsets();

  const [startDate, setStartDate] = useState<string>(() => {
    return new Date().toISOString().split('T')[0]; // Bugün
  });
  const [endDate, setEndDate] = useState<string>(() => {
    return new Date().toISOString().split('T')[0]; // Bugün
  });
  const [showDatePicker, setShowDatePicker] = useState(false);
  const [tempStartDate, setTempStartDate] = useState<string>('');
  const [tempEndDate, setTempEndDate] = useState<string>('');

  const [energyFlowData, setEnergyFlowData] = useState<any>(null);
  const [loading, setLoading] = useState(true);

  const loadEnergyFlowData = useCallback(async () => {
    if (!hasDevice || !device?.id) {
      setLoading(false);
      return;
    }

    setLoading(true);
    try {
      const data = await mobileDashboardService.getEnergyFlow(device.id, startDate, endDate);
      setEnergyFlowData(data);
    } catch (error) {setEnergyFlowData(null);
    } finally {
      setLoading(false);
    }
  }, [hasDevice, device?.id, startDate, endDate]);

  useEffect(() => {
    loadEnergyFlowData();
  }, [loadEnergyFlowData]);

  useEffect(() => {const unregister = registerRefreshCallback(loadEnergyFlowData);return () => {unregister();
    };
  }, [registerRefreshCallback, loadEnergyFlowData]);

  const convertToSankeyData = (apiData: any) => {
    if (!apiData) return { nodes: [], links: [], percentByNodeId: {} };const nodes: SankeyNode[] = [];
    const links: SankeyLink[] = [];
    const percentByNodeId: Record<string, number> = {};
    
    const nodeMap = new Map<string, number>();
    let nodeIndex = 0;

    apiData.sources.forEach((source: any) => {
      if (!nodeMap.has(source.id)) {
        nodeMap.set(source.id, nodeIndex++);
        nodes.push({ 
          id: source.id,
          label: source.label,
          side: "left",
          color: source.color
        });
        percentByNodeId[source.id] = source.percentage;
      }
    });

    if (!nodeMap.has("CENTER")) {
      nodeMap.set("CENTER", nodeIndex++);
      nodes.push({ 
        id: "CENTER",
        label: "SYSTEM",
        side: "center",
        color: "#96CEB4"
      });
    }

    apiData.destinations.forEach((destination: any) => {
      if (!nodeMap.has(destination.id)) {
        nodeMap.set(destination.id, nodeIndex++);
        nodes.push({ 
          id: destination.id,
          label: destination.label,
          side: "right",
          color: destination.color
        });
        percentByNodeId[destination.id] = destination.percentage;
      }
    });

    apiData.flows.forEach((flow: any) => {
      const sourceIndex = nodeMap.get(flow.source);
      const targetIndex = nodeMap.get(flow.target);
      
      if (sourceIndex !== undefined && targetIndex !== undefined) {
        links.push({
          source: flow.source,
          target: flow.target,
          value: flow.value
        });
      }
    });return { nodes, links, percentByNodeId };
  };

  const { nodes, links, percentByNodeId } = energyFlowData ? convertToSankeyData(energyFlowData) : { nodes: [], links: [], percentByNodeId: {} };

  const openDatePicker = () => {
    setTempStartDate(startDate);
    setTempEndDate(endDate);
    setShowDatePicker(true);
  };

  const handleDateSelect = (day: any) => {
    const selectedDate = day.dateString;
    
    if (!tempStartDate || (tempStartDate && tempEndDate)) {
      setTempStartDate(selectedDate);
      setTempEndDate('');
    } else if (tempStartDate && !tempEndDate) {
      if (selectedDate >= tempStartDate) {
        setTempEndDate(selectedDate);
      } else {
        setTempEndDate(tempStartDate);
        setTempStartDate(selectedDate);
      }
    }
  };

  const confirmDateSelection = () => {
    if (tempStartDate && tempEndDate) {
      setStartDate(tempStartDate);
      setEndDate(tempEndDate);
      setShowDatePicker(false);
    }
  };

  // Cancel date selection
  const cancelDateSelection = () => {
    setTempStartDate('');
    setTempEndDate('');
    setShowDatePicker(false);
  };

  // Generate marked dates for calendar
  const getMarkedDates = () => {
    const markedDates: any = {};
    
    if (tempStartDate) {
      markedDates[tempStartDate] = {
        startingDay: true,
        color: COLORS.accent,
        textColor: COLORS.white,
      };
    }
    
    if (tempEndDate && tempStartDate) {
      // Mark all dates between start and end
      const start = new Date(tempStartDate);
      const end = new Date(tempEndDate);
      
      for (let date = new Date(start); date <= end; date.setDate(date.getDate() + 1)) {
        const dateString = date.toISOString().split('T')[0];
        if (dateString === tempStartDate) {
          markedDates[dateString] = {
            startingDay: true,
            color: COLORS.accent,
            textColor: COLORS.white,
          };
        } else if (dateString === tempEndDate) {
          markedDates[dateString] = {
            endingDay: true,
            color: COLORS.accent,
            textColor: COLORS.white,
          };
        } else {
          markedDates[dateString] = {
            color: COLORS.accent + '40',
            textColor: COLORS.accent,
          };
        }
      }
    }
    
    return markedDates;
  };

  return (
    <Animated.View
      entering={FadeInUp.delay(300).duration(600)}
      style={styles.card}
    >
      <Text style={styles.title}>{t('dashboardFlow.title')}</Text>

      {/* Date Range Filter */}
      <View style={styles.filterContainer}>
        <View style={styles.dateRangeContainer}>
          <Text style={styles.dateLabel}>
            {t('dashboardFlow.dateRange')}
          </Text>
          <TouchableOpacity
            style={styles.dateRangeButton}
            onPress={openDatePicker}
            activeOpacity={0.7}
          >
            <Text style={styles.dateRangeButtonText}>
              {startDate} - {endDate}
            </Text>
            <Text style={styles.dateRangeButtonIcon}>▼</Text>
          </TouchableOpacity>
        </View>
      </View>

      {/* Don't render if no device */}
      {!hasDevice || !device?.id ? (
        <View style={styles.noDeviceContainer}>
          <Text style={styles.noDeviceText}>
            {t('dashboardFlow.noDevice')}
          </Text>
        </View>
      ) : loading ? (
        <View style={styles.loadingContainer}>
          <ActivityIndicator color={COLORS.accent} size="large" />
          <Text style={styles.loadingText}>
            {t('dashboardFlow.loading')}
          </Text>
        </View>
      ) : !energyFlowData || nodes.length === 0 || links.length === 0 ? (
        <View style={styles.noDataContainer}>
          <Text style={styles.noDataText}>
            {t('dashboardFlow.noData')}
          </Text>
        </View>
      ) : (
        <View style={styles.chartContainer}>
          <FlowSankey
            width={chartWidth}
            height={380}
            nodes={nodes}
            links={links}
            percentByNodeId={percentByNodeId}
            theme={isDark ? 'dark' : 'light'}
            layoutOpts={{
              nodeWidth: 70,
              minHeight: 50,
              gapY: 10,
              padding: 8,
              curviness: 0.18,
              fontScale: 0.85,
            }}
          />
        </View>
      )}

      {/* Date Range Picker Modal */}
      <Modal
        visible={showDatePicker}
        transparent
        animationType="none"
        onRequestClose={cancelDateSelection}
      >
        <TouchableOpacity
          style={styles.modalOverlay}
          activeOpacity={1}
          onPress={cancelDateSelection}
        >
          <Animated.View
            entering={FadeInDown.duration(300)}
            exiting={FadeOut.duration(200)}
            style={[styles.bottomSheet, { paddingBottom: Math.max(insets.bottom, 16) + 16 }]}
          >
            <View style={styles.bottomSheetHandle} />
            <Text style={styles.bottomSheetTitle}>{t('dashboardFlow.selectDateRange')}</Text>
            
            {/* Selection Status */}
            <View style={styles.selectionStatus}>
              <Text style={styles.selectionStatusText}>
                {!tempStartDate ? t('dashboardFlow.selectStartDate') : 
                 !tempEndDate ? t('dashboardFlow.selectEndDate') : 
                 `${tempStartDate} - ${tempEndDate}`}
              </Text>
            </View>

            <Calendar
              current={tempStartDate || startDate}
              onDayPress={handleDateSelect}
              markedDates={getMarkedDates()}
              theme={{
                backgroundColor: COLORS.surface,
                calendarBackground: COLORS.surface,
                textSectionTitleColor: COLORS.textSoft,
                selectedDayBackgroundColor: COLORS.accent,
                selectedDayTextColor: COLORS.white,
                todayTextColor: COLORS.accent,
                dayTextColor: isDark ? COLORS.white : COLORS.black, // Dark mode'da beyaz, light mode'da siyah
                textDisabledColor: COLORS.textSoft + '40',
                monthTextColor: isDark ? COLORS.white : COLORS.black, // Dark mode'da beyaz, light mode'da siyah
                textMonthFontWeight: '700',
                textDayFontSize: 16,
                textMonthFontSize: 18,
                arrowColor: COLORS.accent,
                textDayHeaderFontWeight: '600',
                textDayHeaderFontSize: 14,
              }}
            />

            {/* Action Buttons */}
            <View style={styles.actionButtons}>
              <TouchableOpacity
                style={[styles.actionButton, styles.cancelButton]}
                onPress={cancelDateSelection}
              >
                <Text style={styles.cancelButtonText}>{t('common.cancel')}</Text>
              </TouchableOpacity>
              
              <TouchableOpacity
                style={[
                  styles.actionButton, 
                  styles.confirmButton,
                  (!tempStartDate || !tempEndDate) && styles.disabledButton
                ]}
                onPress={confirmDateSelection}
                disabled={!tempStartDate || !tempEndDate}
              >
                <Text style={styles.confirmButtonText}>{t('common.confirm')}</Text>
              </TouchableOpacity>
            </View>
            {/* Bottom safe area spacer */}
            <View style={{ height: Math.max(insets.bottom, 12) }} />
          </Animated.View>
        </TouchableOpacity>
      </Modal>


      <View style={styles.legend}>
        <View style={styles.legendItem}>
          <View style={[styles.legendColor, { backgroundColor: '#FDB52E' }]} />
          <Text style={styles.legendText}>{t('dashboardFlow.pv')}</Text>
        </View>
        <View style={styles.legendItem}>
          <View style={[styles.legendColor, { backgroundColor: '#12C6A9' }]} />
          <Text style={styles.legendText}>{t('dashboardFlow.battery')}</Text>
        </View>
        <View style={styles.legendItem}>
          <View style={[styles.legendColor, { backgroundColor: '#FF9500' }]} />
          <Text style={styles.legendText}>{t('dashboardFlow.generator')}</Text>
        </View>
        <View style={styles.legendItem}>
          <View style={[styles.legendColor, { backgroundColor: '#9D76F2' }]} />
          <Text style={styles.legendText}>{t('dashboardFlow.grid')}</Text>
        </View>
        <View style={styles.legendItem}>
          <View style={[styles.legendColor, { backgroundColor: '#FF6B6B' }]} />
          <Text style={styles.legendText}>{t('dashboardFlow.load')}</Text>
        </View>
        <View style={styles.legendItem}>
          <View style={[styles.legendColor, { backgroundColor: '#4A90E2' }]} />
          <Text style={styles.legendText}>{t('dashboardFlow.system')}</Text>
        </View>
      </View>
    </Animated.View>
  );
}