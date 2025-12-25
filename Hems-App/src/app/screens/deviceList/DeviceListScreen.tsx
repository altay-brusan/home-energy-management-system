import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { ActivityIndicator, Image, Pressable, RefreshControl, SafeAreaView, ScrollView, Text, View } from 'react-native';
import FontAwesome from '@react-native-vector-icons/fontawesome';
import { useThemeStore } from '../../../store/themeStore';
import { getThemeColors } from '../../../theme/constants';
import { getDeviceListStyles } from './DeviceListScreen.styles';
import { useNavigation, useFocusEffect } from '@react-navigation/native';
import { useI18n } from '../../../hooks/useI18n';
import { useSitesStore } from '../../../store/sitesStore';
import { deviceComponentsService } from '../../../api/services/deviceComponentsService';
import {
  ComponentSummaryDto,
  isInverterSummary,
  isBatterySummary,
} from '../../../api/types/deviceComponents.types';

const POLLING_INTERVAL = 10000; // 10 seconds

export default function DeviceListScreen() {
  const isDark = useThemeStore(s => s.mode) === 'dark';
  const COLORS = getThemeColors(isDark);
  const styles = useMemo(() => getDeviceListStyles(COLORS), [COLORS]);
  const navigation = useNavigation<any>();
  const { t } = useI18n();

  const { selectedSiteDevice, loadSelectedSiteDevice, selectedSite } = useSitesStore();

  const [components, setComponents] = useState<ComponentSummaryDto[]>([]);
  const [rawResponse, setRawResponse] = useState<any>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const deviceId = selectedSiteDevice?.device?.id;

  const fetchDeviceSummary = useCallback(async (showLoading = false) => {
    if (!deviceId) {
      setComponents([]);
      setIsLoading(false);
      return;
    }

    if (showLoading) {
      setIsLoading(true);
    }
    setError(null);

    try {
      const response = await deviceComponentsService.getDeviceSummary(deviceId);
      setRawResponse(response);
      setComponents(response.components || []);
    } catch (err: any) {
      if (err?.response?.status === 404) {
        setError(t('deviceList.deviceNotFound'));
      } else if (err?.response?.status === 401) {
        // Token refresh will handle this
      } else {
        setError(t('deviceList.fetchError'));
      }
    } finally {
      setIsLoading(false);
      setIsRefreshing(false);
    }
  }, [deviceId, t]);

  // Load device on mount if not already loaded
  useEffect(() => {
    if (selectedSite?.hasDevice && !selectedSiteDevice) {
      loadSelectedSiteDevice();
    }
  }, [selectedSite, selectedSiteDevice, loadSelectedSiteDevice]);

  // Fetch data when screen is focused
  useFocusEffect(
    useCallback(() => {
      fetchDeviceSummary(true);

      // Set up polling
      const pollTimer = setInterval(() => {
        fetchDeviceSummary(false);
      }, POLLING_INTERVAL);

      return () => {
        clearInterval(pollTimer);
      };
    }, [fetchDeviceSummary])
  );

  const handleRefresh = useCallback(() => {
    setIsRefreshing(true);
    fetchDeviceSummary(false);
  }, [fetchDeviceSummary]);

  const getComponentStatusText = (component: ComponentSummaryDto): string => {
    if (isInverterSummary(component)) {
      return component.activePowerKw !== null ? t('deviceList.online') : t('deviceList.offline');
    }
    if (isBatterySummary(component)) {
      return component.stateOfCharge !== null ? t('deviceList.online') : t('deviceList.offline');
    }
    return t('deviceList.offline');
  };

  const isComponentOnline = (component: ComponentSummaryDto): boolean => {
    if (isInverterSummary(component)) {
      return component.activePowerKw !== null;
    }
    if (isBatterySummary(component)) {
      return component.stateOfCharge !== null;
    }
    return false;
  };

  const getComponentValueText = (component: ComponentSummaryDto): string => {
    if (isInverterSummary(component)) {
      return component.activePowerKw !== null
        ? `${component.activePowerKw.toFixed(2)} kW`
        : '-- kW';
    }
    if (isBatterySummary(component)) {
      return component.stateOfCharge !== null
        ? `${component.stateOfCharge.toFixed(0)}%`
        : '--%';
    }
    return '--';
  };

  const getComponentImage = (component: ComponentSummaryDto) => {
    if (isInverterSummary(component)) {
      return require('../../../assets/images/device-image.png');
    }
    if (isBatterySummary(component)) {
      return require('../../../assets/images/device-image.png');
    }
    return require('../../../assets/images/device-image.png');
  };

  const handleComponentPress = (component: ComponentSummaryDto) => {
    navigation.navigate('DeviceDetail', {
      device: {
        id: component.id,
        name: component.name,
        serial: component.serialNumber,
        status: isComponentOnline(component) ? 'online' : 'offline',
        mode: 'On Grid',
        powerKw: isInverterSummary(component) ? (component.activePowerKw ?? 0) : 0,
        type: component.type,
        model: component.model,
        manufacturer: component.manufacturer,
        stateOfCharge: isBatterySummary(component) ? component.stateOfCharge : null,
      },
    });
  };

  const renderComponent = (component: ComponentSummaryDto) => {
    const isOnline = isComponentOnline(component);

    return (
      <Pressable
        key={component.id}
        style={styles.card}
        onPress={() => handleComponentPress(component)}
      >
        <View style={styles.thumbWrap}>
          <Image
            source={getComponentImage(component)}
            style={styles.image}
            resizeMode="contain"
          />
        </View>
        <View style={styles.body}>
          <View style={{ flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between' }}>
            <Text style={styles.name} numberOfLines={1}>{component.name}</Text>
            <FontAwesome name="chevron-right" size={14} color={COLORS.slate400} style={styles.rightIcon} />
          </View>
          <Text style={styles.serial} numberOfLines={1}>
            {t('common.serialLabel')}: {component.serialNumber}
          </Text>
          <View style={styles.row}>
            <View style={{ flexDirection: 'row', alignItems: 'center' }}>
              <View style={[styles.dot, { backgroundColor: isOnline ? COLORS.success : COLORS.error }]} />
              <Text style={styles.serial}>
                {getComponentStatusText(component)} • {component.type === 'inverter' ? t('deviceList.inverter') : t('deviceList.battery')}
              </Text>
            </View>
            <View style={styles.chip}>
              <Text style={styles.chipText}>{getComponentValueText(component)}</Text>
            </View>
          </View>
        </View>
      </Pressable>
    );
  };

  if (isLoading && components.length === 0) {
    return (
      <SafeAreaView style={styles.screen}>
        <ScrollView contentContainerStyle={styles.container}>
          <Text style={styles.title}>{t('deviceList.title')}</Text>
          {/* Debug: Raw API Response */}
          <View style={{
            padding: 12,
            marginBottom: 16,
            backgroundColor: COLORS.surface,
            borderRadius: 8,
            borderWidth: 2,
            borderColor: '#ff6600',
          }}>
            <Text style={{ color: '#ff6600', fontWeight: 'bold', marginBottom: 8, fontSize: 14 }}>
              DEBUG - Device Summary Response
            </Text>
            <Text style={{ color: COLORS.text, fontSize: 12, marginBottom: 4 }}>
              DeviceId: {deviceId || 'null'}
            </Text>
            <Text style={{ color: COLORS.text, fontSize: 12, marginBottom: 4 }}>
              Loading: true
            </Text>
            <Text style={{ color: COLORS.text, fontSize: 12, marginBottom: 8 }}>
              Response: waiting...
            </Text>
          </View>
          <View style={{ flex: 1, justifyContent: 'center', alignItems: 'center', paddingVertical: 40 }}>
            <ActivityIndicator size="large" color={COLORS.primary} />
          </View>
        </ScrollView>
      </SafeAreaView>
    );
  }

  return (
    <SafeAreaView style={styles.screen}>
      <ScrollView
        contentContainerStyle={styles.container}
        refreshControl={
          <RefreshControl
            refreshing={isRefreshing}
            onRefresh={handleRefresh}
            tintColor={COLORS.primary}
            colors={[COLORS.primary]}
          />
        }
      >
        <Text style={styles.title}>{t('deviceList.title')}</Text>

        {/* Debug: Raw API Response */}
        <View style={{
          padding: 12,
          marginBottom: 16,
          backgroundColor: COLORS.surface,
          borderRadius: 8,
          borderWidth: 2,
          borderColor: '#ff6600',
        }}>
          <Text style={{ color: '#ff6600', fontWeight: 'bold', marginBottom: 8, fontSize: 14 }}>
            DEBUG - Device Summary Response
          </Text>
          <Text style={{ color: COLORS.text, fontSize: 12, marginBottom: 4 }}>
            DeviceId: {deviceId || 'null'}
          </Text>
          <Text style={{ color: COLORS.text, fontSize: 12, marginBottom: 4 }}>
            Loading: {isLoading ? 'true' : 'false'}
          </Text>
          <Text style={{ color: COLORS.text, fontSize: 12, marginBottom: 8 }}>
            Response: {rawResponse ? 'received' : 'null'}
          </Text>
          {rawResponse && (
            <View style={{ maxHeight: 250, backgroundColor: '#1a1a2e', padding: 8, borderRadius: 4 }}>
              <Text style={{ color: '#00ff00', fontSize: 10, fontFamily: 'Courier' }}>
                {JSON.stringify(rawResponse, null, 2)}
              </Text>
            </View>
          )}
        </View>

        {error && (
          <View style={{ padding: 16, marginBottom: 12, backgroundColor: COLORS.errorBg, borderRadius: 8 }}>
            <Text style={{ color: COLORS.error }}>{error}</Text>
          </View>
        )}

        {!deviceId && !isLoading && (
          <View style={{ padding: 16, marginBottom: 12, backgroundColor: COLORS.surface, borderRadius: 8 }}>
            <Text style={{ color: COLORS.textSoft }}>{t('deviceList.noDeviceSelected')}</Text>
          </View>
        )}

        <View style={styles.grid}>
          {components.map(renderComponent)}
        </View>

        {components.length === 0 && deviceId && !isLoading && !error && (
          <View style={{ padding: 16, marginTop: 12, backgroundColor: COLORS.surface, borderRadius: 8 }}>
            <Text style={{ color: COLORS.textSoft }}>{t('deviceList.noComponents')}</Text>
          </View>
        )}
      </ScrollView>
    </SafeAreaView>
  );
}
