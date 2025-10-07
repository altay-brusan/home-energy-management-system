import React, { useMemo, useState } from 'react';
import { SafeAreaView, ScrollView, Text, View, RefreshControl } from 'react-native';
import { useThemeStore } from '../../../store/themeStore';
import { getThemeColors } from '../../../theme/constants';
import { getGWStyles } from './GatewayDetailScreen.styles';
import { Button } from '../../../components';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useNavigation } from '@react-navigation/native';
import DeviceHeader from './ui/DeviceHeader';

export default function GatewayDetailScreen() {
  const navigation = useNavigation<any>();
  const isDark = useThemeStore(s => s.mode) === 'dark';
  const COLORS = getThemeColors(isDark);
  const styles = useMemo(() => getGWStyles(COLORS), [COLORS]);
  const [mode, setMode] = useState<'grid' | 'offgrid'>('grid');
  const insets = useSafeAreaInsets();
  const [refreshing, setRefreshing] = useState(false);
  const onRefresh = () => {
    setRefreshing(true);
    setTimeout(() => setRefreshing(false), 800);
  };

  return (
    <SafeAreaView style={styles.screen}>
      <DeviceHeader title="Energy Gateway" onBack={() => navigation.goBack()} />

      <ScrollView
        contentContainerStyle={[styles.container, { paddingTop: insets.top + 60 }]}
        showsVerticalScrollIndicator={false}
        refreshControl={
          <RefreshControl refreshing={refreshing} onRefresh={onRefresh} tintColor={COLORS.accent} />
        }
      >
        <View style={styles.card}>
          <Text style={styles.title}>Energy Gateway</Text>
          <Text style={styles.sub}>Grid/Off Grid kontrolü ve ölçümler</Text>
          <View style={styles.row}>
            <Button
              title={mode === 'grid' ? 'Go Off-Grid' : 'Go On-Grid'}
              variant={'primary'}
              style={[styles.switchBtn, { backgroundColor: COLORS.accent }]}
              textStyle={{ color: COLORS.white, fontWeight: '700' }}
              onPress={() => setMode(p => (p === 'grid' ? 'offgrid' : 'grid'))}
            />
          </View>
        </View>

        <View style={styles.card}>
          <Text style={styles.title}>Inverter Side</Text>
          <View style={styles.gridRow}><Text style={styles.gridText}>Voltaj</Text><Text style={styles.gridText}>230 V</Text></View>
          <View style={styles.gridRow}><Text style={styles.gridText}>Frekans</Text><Text style={styles.gridText}>50 Hz</Text></View>
          <View style={styles.gridRow}><Text style={styles.gridText}>Faz</Text><Text style={styles.gridText}>L1</Text></View>
        </View>

        <View style={styles.card}>
          <Text style={styles.title}>Grid Side</Text>
          <View style={styles.gridRow}><Text style={styles.gridText}>Voltaj</Text><Text style={styles.gridText}>231 V</Text></View>
          <View style={styles.gridRow}><Text style={styles.gridText}>Frekans</Text><Text style={styles.gridText}>49.9 Hz</Text></View>
          <View style={styles.gridRow}><Text style={styles.gridText}>Mod</Text><Text style={styles.gridText}>{mode === 'grid' ? 'On Grid' : 'Off Grid'}</Text></View>
        </View>

        <View style={styles.card}>
          <Text style={styles.title}>Alarm & Fault</Text>
          <Text style={styles.sub}>Şu anda aktif bir alarm yok.</Text>
        </View>
      </ScrollView>
    </SafeAreaView>
  );
}
