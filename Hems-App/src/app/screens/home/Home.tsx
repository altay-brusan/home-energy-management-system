import React, { useMemo, useState, useCallback, useEffect } from 'react';
import { ScrollView, View, RefreshControl, Text, ActivityIndicator, Alert } from 'react-native';
import { useNavigation, useFocusEffect } from '@react-navigation/native';
import { NativeStackNavigationProp } from '@react-navigation/native-stack';
import AsyncStorage from '@react-native-async-storage/async-storage';

import MainLayout from '../../layouts/MainLayout';
import { useThemeStore } from '../../../store/themeStore';
import { getThemeColors } from '../../../theme/constants';
import { getHomeStyles } from './Home.styles';
import { useTranslation } from 'react-i18next';
import { HomeHeader, EmptyState, HomeCard } from './ui';
import { useSitesStore } from '../../../store/sitesStore';
import { useToastStore } from '../../../store/toastStore';

type RootStackParamList = {
  MainTabs: undefined;
  HomeSetup: undefined;
  DeviceSetup: undefined;
  DeviceAdd: { siteId: string };
  HomeDetail?: { homeId: string };
};

export default function Home () {
  const { t } = useTranslation();
  const navigation =
    useNavigation<NativeStackNavigationProp<RootStackParamList>>();

  const mode = useThemeStore(s => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);
  const styles = useMemo(() => getHomeStyles(COLORS, isDark), [COLORS, isDark]);

  // Sites store integration
  const {
    sites,
    selectedSite,
    isLoading,
    error,
    getUserSites,
    deleteSite,
    selectSiteById,
    clearError
  } = useSitesStore();
  const { show: showToast } = useToastStore();

  const [refreshing, setRefreshing] = useState(false);
  const [menuVisibleId, setMenuVisibleId] = useState<string | null>(null);
  const loadSites = useCallback(async () => {
    try {
      await getUserSites();
    } catch (error: any) {
      // Canceled request'leri sessizce handle et - normal akış bu
      if (error?.message?.includes('canceled') || error?.name === 'CanceledError' || error?.code === 'ERR_CANCELED') {return;
      }

      if (error.response?.status === 401) {return;
      }}
  }, [getUserSites]);
  useEffect(() => {
    loadSites();
  }, []);

  // Refresh sites when screen comes into focus (e.g., after adding a new home)
  useFocusEffect(
    useCallback(() => {loadSites();
    }, [loadSites])
  );

  // Auto-redirect to HomeSetup when no sites exist and not loading
  useEffect(() => {
    if (!isLoading && sites.length === 0) {
      navigation.navigate('HomeSetup');
    }
  }, [isLoading, sites.length, navigation]);

  useEffect(() => {
    const saveSitesToStorageAndNavigate = async () => {
      try {
        if (!sites || !Array.isArray(sites)) {return;
        }

        const sitesForStorage = sites.map(site => ({
          id: site.id,
          name: site.name,
          address: `${site.city || ''}, ${site.country || ''}`.replace(/^,\s*|,\s*$/g, ''),
          hasDevice: site.hasDevice
        }));
        await AsyncStorage.setItem('@user_homes', JSON.stringify(sitesForStorage));

      } catch (error) {}
    };

    if (sites && Array.isArray(sites) && sites.length > 0) {
      saveSitesToStorageAndNavigate();
    }
  }, [sites, navigation]);

 

  const onRefresh = useCallback(async () => {
    setRefreshing(true);
    try {
      await loadSites();
    } catch (error) {} finally {
      setRefreshing(false);
    }
  }, []);

  const handleDelete = async (id: string) => {
    if (!sites || !Array.isArray(sites)) {return;
    }

    const siteToDelete = sites.find(site => site.id === id);
    const siteName = siteToDelete?.name || 'this site';

    Alert.alert(
      t('homeScreen.deleteConfirm.title'),
      t('homeScreen.deleteConfirm.message', { siteName }),
      [
        {
          text: t('homeScreen.deleteConfirm.cancel'),
          style: 'cancel',
          onPress: () => setMenuVisibleId(null)
        },
        {
          text: t('homeScreen.deleteConfirm.delete'),
          style: 'destructive',
          onPress: async () => {
            try {
              await deleteSite(id);
              setMenuVisibleId(null);
              showToast({
                type: 'success',
                title: t('homeScreen.deleteSuccess.title'),
                message: t('homeScreen.deleteSuccess.message', { siteName }),
                duration: 3000,
              });
            } catch (error) {showToast({
                type: 'error',
                title: t('homeScreen.deleteError.title'),
                message: t('homeScreen.deleteError.message'),
                duration: 5000,
              });
            }
          }
        }
      ]
    );
  };

  const homes = sites.map(site => ({
    id: site.id,
    name: site.name,
    address: `${site.city || ''}, ${site.country || ''}`.replace(/^,\s*|,\s*$/g, ''),
    hasDevice: site.hasDevice,
    power: site.power || '0kW'
  }));

  if (isLoading && !refreshing) {
    return (
      <MainLayout>
        <View style={[styles.container, { justifyContent: 'center', alignItems: 'center' }]}>
          <ActivityIndicator size="large" color={COLORS.accent} />
          <Text style={[styles.sectionTitle, { marginTop: 16 }]}>
            {t('homeScreen.loading')}
          </Text>
        </View>
      </MainLayout>
    );
  }

  if (error) {
    return (
      <MainLayout>
        <View style={[styles.container, { justifyContent: 'center', alignItems: 'center' }]}>
          <Text style={[styles.sectionTitle, { color: COLORS.error, textAlign: 'center' }]}>
            {error}
          </Text>
          <Text
            style={[styles.sectionTitle, { marginTop: 16, color: COLORS.accent }]}
            onPress={() => {
              clearError();
              loadSites();
            }}
          >
            {t('homeScreen.retry')}
          </Text>
        </View>
      </MainLayout>
    );
  }

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

          <Text style={styles.sectionTitle}>{t('homeScreen.myHomes')}</Text>
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
