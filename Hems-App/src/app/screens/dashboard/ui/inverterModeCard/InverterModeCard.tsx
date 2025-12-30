/**
 * InverterModeCard
 * Dashboard'da gösterilen inverter mod kartı
 */

import React, { useEffect } from 'react';
import { View, Text, TouchableOpacity, StyleSheet } from 'react-native';
import { useNavigation } from '@react-navigation/native';
import { useTranslation } from 'react-i18next';
import MaterialIcons from '@react-native-vector-icons/material-icons';
import Reanimated, { FadeInDown } from 'react-native-reanimated';
import { useThemeStore } from '../../../../../store/themeStore';
import { getThemeColors } from '../../../../../theme/constants';
import { useInverterModeStore } from '../../../../../store/inverterModeStore';
import { useSitesStore } from '../../../../../store/sitesStore';

interface InverterModeCardProps {
  COLORS: Record<string, string>;
}

const InverterModeCard: React.FC<InverterModeCardProps> = ({ COLORS }) => {
  const navigation = useNavigation<any>();
  const { t } = useTranslation();
  const themeMode = useThemeStore((s) => s.mode);
  const isDark = themeMode === 'dark';

  const { activeModeId, activeModeNumber, modes, fetchModes, fetchActiveMode } = useInverterModeStore();
  const { selectedSiteDevice } = useSitesStore();
  const deviceId = selectedSiteDevice?.device?.id;

  // Fetch modes on mount if not already loaded
  useEffect(() => {
    if (modes.length === 0) {
      fetchModes();
    }
  }, [modes.length, fetchModes]);

  // Fetch active mode from API on mount
  useEffect(() => {
    if (deviceId) {
      fetchActiveMode(deviceId);
    }
  }, [deviceId, fetchActiveMode]);

  const handlePress = () => {
    navigation.navigate('InverterModes');
  };

  // Get active mode from API modes (store)
  const mode = activeModeId ? modes.find((m) => m.id === activeModeId) : null;
  const isCustom = mode?.isCustom || false;
  const modeName = mode?.name || t('inverterModes.noModeSelected');
  const modeDescription = mode?.description || t('inverterModes.configureMode');
  const modeNumber = activeModeNumber ? String(activeModeNumber) : null;

  return (
    <Reanimated.View entering={FadeInDown.delay(150).duration(600)}>
      <TouchableOpacity
        onPress={handlePress}
        activeOpacity={0.8}
        style={[
          styles.container,
          { backgroundColor: isDark ? COLORS.surface : COLORS.white },
        ]}
      >
        {/* Header Row */}
        <View style={styles.headerRow}>
          <View style={styles.titleContainer}>
            <MaterialIcons
              name="tune"
              size={20}
              color={COLORS.accent}
              style={styles.icon}
            />
            <Text style={[styles.title, { color: COLORS.text }]}>
              {t('inverterModes.title')}
            </Text>
          </View>
          <MaterialIcons name="chevron-right" size={22} color={COLORS.textSoft} />
        </View>

        {/* Mode Info Row */}
        <View style={styles.modeRow}>
          {/* Mode Badge */}
          {modeNumber && (
            <View
              style={[
                styles.badge,
                {
                  backgroundColor: isCustom ? '#9C27B0' : COLORS.accent,
                },
              ]}
            >
              <Text style={styles.badgeText}>{modeNumber}</Text>
            </View>
          )}

          {/* Mode Name */}
          <Text
            style={[
              styles.modeName,
              { color: activeModeId ? COLORS.text : COLORS.textSoft },
            ]}
            numberOfLines={1}
          >
            {modeName}
          </Text>

          {/* Active Indicator */}
          {activeModeId && (
            <MaterialIcons name="check-circle" size={18} color="#28A745" />
          )}
        </View>

        {/* Description */}
        <Text
          style={[styles.description, { color: COLORS.textSoft }]}
          numberOfLines={2}
        >
          {modeDescription}
        </Text>
      </TouchableOpacity>
    </Reanimated.View>
  );
};

const styles = StyleSheet.create({
  container: {
    borderRadius: 12,
    padding: 16,
    marginBottom: 12,
  },
  headerRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 12,
  },
  titleContainer: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  icon: {
    marginRight: 8,
  },
  title: {
    fontSize: 14,
    fontWeight: '600',
  },
  modeRow: {
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: 8,
    gap: 8,
  },
  badge: {
    paddingHorizontal: 10,
    paddingVertical: 4,
    borderRadius: 12,
  },
  badgeText: {
    color: '#fff',
    fontSize: 11,
    fontWeight: '700',
  },
  modeName: {
    fontSize: 17,
    fontWeight: '700',
    flex: 1,
  },
  description: {
    fontSize: 13,
    lineHeight: 18,
    marginBottom: 8,
  },
});

export default InverterModeCard;
