/**
 * TimelineBar Component
 * 24 saatlik görsel timeline bar
 */

import React from 'react';
import { View, Text, StyleSheet, Dimensions } from 'react-native';
import { useThemeStore } from '../../store/themeStore';
import { getThemeColors } from '../../theme/constants';
import { TimeOfUsePeriod } from '../../api/types/inverterModes.types';

const { width: SCREEN_WIDTH } = Dimensions.get('window');
const BAR_WIDTH = SCREEN_WIDTH - 48; // Padding on both sides

interface TimelineBarProps {
  periods: TimeOfUsePeriod[];
}

// Parse time string "HH:MM" to minutes from midnight
const timeToMinutes = (time: string): number => {
  const [hours, minutes] = time.split(':').map(Number);
  return hours * 60 + minutes;
};

// Get color based on period settings
const getPeriodColor = (period: TimeOfUsePeriod): string => {
  if (period.gridCharge === 'on') return '#4CAF50'; // Green for grid charging
  if (period.genCharge === 'on') return '#FF9800'; // Orange for generator charging
  return '#2196F3'; // Blue for discharge/normal
};

const TimelineBar: React.FC<TimelineBarProps> = ({ periods }) => {
  const mode = useThemeStore((s) => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);

  const TOTAL_MINUTES = 24 * 60;

  // Sort periods by start time
  const sortedPeriods = [...periods].sort(
    (a, b) => timeToMinutes(a.startTime) - timeToMinutes(b.startTime)
  );

  return (
    <View style={styles.container}>
      {/* Timeline Bar */}
      <View
        style={[
          styles.bar,
          { backgroundColor: isDark ? COLORS.border : COLORS.gray200 },
        ]}
      >
        {sortedPeriods.map((period, index) => {
          const startMinutes = timeToMinutes(period.startTime);
          let endMinutes = timeToMinutes(period.endTime);

          // Handle "24:00" as end of day
          if (period.endTime === '24:00' || period.endTime === '00:00') {
            endMinutes = TOTAL_MINUTES;
          }

          // Handle overnight periods
          if (endMinutes <= startMinutes) {
            endMinutes = TOTAL_MINUTES;
          }

          const left = (startMinutes / TOTAL_MINUTES) * BAR_WIDTH;
          const width = ((endMinutes - startMinutes) / TOTAL_MINUTES) * BAR_WIDTH;

          return (
            <View
              key={period.id}
              style={[
                styles.periodBlock,
                {
                  left,
                  width: Math.max(width, 2), // Minimum width for visibility
                  backgroundColor: getPeriodColor(period),
                },
              ]}
            />
          );
        })}
      </View>

      {/* Time Labels */}
      <View style={styles.labelsContainer}>
        {[0, 6, 12, 18, 24].map((hour) => (
          <Text
            key={hour}
            style={[styles.label, { color: COLORS.textSoft }]}
          >
            {hour.toString().padStart(2, '0')}:00
          </Text>
        ))}
      </View>

      {/* Legend */}
      <View style={styles.legend}>
        <View style={styles.legendItem}>
          <View style={[styles.legendDot, { backgroundColor: '#4CAF50' }]} />
          <Text style={[styles.legendText, { color: COLORS.textSoft }]}>
            Şebeke Şarj
          </Text>
        </View>
        <View style={styles.legendItem}>
          <View style={[styles.legendDot, { backgroundColor: '#FF9800' }]} />
          <Text style={[styles.legendText, { color: COLORS.textSoft }]}>
            Jeneratör Şarj
          </Text>
        </View>
        <View style={styles.legendItem}>
          <View style={[styles.legendDot, { backgroundColor: '#2196F3' }]} />
          <Text style={[styles.legendText, { color: COLORS.textSoft }]}>
            Deşarj
          </Text>
        </View>
      </View>
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    marginVertical: 16,
  },
  bar: {
    height: 24,
    borderRadius: 12,
    overflow: 'hidden',
    position: 'relative',
  },
  periodBlock: {
    position: 'absolute',
    top: 0,
    bottom: 0,
    borderRadius: 4,
  },
  labelsContainer: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    marginTop: 4,
  },
  label: {
    fontSize: 10,
  },
  legend: {
    flexDirection: 'row',
    justifyContent: 'center',
    marginTop: 12,
    gap: 16,
  },
  legendItem: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  legendDot: {
    width: 10,
    height: 10,
    borderRadius: 5,
    marginRight: 4,
  },
  legendText: {
    fontSize: 11,
  },
});

export default TimelineBar;
