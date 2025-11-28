import React, { useState, useRef } from 'react';
import { View, ScrollView, Dimensions, NativeSyntheticEvent, NativeScrollEvent } from 'react-native';
import { PowerFlowDiagram } from '../powerFlowDiagram';
import { DashboardSummaryCard } from '../dashboardSummaryCard';
import { useThemeStore } from '../../../../../store/themeStore';
import { getThemeColors, SPACING } from '../../../../../theme/constants';
import { createStyles } from './DashboardSlider.styles';

const { width: SCREEN_WIDTH } = Dimensions.get('window');
const SLIDE_WIDTH = SCREEN_WIDTH - (SPACING.md * 2); // Subtract padding from both sides

interface DashboardSliderProps {
  deviceId?: string;
}

export const DashboardSlider: React.FC<DashboardSliderProps> = ({ deviceId }) => {
  const mode = useThemeStore(s => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);
  const styles = createStyles(COLORS);

  const [activeIndex, setActiveIndex] = useState(0);
  const scrollViewRef = useRef<ScrollView>(null);

  const handleScroll = (event: NativeSyntheticEvent<NativeScrollEvent>) => {
    const slideIndex = Math.round(event.nativeEvent.contentOffset.x / SLIDE_WIDTH);
    setActiveIndex(slideIndex);
  };

  return (
    <View style={styles.container}>
      <ScrollView
        ref={scrollViewRef}
        horizontal
        pagingEnabled
        showsHorizontalScrollIndicator={false}
        onScroll={handleScroll}
        scrollEventThrottle={16}
        decelerationRate="fast"
        snapToInterval={SLIDE_WIDTH}
        snapToAlignment="start"
        contentContainerStyle={styles.scrollContent}
      >
        {/* Slide 1: Energy Flow Radial */}
        <View style={[styles.slide, { width: SLIDE_WIDTH }]}>
          <PowerFlowDiagram />
        </View>

        {/* Slide 2: Dashboard Summary Card */}
        <View style={[styles.slide, { width: SLIDE_WIDTH }]}>
          <DashboardSummaryCard deviceId={deviceId} />
        </View>
      </ScrollView>

      {/* Pagination Dots */}
      <View style={styles.pagination}>
        {[0, 1].map((index) => (
          <View
            key={index}
            style={[
              styles.dot,
              {
                backgroundColor: activeIndex === index ? COLORS.accent : COLORS.border,
                opacity: activeIndex === index ? 1 : 0.3,
              },
            ]}
          />
        ))}
      </View>
    </View>
  );
};

export default DashboardSlider;
