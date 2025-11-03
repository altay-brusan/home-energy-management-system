import React, { useMemo } from 'react';
import { Image, Pressable, SafeAreaView, Text, View } from 'react-native';
import FontAwesome from '@react-native-vector-icons/fontawesome';
import { useThemeStore } from '../../../store/themeStore';
import { getThemeColors } from '../../../theme/constants';
import { getDeviceListStyles } from './DeviceListScreen.styles';
import { useNavigation } from '@react-navigation/native';
import { useI18n } from '../../../hooks/useI18n';

type Device = {
  id: string;
  name: string;
  serial: string;
  status: 'online' | 'offline';
  mode: 'On Grid' | 'Off Grid';
  powerKw: number;
};

const DEVICE: Device = {
  id: 'dev-1', 
  name: 'SigenStor', 
  serial: 'CMU********0012', 
  status: 'online', 
  mode: 'On Grid', 
  powerKw: 5.166 
};

export default function DeviceListScreen() {
  const isDark = useThemeStore(s => s.mode) === 'dark';
  const COLORS = getThemeColors(isDark);
  const styles = useMemo(() => getDeviceListStyles(COLORS), [COLORS]);
  const navigation = useNavigation<any>();
  const { t } = useI18n();

  return (
    <SafeAreaView style={styles.screen}>
      <View style={styles.container}>
        <Text style={styles.title}>{t('deviceList.title')}</Text>
        
        <Pressable
          style={styles.card}
          onPress={() => navigation.navigate('DeviceDetail', { device: DEVICE })}
        >
          <View style={styles.thumbWrap}>
            <Image 
              source={require('../../../assets/images/device-image.png')} 
              style={styles.image} 
              resizeMode="contain" 
            />
          </View>
          <View style={styles.body}>
            <View style={{ flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between' }}>
              <Text style={styles.name} numberOfLines={1}>{DEVICE.name}</Text>
              <FontAwesome name="chevron-right" size={14} color={COLORS.slate400} style={styles.rightIcon} />
            </View>
            <Text style={styles.serial} numberOfLines={1}>SN: {DEVICE.serial}</Text>
            <View style={styles.row}>
              <View style={{ flexDirection: 'row', alignItems: 'center' }}>
                <View style={[styles.dot, { backgroundColor: DEVICE.status === 'online' ? COLORS.success : COLORS.error }]} />
                <Text style={styles.serial}>{DEVICE.status === 'online' ? t('deviceList.online') : t('deviceList.offline')} • {DEVICE.mode === 'On Grid' ? t('deviceList.onGrid') : t('deviceList.offGrid')}</Text>
              </View>
              <View style={styles.chip}>
                <Text style={styles.chipText}>{DEVICE.powerKw.toFixed(3)} kW</Text>
              </View>
            </View>
          </View>
        </Pressable>
      </View>
    </SafeAreaView>
  );
}
