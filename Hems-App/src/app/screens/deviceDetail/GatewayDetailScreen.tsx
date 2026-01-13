import React, { useMemo, useState } from 'react';
import { SafeAreaView, ScrollView, Text, View, RefreshControl } from 'react-native';
import { useThemeStore } from '../../../store/themeStore';
import { getThemeColors } from '../../../theme/constants';
import { getGWStyles } from './GatewayDetailScreen.styles';
import { Button } from '../../../components';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useNavigation } from '@react-navigation/native';
import DeviceHeader from './ui/DeviceHeader';
import { useI18n } from '../../../hooks/useI18n';

export default function GatewayDetailScreen() {
  const navigation = useNavigation<any>();
  const isDark = useThemeStore(s => s.mode) === 'dark';
  const COLORS = getThemeColors(isDark);
  const styles = useMemo(() => getGWStyles(COLORS), [COLORS]);
  const { t } = useI18n();
  const [mode, setMode] = useState<'grid' | 'offgrid'>('grid');
  const insets = useSafeAreaInsets();
  const [refreshing, setRefreshing] = useState(false);
  const onRefresh = () => {
    setRefreshing(true);
    setTimeout(() => setRefreshing(false), 800);
  };

  return (
    <SafeAreaView style={styles.screen}>
      <DeviceHeader title={t('deviceDetail.gateway.title')} onBack={() => navigation.goBack()} />

      <ScrollView
        contentContainerStyle={[styles.container, { paddingTop: insets.top + 60 }]}
        showsVerticalScrollIndicator={false}
        refreshControl={
          <RefreshControl refreshing={refreshing} onRefresh={onRefresh} tintColor={COLORS.accent} />
        }
      >
        <View style={styles.card}>
          <Text style={styles.title}>{t('deviceDetail.gateway.title')}</Text>
          <Text style={styles.sub}>{t('deviceDetail.gateway.subtitle')}</Text>
          <View style={styles.row}>
            <Button
              title={mode === 'grid' ? t('deviceDetail.gateway.goOffGrid') : t('deviceDetail.gateway.goOnGrid')}
              variant={'primary'}
              style={[styles.switchBtn, { backgroundColor: COLORS.accent }]}
              textStyle={{ color: COLORS.white, fontWeight: '700' }}
              onPress={() => setMode(p => (p === 'grid' ? 'offgrid' : 'grid'))}
            />
          </View>
        </View>

        <View style={styles.card}>
          <Text style={styles.title}>{t('deviceDetail.gateway.inverterSide')}</Text>
          <View style={styles.gridRow}><Text style={styles.gridText}>{t('deviceDetail.gateway.voltage')}</Text><Text style={styles.gridText}>—</Text></View>
          <View style={styles.gridRow}><Text style={styles.gridText}>{t('deviceDetail.gateway.frequency')}</Text><Text style={styles.gridText}>—</Text></View>
          <View style={styles.gridRow}><Text style={styles.gridText}>{t('deviceDetail.gateway.phase')}</Text><Text style={styles.gridText}>—</Text></View>
        </View>

        <View style={styles.card}>
          <Text style={styles.title}>{t('deviceDetail.gateway.gridSide')}</Text>
          <View style={styles.gridRow}><Text style={styles.gridText}>{t('deviceDetail.gateway.voltage')}</Text><Text style={styles.gridText}>—</Text></View>
          <View style={styles.gridRow}><Text style={styles.gridText}>{t('deviceDetail.gateway.frequency')}</Text><Text style={styles.gridText}>—</Text></View>
          <View style={styles.gridRow}><Text style={styles.gridText}>{t('deviceDetail.gateway.mode')}</Text><Text style={styles.gridText}>{mode === 'grid' ? t('deviceList.onGrid') : t('deviceList.offGrid')}</Text></View>
        </View>

        <View style={styles.card}>
          <Text style={styles.title}>{t('deviceDetail.gateway.alarmFault')}</Text>
          <Text style={styles.sub}>{t('deviceDetail.gateway.noActiveAlarm')}</Text>
        </View>
      </ScrollView>
    </SafeAreaView>
  );
}
