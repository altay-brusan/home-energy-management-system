import React from 'react';
import { Text, View, TouchableOpacity } from 'react-native';
import Animated, { FadeInUp, useSharedValue, useAnimatedStyle, withSpring } from 'react-native-reanimated';
import { getEnergyBalanceChartStyles } from './EnergyBalanceChart.styles';

type EnergyData = {
  label: string;
  value: number;
  color: string;
  percentage: number;
  icon: string;
  trend: 'up' | 'down' | 'stable';
};

type Props = {
  width: number;
  height: number;
  COLORS: Record<string, string>;
  selectedIndex: number | null;
  setSelectedIndex: (i: number | null) => void;
};

export default function EnergyBalanceChart({
  width,
  height,
  COLORS,
  selectedIndex,
  setSelectedIndex,
}: Props) {
  const styles = getEnergyBalanceChartStyles(COLORS);
  
  const productionData: EnergyData[] = [
    { 
      label: 'Used', 
      value: 7.06, 
      color: COLORS.info || '#3B82F6', 
      percentage: 95.79,
      icon: '⚡',
      trend: 'up'
    },
    { 
      label: 'To Grid', 
      value: 0.31, 
      color: COLORS.accent || '#10B981', 
      percentage: 4.21,
      icon: '🔋',
      trend: 'stable'
    },
  ];

  const consumptionData: EnergyData[] = [
    { 
      label: 'From Grid', 
      value: 0.04, 
      color: COLORS.warning || '#F59E0B', 
      percentage: 1.04,
      icon: '🔌',
      trend: 'down'
    },
    { 
      label: 'Self-Consumption', 
      value: 3.81, 
      color: COLORS.success || '#8B5CF6', 
      percentage: 98.96,
      icon: '🏠',
      trend: 'up'
    },
  ];

  const totalProduction = productionData.reduce((sum, item) => sum + item.value, 0);
  const totalConsumption = consumptionData.reduce((sum, item) => sum + item.value, 0);
  const netProduction = totalProduction - totalConsumption;

  const renderEnergyCard = (data: EnergyData[], title: string, total: number, chartIndex: number) => {
    return (
      <Animated.View 
        entering={FadeInUp.delay(chartIndex * 100).duration(600)}
        style={styles.energyCard}
      >
        <View style={styles.cardHeader}>
          <View style={styles.cardTitleRow}>
            <Text style={styles.cardIcon}>
              {title === 'Production' ? '⚡' : '🔋'}
            </Text>
            <Text style={styles.cardTitle}>{title}</Text>
          </View>
          <Text style={styles.cardTotal}>{total.toFixed(2)} kWh</Text>
        </View>
        
        <View style={styles.progressContainer}>
          {data.map((item, index) => {
            const isSelected = selectedIndex === index;
            const scale = useSharedValue(1);
            
            const animatedStyle = useAnimatedStyle(() => ({
              transform: [{ scale: scale.value }],
            }));

            const handlePress = () => {
              scale.value = withSpring(0.95, { damping: 15, stiffness: 150 }, () => {
                scale.value = withSpring(1, { damping: 15, stiffness: 150 });
              });
              setSelectedIndex(index);
            };

            return (
              <Animated.View key={index} style={animatedStyle}>
                <TouchableOpacity
                  style={[styles.progressItem, isSelected && styles.progressItemSelected]}
                  onPress={handlePress}
                  activeOpacity={0.8}
                >
                  <View style={styles.progressHeader}>
                    <View style={styles.progressLabelRow}>
                      <Text style={styles.progressIcon}>{item.icon}</Text>
                      <Text style={[styles.progressLabel, isSelected && styles.progressLabelSelected]}>
                        {item.label}
                      </Text>
                      <View style={[styles.trendIndicator, { backgroundColor: item.color }]}>
                        <Text style={styles.trendIcon}>
                          {item.trend === 'up' ? '↗' : item.trend === 'down' ? '↘' : '→'}
                        </Text>
                      </View>
                    </View>
                    <Text style={[styles.progressValue, isSelected && styles.progressValueSelected]}>
                      {item.value} kWh
                    </Text>
                  </View>
                  
                  <View style={styles.progressBar}>
                    <View 
                      style={[
                        styles.progressFill, 
                        { 
                          width: `${item.percentage}%`,
                          backgroundColor: item.color,
                          opacity: isSelected ? 1 : 0.8
                        }
                      ]} 
                    />
                  </View>
                  
                  <View style={styles.progressFooter}>
                    <Text style={[styles.percentageText, isSelected && styles.percentageTextSelected]}>
                      {item.percentage.toFixed(1)}%
                    </Text>
                    <View style={[styles.percentageBar, { backgroundColor: COLORS.border }]}>
                      <View 
                        style={[
                          styles.percentageFill, 
                          { 
                            width: `${item.percentage}%`,
                            backgroundColor: item.color
                          }
                        ]} 
                      />
                    </View>
                  </View>
                </TouchableOpacity>
              </Animated.View>
            );
          })}
        </View>
      </Animated.View>
    );
  };

  return (
    <Animated.View entering={FadeInUp.delay(100).duration(600)} style={styles.container}>
      <View style={styles.header}>
        <Text style={styles.title}>Energy Balance</Text>
        <View style={styles.netProductionContainer}>
          <Text style={styles.netProductionLabel}>Net Production</Text>
          <Text style={[styles.netProductionValue, { color: netProduction >= 0 ? COLORS.success : COLORS.warning }]}>
            {netProduction >= 0 ? '+' : ''}{netProduction.toFixed(2)} kWh
          </Text>
        </View>
      </View>
      
      <View style={styles.chartsRow}>
        {renderEnergyCard(productionData, 'Production', totalProduction, 0)}
        {renderEnergyCard(consumptionData, 'Consumption', totalConsumption, 1)}
      </View>
    </Animated.View>
  );
}
