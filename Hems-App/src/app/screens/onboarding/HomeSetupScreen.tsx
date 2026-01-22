import { zodResolver } from '@hookform/resolvers/zod';
import { useNavigation } from '@react-navigation/native';
import { NativeStackNavigationProp } from '@react-navigation/native-stack';
import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import {
  Alert,
  Keyboard,
  KeyboardAvoidingView,
  Linking,
  PermissionsAndroid,
  Platform,
  ScrollView,
  Text,
  TouchableOpacity,
  TouchableWithoutFeedback,
  View,
} from 'react-native';
import { LogOut, MapPin, CheckCircle, AlertCircle } from 'lucide-react-native';
import Geolocation from 'react-native-geolocation-service';

import { Card, ThemeToggle, LanguageSelector } from '../../../components';
import Input from '../../../components/forms/Input';
import Button from '../../../components/ui/Button';
import CountrySelector, { Country } from '../../../components/ui/CountrySelector/CountrySelector';
import CitySelector, { City } from '../../../components/ui/CitySelector/CitySelector';
import { usePlatform } from '../../../hooks/usePlatform';
import useAuth from '../../../hooks/useAuth';
import { useOnboardingStore } from '../../../store/onboardingStore';
import { useSitesStore } from '../../../store/sitesStore';
import { useThemeStore } from '../../../store/themeStore';
import { useToastStore } from '../../../store/toastStore';
import { getThemeColors } from '../../../theme/constants';
import { reverseGeocode, GeocodingResult } from '../../../utils/geocoding';
import MainLayout from '../../layouts/MainLayout';
import { getOnboardingStyles } from './Onboarding.styles';
import { getHomeSetupSchema, type HomeSetupForm } from './home.schema';
import { navigationRef } from '../../routes';

type RootStackParamList = {
  Home: undefined;
  HomeSetup: undefined;
  DeviceSetup: undefined;
  MainTabs: undefined;
};

