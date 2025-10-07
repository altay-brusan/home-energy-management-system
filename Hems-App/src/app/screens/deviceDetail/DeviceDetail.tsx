import React, { useMemo, useRef, useState } from 'react';
import {
  Animated,
  Dimensions,
  Image,
  Alert,
  Modal,
  Pressable,
  ScrollView,
  Text,
  View,
  StatusBar,
  RefreshControl,
} from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { getThemeColors, SPACING } from '../../../theme/constants';
import { useThemeStore } from '../../../store/themeStore';
import Button from '../../../components/ui/Button';
import { useRoute, useNavigation } from '@react-navigation/native';
import FontAwesome from '@react-native-vector-icons/fontawesome';
import { getStyles } from './DeviceDetail.styles';
import DeviceBadge from './ui/DeviceBadge';
import GridCard from './ui/GridCard';
import BatteryDetailBottomSheet from './ui/BatteryDetailBottomSheet';

type SheetData = {
  id: string;
  title: string;
  value: string;
  subtitle?: string;
  description?: string;
  icon?: string;
  status?: 'normal' | 'warning' | 'error' | 'inactive';
} | null;

export default function DeviceDetail () {
  const navigation = useNavigation<any>();
  const isDark = useThemeStore(s => s.mode) === 'dark';
  const COLORS = getThemeColors(isDark);
  const styles = useMemo(() => getStyles(COLORS), [COLORS]);
  const insets = useSafeAreaInsets();
  const route = useRoute<any>();
  const device = route?.params?.device;

  const headerOpacity = useRef(new Animated.Value(0)).current;
  const headerY = useRef(new Animated.Value(-50)).current;

  const [batterySheetVisible, setBatterySheetVisible] = useState(false);

  const batteryData = {
    soc: 89.0,
    power: 0.763,
    voltage: 30.2,
    preheating: false,
    discharge: 3.0,
    avgCellVoltage: 3.357,
    avgTemp: 24,
    sn: '110*****0012',
    version: 'V100R001C00',
    model: 'Netzr BAT 8.0',
    capacity: 8.06,
  };

  const SHEET_H = Math.round(Dimensions.get('window').height * 0.6);
  const sheetY = useRef(new Animated.Value(SHEET_H)).current;
  const sheetOpacity = useRef(new Animated.Value(0)).current;
  const canCloseRef = useRef(false);
  const [sheet, setSheet] = useState<SheetData>(null);
  const [refreshing, setRefreshing] = useState(false);

  const onRefresh = () => {
    setRefreshing(true);
    setTimeout(() => setRefreshing(false), 800);
  };

  const openSheet = (data: NonNullable<SheetData>) => {
    if (['bat1', 'bat2', 'bat3', 'batCluster'].includes(data.id)) {
      setBatterySheetVisible(true);
      return;
    }

    sheetY.setValue(SHEET_H);
    sheetOpacity.setValue(0);
    canCloseRef.current = false;
    setSheet(data);
    requestAnimationFrame(() => {
      Animated.parallel([
        Animated.spring(sheetY, {
          toValue: 0,
          useNativeDriver: true,
          bounciness: 7,
          speed: 18,
        }),
        Animated.timing(sheetOpacity, {
          toValue: 1,
          duration: 180,
          useNativeDriver: true,
        }),
      ]).start(() => {
        setTimeout(() => {
          canCloseRef.current = true;
        }, 120);
      });
    });
  };

  const closeSheet = () => {
    canCloseRef.current = false;
    Animated.parallel([
      Animated.timing(sheetY, {
        toValue: SHEET_H,
        duration: 180,
        useNativeDriver: true,
      }),
      Animated.timing(sheetOpacity, {
        toValue: 0,
        duration: 140,
        useNativeDriver: true,
      }),
    ]).start(() => setSheet(null));
  };

  React.useEffect(() => {
    Animated.parallel([
      Animated.timing(headerOpacity, {
        toValue: 1,
        duration: 800,
        useNativeDriver: true,
      }),
      Animated.spring(headerY, {
        toValue: 0,
        useNativeDriver: true,
        tension: 50,
        friction: 8,
      }),
    ]).start();
  }, []);

  const deviceComponents = [
    { id: 'inv', title: 'Inverter', subtitle: '(012)', value: '5.166 kW', icon: 'bolt', status: 'normal', position: 'left-1' },
    { id: 'bat1', title: 'Battery', subtitle: '(1)', value: '100.0%', icon: 'battery-full', status: 'normal', position: 'left-2' },
    { id: 'bat3', title: 'Battery', subtitle: '(3)', value: '100.0%', icon: 'battery-full', status: 'normal', position: 'left-3' },
    { id: 'dc', title: 'DC Charger', subtitle: '', value: '0.000 kW', icon: 'plug', status: 'inactive', position: 'right-1' },
    { id: 'bat2', title: 'Battery', subtitle: '(2)', value: '100.0%', icon: 'battery-full', status: 'normal', position: 'right-2' },
    { id: 'batCluster', title: 'Battery Cluster', subtitle: '', value: '100.0%', icon: 'battery', status: 'normal', position: 'right-3' },
  ] as const;

  const leftBadges = deviceComponents.filter(c => c.position.startsWith('left'));
  const rightBadges = deviceComponents.filter(c => c.position.startsWith('right'));

  const navigateToDetailScreen = (componentId: string) => {
    switch (componentId) {
      case 'pvpanel':
        navigation.navigate('PVPanelDetail', { device });
        break;
      case 'gateway':
        navigation.navigate('GatewayDetail', { device });
        break;
      case 'inv':
      case 'dc':
        navigation.navigate('PVPanelDetail', { device, componentId });
        break;
      default:
        break;
    }
  };

  const getStatusColor = (status?: string) => {
    switch (status) {
      case 'warning': return COLORS.warning;
      case 'error': return COLORS.error;
      case 'inactive': return COLORS.slate400;
      default: return COLORS.success;
    }
  };

  const handleRealTimeInfo = () => {
    navigation.navigate('PVPanelDetail', { device, isRealTime: true });
  };

  const handleRestartDevice = () => {
    Alert.alert(
      'Cihazı Tekrar Başlat',
      'Cihazı şimdi yeniden başlatmak istediğinize emin misiniz?',
      [
        { text: 'Vazgeç', style: 'cancel' },
        {
          text: 'Evet, Başlat',
          style: 'destructive',
          onPress: () => {
            closeSheet();
            setTimeout(() => {
              Alert.alert('İşlem Başlatıldı', 'Cihaz yeniden başlatılıyor.');
            }, 150);
          },
        },
      ],
    );
  };

  return (
    <View style={styles.container}>
      <StatusBar barStyle={isDark ? 'light-content' : 'dark-content'} />

      <Animated.View
        style={[
          styles.header,
          { paddingTop: insets.top, opacity: headerOpacity, transform: [{ translateY: headerY }] },
        ]}
      >
        <View style={styles.headerContent}>
          <Pressable style={styles.backButton} onPress={() => navigation.goBack()}>
            <FontAwesome name="chevron-left" size={16} color={COLORS.textSoft} />
          </Pressable>
          <Text style={styles.headerTitle}>{device?.name || 'NetZr'}</Text>
          <Pressable style={styles.infoButton} onPress={handleRealTimeInfo}>
            <FontAwesome name="info-circle" size={18} color={COLORS.accent} />
          </Pressable>
        </View>
      </Animated.View>

      <ScrollView
        contentContainerStyle={{ paddingBottom: insets.bottom + SPACING.lg }}
        showsVerticalScrollIndicator={false}
        refreshControl={<RefreshControl refreshing={refreshing} onRefresh={onRefresh} tintColor={COLORS.accent} />}
      >
        <View style={[styles.heroCard, { marginTop: insets.top + 72 }]}>
          <View style={styles.headerRow}>
            <View>
              <Text style={styles.title}>{device?.name ?? 'NetZr'}</Text>
            </View>
            <Pressable style={styles.rtBtn} onPress={handleRealTimeInfo}>
              <FontAwesome name="line-chart" size={14} color={COLORS.accent} style={{ marginRight: 6 }} />
              <Text style={styles.rtBtnText}>Live Data</Text>
            </Pressable>
          </View>

          <View style={styles.visualWrap}>
            <Image source={require('../../../assets/images/device-image.png')} style={styles.visualImg} resizeMode="contain" />
          </View>

          <View style={styles.badgesBelowRow}>
            <View style={styles.badgesCol}>
              {leftBadges.map(b => (
                <DeviceBadge key={b.id} COLORS={COLORS} title={b.title} subtitle={b.subtitle} value={b.value} icon={b.icon} status={b.status} onPress={() => openSheet(b)} />
              ))}
            </View>
            <View style={styles.badgesCol}>
              {rightBadges.map(b => (
                <DeviceBadge key={b.id} COLORS={COLORS} title={b.title} subtitle={b.subtitle} value={b.value} icon={b.icon} status={b.status} align="right" onPress={() => openSheet(b)} />
              ))}
            </View>
          </View>
        </View>

        <View style={styles.statsContainer}>
          <View style={styles.statCard}>
            <FontAwesome name="bolt" size={18} color={COLORS.accent} />
            <Text style={styles.statValue}>8.24 kWh</Text>
            <Text style={styles.statLabel}>Günlük Üretim</Text>
          </View>
          <View style={styles.statCard}>
            <FontAwesome name="battery-full" size={18} color={COLORS.success} />
            <Text style={styles.statValue}>100%</Text>
            <Text style={styles.statLabel}>Batarya Şarjı</Text>
          </View>
          <View style={styles.statCard}>
            <FontAwesome name="plug" size={18} color={COLORS.textSoft} />
            <Text style={styles.statValue}>On-Grid</Text>
            <Text style={styles.statLabel}>Bağlantı</Text>
          </View>
        </View>


        <View style={styles.sectionContainer}>
          <Text style={styles.sectionTitle}>Netzr Cihazları</Text>
          <View style={styles.gridRow}>
            <GridCard COLORS={COLORS} title="PV Panel" subtitle="10.0 kWp" icon="sun-o" onPress={() => navigateToDetailScreen('pvpanel')} />
            <GridCard COLORS={COLORS} title="Energy Gateway" subtitle="On Grid" icon="exchange" onPress={() => navigateToDetailScreen('gateway')} />
          </View>
        </View>
      </ScrollView>

      <Modal visible={!!sheet} transparent animationType="none" onRequestClose={closeSheet}>
        <Animated.View style={[styles.scrim, { opacity: sheetOpacity }]}>
          <Pressable style={{ flex: 1 }} onPress={() => { if (canCloseRef.current) closeSheet(); }} />
        </Animated.View>
        <Animated.View style={[styles.sheet, { height: SHEET_H, paddingBottom: insets.bottom + SPACING.md, transform: [{ translateY: sheetY }] }]}>
          <View style={styles.sheetHandle} />
          <View style={styles.sheetHeader}>
            <View style={styles.sheetHeaderLeft}>
              <Text style={styles.sheetTitle}>{sheet?.title} {sheet?.subtitle}</Text>
              <Text style={styles.sheetValue}>{sheet?.value}</Text>
            </View>
            {sheet?.icon && (
              <View style={[styles.sheetIconContainer, { backgroundColor: getStatusColor(sheet?.status) }]}>
                <FontAwesome name={sheet?.icon as any} size={32} color="#fff" />
              </View>
            )}
          </View>
          <View style={styles.sheetDivider} />
          <Text style={styles.sheetSectionTitle}>Durum</Text>
          <View style={styles.sheetStatusRow}>
            <View style={[styles.sheetStatusDot, { backgroundColor: getStatusColor(sheet?.status) }]} />
            <Text style={styles.sheetStatusText}>
              {sheet?.status === 'warning' ? 'Uyarı' : sheet?.status === 'error' ? 'Hata' : sheet?.status === 'inactive' ? 'Pasif' : 'Normal Çalışıyor'}
            </Text>
          </View>
          <Text style={styles.sheetSectionTitle}>Detaylar</Text>
          <Text style={styles.sheetInfo}>
            {sheet?.description || 'Bu bileşen hakkında detaylı bilgi ve gerçek zamanlı veriler için detay sayfasına gidebilirsiniz.'}
          </Text>
          <View style={styles.sheetBtnRow}>
            <Button title="Detaya Git" onPress={() => navigateToDetailScreen(sheet?.id || '')} variant="primary" style={{ flex: 1 }} />
            <Button title="Kapat" onPress={closeSheet} variant="secondary" style={{ flex: 1 }} />
          </View>
          <View style={{ marginTop: SPACING.sm }}>
            <Button title="Cihazı Tekrar Başlat" onPress={handleRestartDevice} variant="danger" style={{ paddingVertical: SPACING.lg }} />
          </View>
        </Animated.View>
      </Modal>

      <BatteryDetailBottomSheet
        visible={batterySheetVisible}
        onClose={() => setBatterySheetVisible(false)}
        onRestart={handleRestartDevice}
        batteryData={batteryData}
      />
    </View>
  );
}
