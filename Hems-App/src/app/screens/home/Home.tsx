import React, { useMemo, useState, useCallback } from 'react';
import { ScrollView, View, RefreshControl, Text } from 'react-native';
import { useNavigation } from '@react-navigation/native';
import { NativeStackNavigationProp } from '@react-navigation/native-stack';

import MainLayout from '../../layouts/MainLayout';
import { useThemeStore } from '../../../store/themeStore';
import { getThemeColors } from '../../../theme/constants';
import { getHomeStyles } from './Home.styles';
import { HomeHeader, HomeStats, EmptyState, HomeCard } from './ui';

type RootStackParamList = {
  MainTabs: undefined;
  HomeSetup: undefined;
  DeviceSetup: undefined;
  HomeDetail?: { homeId: string };
};

export default function Home() {
  const navigation =
    useNavigation<NativeStackNavigationProp<RootStackParamList>>();

  const mode = useThemeStore(s => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);
  const styles = useMemo(() => getHomeStyles(COLORS, isDark), [COLORS, isDark]);

  const [homes, setHomes] = useState([
    { id: '1', name: 'Istanbul Apartment', address: 'Beyoğlu, Istanbul', hasDevice: true },
    { id: '2', name: 'Summer House', address: 'Çeşme, Izmir', hasDevice: true },
    { id: '3', name: 'Office Building', address: 'Levent, Istanbul', hasDevice: false },
  ]);

  const [refreshing, setRefreshing] = useState(false);
  const onRefresh = useCallback(() => {
    setRefreshing(true);
    setTimeout(() => setRefreshing(false), 1000);
  }, []);

  const [menuVisibleId, setMenuVisibleId] = useState<string | null>(null);

  const handleDelete = (id: string) => {
    setHomes(prev => prev.filter(h => h.id !== id));
    setMenuVisibleId(null);
  };

  return (
    <MainLayout>
      <View style={styles.container}>
        <ScrollView
          contentContainerStyle={styles.scrollContent}
          refreshControl={
            <RefreshControl
              refreshing={refreshing}
              onRefresh={onRefresh}
              colors={[COLORS.accent]}
              tintColor={COLORS.accent}
            />
          }
          showsVerticalScrollIndicator={false}
        >
          <HomeHeader navigation={navigation} styles={styles} COLORS={COLORS} />
          <HomeStats homes={homes} styles={styles} COLORS={COLORS} />

          <Text style={styles.sectionTitle}>My Homes</Text>
          <View style={styles.cardsWrapper}>
            {homes.length === 0 ? (
              <EmptyState styles={styles} COLORS={COLORS} />
            ) : (
              homes.map(home => (
                <HomeCard
                  key={home.id}
                  home={home}
                  styles={styles}
                  COLORS={COLORS}
                  isDark={isDark}
                  navigation={navigation}
                  menuVisibleId={menuVisibleId}
                  setMenuVisibleId={setMenuVisibleId}
                  onDelete={handleDelete}
                />
              ))
            )}
          </View>
        </ScrollView>
      </View>
    </MainLayout>
  );
}