export default function HomeSetupScreen () {
  const { t } = useTranslation();
  const navigation =
    useNavigation<NativeStackNavigationProp<RootStackParamList>>();
  const { setHome } = useOnboardingStore();
  const { createSite, sites } = useSitesStore();
  const hasSites = sites.length > 0;
  const { isIOS } = usePlatform();
  const { logout } = useAuth();
  const { show: showToast } = useToastStore();
  const mode = useThemeStore(s => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);
  const styles = useMemo(() => getOnboardingStyles(COLORS, isDark), [COLORS, isDark]);

  const [isGettingLocation, setIsGettingLocation] = useState(false);
  const [locationData, setLocationData] = useState<{
    coordinates: { latitude: number; longitude: number } | null;
    geocoding: GeocodingResult | null;
    hasPermission: boolean | null;
  }>({
    coordinates: null,
    geocoding: null,
    hasPermission: null,
  });

  const [selectedCountry, setSelectedCountry] = useState<Country | null>(null);
  const [selectedCity, setSelectedCity] = useState<City | null>(null);

  const {
    control,
    handleSubmit,
    setValue,
    formState: { errors, isSubmitting },
  } = useForm<HomeSetupForm>({
    resolver: zodResolver(getHomeSetupSchema(t)),
    defaultValues: {
      homeName: '',
      addressLine: '',
      countryId: '',
      cityId: '',
    },
  });


  // Get device timezone
  const getDeviceTimezone = (): string => {
    try {
      const timezone = Intl.DateTimeFormat().resolvedOptions().timeZone;
      return timezone || 'UTC';
    } catch (error) {
      console.warn('Could not get device timezone, using UTC');
      return 'UTC';
    }
  };

  // Request location permissions
  const requestLocationPermission = useCallback(async () => {
    if (Platform.OS === 'android') {
      try {
        const granted = await PermissionsAndroid.request(
          PermissionsAndroid.PERMISSIONS.ACCESS_FINE_LOCATION,
          {
            title: t('homeSetup.locationPermission.title'),
            message: t('homeSetup.locationPermission.message'),
            buttonNeutral: t('homeSetup.locationPermission.askLater'),
            buttonNegative: t('homeSetup.locationPermission.cancel'),
            buttonPositive: t('homeSetup.locationPermission.ok'),
          }
        );
        return granted === PermissionsAndroid.RESULTS.GRANTED;
      } catch (err) {return false;
      }
    } else if (Platform.OS === 'ios') {
      try {
        const status = await Geolocation.requestAuthorization('whenInUse');
        return status === 'granted';
      } catch (err) {return false;
      }
    }
    return false;
  }, [t]);

  // Get current location coordinates and reverse geocode
  const getCurrentLocation = useCallback(async () => {
    setIsGettingLocation(true);
    try {
      const hasPermission = await requestLocationPermission();

      if (!hasPermission) {
        setLocationData(prev => ({ ...prev, hasPermission: false }));
        return null;
      }

      setLocationData(prev => ({ ...prev, hasPermission: true }));

      const position = await new Promise<Geolocation.GeoPosition>((resolve, reject) => {
        Geolocation.getCurrentPosition(
          resolve,
          reject,
          {
            enableHighAccuracy: true,
            timeout: 30000,
            maximumAge: 0,
            forceRequestLocation: true,
            showLocationDialog: true,
          }
        );
      });

      const { latitude, longitude } = position.coords;

      console.log('[Location] GPS position:', { latitude, longitude });

      // Reverse geocode to get address
      const geocodingResult = await reverseGeocode(latitude, longitude);

      console.log('[Location] Geocoding result:', geocodingResult);

      setLocationData({
        coordinates: { latitude, longitude },
        geocoding: geocodingResult,
        hasPermission: true,
      });

      return { latitude, longitude, geocoding: geocodingResult };
    } catch (error) {
      console.warn('[Location] Error getting location:', error);
      setLocationData(prev => ({ ...prev, hasPermission: true }));
      return null;
    } finally {
      setIsGettingLocation(false);
    }
  }, [requestLocationPermission]);

  // Open device settings
  const openSettings = useCallback(() => {
    if (Platform.OS === 'ios') {
      Linking.openURL('app-settings:');
    } else {
      Linking.openSettings();
    }
  }, []);

  // Country selection handler
  const handleCountrySelect = useCallback((country: Country) => {
    setSelectedCountry(country);
    setSelectedCity(null);
    setValue('countryId', country.id);
    setValue('cityId', '');
  }, [setValue]);

  // City selection handler
  const handleCitySelect = useCallback((city: City) => {
    setSelectedCity(city);
    setValue('cityId', city.id);
  }, [setValue]);

  const onSubmit = useCallback(
    async (values: HomeSetupForm) => {
      try {
        // Check if we have location data
        if (!locationData.coordinates) {
          Alert.alert(
            t('homeSetup.locationRequired.title'),
            t('homeSetup.locationRequired.message'),
            [
              { text: t('common.cancel'), style: 'cancel' },
              {
                text: t('homeSetup.locationRequired.retry'),
                onPress: getCurrentLocation,
              },
            ]
          );
          return;
        }

        const siteData = {
          name: values.homeName,
          addressLine: values.addressLine || null,
          city: selectedCity?.name || locationData.geocoding?.city || null,
          country: selectedCountry?.name || locationData.geocoding?.country || null,
          district: locationData.geocoding?.district || null,
          timezone: getDeviceTimezone(),
          latitude: locationData.coordinates.latitude,
          longitude: locationData.coordinates.longitude,
        };

        console.log('[HomeSetup] Creating site:', JSON.stringify(siteData, null, 2));

        await createSite(siteData);

        showToast({
          type: 'success',
          title: t('homeSetup.success.title'),
          message: t('homeSetup.success.message', { homeName: values.homeName }),
          duration: 3000,
        });

        setHome({
          name: values.homeName,
          address: {
            line1: values.addressLine || '',
            line2: '',
            city: selectedCity?.name || locationData.geocoding?.city || '',
            state: '',
            country: selectedCountry?.name || locationData.geocoding?.country || '',
          },
        });

        // Reset navigation stack to MainTabs to show the tab bar
        navigationRef.current?.reset({
          index: 0,
          routes: [{ name: 'MainTabs' }],
        });
      } catch (error) {
        showToast({
          type: 'error',
          title: t('homeSetup.createError.title'),
          message: t('homeSetup.createError.message'),
          duration: 5000,
        });
      }
    },
    [navigation, setHome, createSite, locationData, getCurrentLocation, showToast, t, selectedCountry, selectedCity],
  );

  useEffect(() => {
    getCurrentLocation();
  }, [getCurrentLocation]);

  return (
    <MainLayout>
      <TouchableWithoutFeedback onPress={Keyboard.dismiss} accessible={false}>
        <View style={styles.root}>
          <View style={[styles.topBar, { justifyContent: 'space-between', paddingHorizontal: 16 }]}>
            {/* Logout Button - only show when user has no sites (forced to stay here) */}
            {!hasSites ? (
              <TouchableOpacity
                onPress={() => {
                  Alert.alert(
                    t('profile.logoutConfirm.title', 'Logout'),
                    t('profile.logoutConfirm.message', 'Are you sure you want to logout?'),
                    [
                      { text: t('common.cancel', 'Cancel'), style: 'cancel' },
                      { text: t('profile.logout', 'Logout'), style: 'destructive', onPress: logout },
                    ]
                  );
                }}
                style={{ padding: 8 }}
              >
                <LogOut size={22} color={COLORS.textSoft} />
              </TouchableOpacity>
            ) : (
              <View style={{ width: 38 }} />
            )}
            <View style={{ flexDirection: 'row', alignItems: 'center', gap: 8 }}>
              <LanguageSelector />
              <ThemeToggle style={styles.themeToggle} />
            </View>
          </View>

          <View style={styles.content}>
            <View style={styles.bgTopShade} pointerEvents="none" />
            <View style={styles.bgBottomShade} pointerEvents="none" />

            <KeyboardAvoidingView
              style={styles.keyboardAvoider}
              behavior={isIOS ? 'padding' : 'height'}
              keyboardVerticalOffset={isIOS ? 0 : 0}
            >
              <ScrollView
                contentContainerStyle={styles.scrollContent}
                keyboardShouldPersistTaps="handled"
                keyboardDismissMode="on-drag"
                showsVerticalScrollIndicator={false}
                bounces={false}
                scrollEventThrottle={16}
                automaticallyAdjustKeyboardInsets={isIOS}
              >
                <Card variant="elevated" padding={24} style={styles.card}>
                  <View style={styles.inner}>
                    <Text style={styles.title}>{t('homeSetup.title')}</Text>
                    <Text style={styles.subtitle}>{t('homeSetup.subtitle')}</Text>

                    <View style={styles.formWrapper}>
                      {/* Location Status Card */}
                      <View style={{
                        backgroundColor: locationData.coordinates
                          ? (isDark ? 'rgba(34, 197, 94, 0.1)' : 'rgba(34, 197, 94, 0.08)')
                          : locationData.hasPermission === false
                            ? (isDark ? 'rgba(239, 68, 68, 0.1)' : 'rgba(239, 68, 68, 0.08)')
                            : (isDark ? 'rgba(59, 130, 246, 0.1)' : 'rgba(59, 130, 246, 0.08)'),
                        borderRadius: 12,
                        padding: 16,
                        marginBottom: 20,
                        borderWidth: 1,
                        borderColor: locationData.coordinates
                          ? (isDark ? 'rgba(34, 197, 94, 0.3)' : 'rgba(34, 197, 94, 0.2)')
                          : locationData.hasPermission === false
                            ? (isDark ? 'rgba(239, 68, 68, 0.3)' : 'rgba(239, 68, 68, 0.2)')
                            : (isDark ? 'rgba(59, 130, 246, 0.3)' : 'rgba(59, 130, 246, 0.2)'),
                      }}>
                        <View style={{ flexDirection: 'row', alignItems: 'center', marginBottom: 8 }}>
                          {isGettingLocation ? (
                            <MapPin size={20} color={COLORS.primary} />
                          ) : locationData.coordinates ? (
                            <CheckCircle size={20} color="#22c55e" />
                          ) : locationData.hasPermission === false ? (
                            <AlertCircle size={20} color="#ef4444" />
                          ) : (
                            <MapPin size={20} color={COLORS.primary} />
                          )}
                          <Text style={{
                            marginLeft: 8,
                            fontSize: 14,
                            fontWeight: '600',
                            color: locationData.coordinates
                              ? '#22c55e'
                              : locationData.hasPermission === false
                                ? '#ef4444'
                                : COLORS.primary,
                          }}>
                            {isGettingLocation
                              ? t('homeSetup.location.detecting')
                              : locationData.coordinates
                                ? t('homeSetup.location.detected')
                                : locationData.hasPermission === false
                                  ? t('homeSetup.location.permissionDenied')
                                  : t('homeSetup.location.waiting')}
                          </Text>
                        </View>

                        {locationData.coordinates && locationData.geocoding && (
                          <Text style={{
                            fontSize: 13,
                            color: COLORS.textSoft,
                            marginTop: 4,
                          }}>
                            {[
                              locationData.geocoding.district,
                              locationData.geocoding.city,
                              locationData.geocoding.country,
                            ].filter(Boolean).join(', ')}
                          </Text>
                        )}

                        {locationData.hasPermission === false && (
                          <TouchableOpacity
                            onPress={openSettings}
                            style={{ marginTop: 8 }}
                          >
                            <Text style={{
                              fontSize: 13,
                              color: COLORS.primary,
                              textDecorationLine: 'underline',
                            }}>
                              {t('homeSetup.location.openSettings')}
                            </Text>
                          </TouchableOpacity>
                        )}

                        {!locationData.coordinates && locationData.hasPermission !== false && !isGettingLocation && (
                          <TouchableOpacity
                            onPress={getCurrentLocation}
                            style={{ marginTop: 8 }}
                          >
                            <Text style={{
                              fontSize: 13,
                              color: COLORS.primary,
                              textDecorationLine: 'underline',
                            }}>
                              {t('homeSetup.location.retry')}
                            </Text>
                          </TouchableOpacity>
                        )}
                      </View>

                      {/* Home Name Input */}
                      <Controller
                        control={control}
                        name="homeName"
                        render={({ field }) => (
                          <Input
                            label={t('homeSetup.homeName')}
                            placeholder={t('homeSetup.homeNamePlaceholder')}
                            autoCapitalize="words"
                            value={field.value}
                            onChangeText={field.onChange}
                            onBlur={field.onBlur}
                            error={errors.homeName?.message}
                          />
                        )}
                      />

                      {/* Optional Address Line */}
                      <Controller
                        control={control}
                        name="addressLine"
                        render={({ field }) => (
                          <Input
                            label={t('homeSetup.addressLine')}
                            placeholder={t('homeSetup.addressLinePlaceholder')}
                            autoCapitalize="words"
                            value={field.value ?? ''}
                            onChangeText={field.onChange}
                            onBlur={field.onBlur}
                            error={errors.addressLine?.message}
                          />
                        )}
                      />

                      {/* Country Selector */}
                      <CountrySelector
                        selectedCountry={selectedCountry}
                        onCountrySelect={handleCountrySelect}
                        label={t('homeSetup.country')}
                        placeholder={t('homeSetup.countryPlaceholder')}
                      />

                      {/* City Selector */}
                      <CitySelector
                        selectedCity={selectedCity}
                        onCitySelect={handleCitySelect}
                        selectedCountryId={selectedCountry?.id}
                        label={t('homeSetup.city')}
                        placeholder={t('homeSetup.cityPlaceholder')}
                      />

                      <Button
                        title={
                          isSubmitting
                            ? t('homeSetup.saving')
                            : t('homeSetup.save')
                        }
                        onPress={handleSubmit(onSubmit)}
                        disabled={isSubmitting || isGettingLocation || !locationData.coordinates}
                        style={styles.primaryButton}
                        textStyle={styles.primaryButtonText}
                      />

                      {/* Only show back button if user already has sites */}
                      {hasSites && (
                        <Button
                          title={t('homeSetup.goBack')}
                          onPress={() => navigation.goBack()}
                          variant="secondary"
                          style={[styles.secondaryButton, { marginTop: 12 }]}
                          textStyle={styles.secondaryButtonText}
                        />
                      )}
                    </View>
                  </View>
                </Card>
              </ScrollView>
            </KeyboardAvoidingView>
          </View>
        </View>
      </TouchableWithoutFeedback>
    </MainLayout>
  );
}
