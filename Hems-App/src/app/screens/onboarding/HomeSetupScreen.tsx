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
  PermissionsAndroid, Platform,
  ScrollView,
  Text,
  TouchableWithoutFeedback,
  View,
} from 'react-native';
import Geolocation from 'react-native-geolocation-service';

import { Card, CitySelector, CountrySelector, ThemeToggle } from '../../../components';
import Input from '../../../components/forms/Input';
import Button from '../../../components/ui/Button';
import type { City } from '../../../components/ui/CitySelector/CitySelector';
import type { Country } from '../../../components/ui/CountrySelector/CountrySelector';
import { usePlatform } from '../../../hooks/usePlatform';
import { useOnboardingStore } from '../../../store/onboardingStore';
import { useSitesStore } from '../../../store/sitesStore';
import { useThemeStore } from '../../../store/themeStore';
import { useToastStore } from '../../../store/toastStore';
import { getThemeColors } from '../../../theme/constants';
import MainLayout from '../../layouts/MainLayout';
import { getOnboardingStyles } from './Onboarding.styles';
import { getHomeSetupSchema, type HomeSetupForm } from './home.schema';

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
  const { createSite } = useSitesStore();
  const { isIOS } = usePlatform();
  const { show: showToast } = useToastStore();
  const mode = useThemeStore(s => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);
  const styles = useMemo(() => getOnboardingStyles(COLORS, isDark), [COLORS, isDark]);

  const [selectedCity, setSelectedCity] = useState<City | null>(null);
  const [selectedCountry, setSelectedCountry] = useState<Country | null>(null);
  const [isGettingLocation, setIsGettingLocation] = useState(false);
  const [coordinates, setCoordinates] = useState<{ latitude: number; longitude: number } | null>(null);

  const {
    control,
    handleSubmit,
    setValue,
    formState: { errors, isSubmitting },
  } = useForm<HomeSetupForm>({
   resolver: zodResolver(getHomeSetupSchema(t)),
    defaultValues: {
      homeName: '',
      addressLine1: '',
      addressLine2: '',
      city: '',
      state: '',
      country: '',
    },
  });


  // Helper function to get timezone based on country
  const getTimezoneForCountry = (country: string): string => {
    const timezoneMap: Record<string, string> = {
      'Turkey': 'Europe/Istanbul',
      'United States': 'America/New_York',
      'United Kingdom': 'Europe/London',
      'Germany': 'Europe/Berlin',
      'France': 'Europe/Paris',
      'Italy': 'Europe/Rome',
      'Spain': 'Europe/Madrid',
      'Netherlands': 'Europe/Amsterdam',
      'Canada': 'America/Toronto',
      'Australia': 'Australia/Sydney',
      'Japan': 'Asia/Tokyo',
      'China': 'Asia/Shanghai',
      'India': 'Asia/Kolkata',
      'Brazil': 'America/Sao_Paulo',
      'Mexico': 'America/Mexico_City',
      'Russia': 'Europe/Moscow',
      'South Korea': 'Asia/Seoul',
      'Singapore': 'Asia/Singapore',
      'United Arab Emirates': 'Asia/Dubai',
    };

    return timezoneMap[country] || 'UTC';
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

  // Get current location coordinates
  const getCurrentLocation = useCallback(async () => {
    setIsGettingLocation(true);
    try {
      // Request location permissions
      const hasPermission = await requestLocationPermission();
      if (!hasPermission) {
        Alert.alert(
          t('homeSetup.locationPermission.title'),
          t('homeSetup.locationPermission.message')
        );
        return null;
      }

      // Get current position using react-native-geolocation-service
      const position = await new Promise<Geolocation.GeoPosition>((resolve, reject) => {
        Geolocation.getCurrentPosition(
          resolve,
          reject,
          {
            enableHighAccuracy: true,
            timeout: 15000,
            maximumAge: 10000,
          }
        );
      });

      const { latitude, longitude } = position.coords;setCoordinates({ latitude, longitude });

      return { latitude, longitude };
    } catch (error) {Alert.alert(
        t('homeSetup.locationError.title'),
        t('homeSetup.locationError.message')
      );
      return null;
    } finally {
      setIsGettingLocation(false);
    }
  }, [t, setValue, requestLocationPermission]);

  // Get coordinates from address using geocoding
  const getCoordinatesFromAddress = useCallback(async (
    addressLine: string,
    city: string,
    country: string
  ) => {
    try {
      // For now, we'll return null and let the app use current location as fallback
      // In a real app, you would use a geocoding service like Google Maps API
      // or OpenStreetMap Nominatim API
      const fullAddress = `${addressLine}, ${city}, ${country}`;// TODO: Implement geocoding service
      // const response = await fetch(`https://nominatim.openstreetmap.org/search?format=json&q=${encodeURIComponent(fullAddress)}`);
      // const results = await response.json();
      // if (results.length > 0) {
      //   return { latitude: parseFloat(results[0].lat), longitude: parseFloat(results[0].lon) };
      // }

      return null;
    } catch (error) {return null;
    }
  }, []);

  const onSubmit = useCallback(
    async (values: HomeSetupForm) => {
      try {
        const hasLocationPermission = await requestLocationPermission();
        if (!hasLocationPermission) {
          Alert.alert(
            t('homeSetup.locationRequired.title'),
            t('homeSetup.locationRequired.message'),
            [
              {
                text: t('homeSetup.locationRequired.cancel'),
                style: 'cancel',
              },
              {
                text: t('homeSetup.locationRequired.settings'),
                onPress: () => {
                  Alert.alert(
                    t('homeSetup.locationSettings.title'),
                    t('homeSetup.locationSettings.message')
                  );
                },
              },
            ]
          );
          return;
        }

        if (!selectedCity) {
          Alert.alert(
            t('homeSetup.validationError.title'),
            t('homeSetup.validationError.cityRequired')
          );
          return;
        }

        if (!selectedCountry) {
          Alert.alert(
            t('homeSetup.validationError.title'),
            t('homeSetup.validationError.countryRequired')
          );
          return;
        }

        const addressLine = values.addressLine1 + (values.addressLine2 ? `, ${values.addressLine2}` : '');
        const city = selectedCity?.name || values.city || '';
        const country = selectedCountry?.name || values.country || 'Turkey';

        let coordinates: { latitude: number; longitude: number } | null = await getCoordinatesFromAddress(addressLine, city, country);
        
        if (!coordinates) {
          const currentLocation = await getCurrentLocation();
          coordinates = currentLocation;
        }

        const siteData = {
          name: values.homeName,
          addressLine,
          city,
          country,
          timezone: getTimezoneForCountry(country),
          latitude: coordinates?.latitude || null,
          longitude: coordinates?.longitude || null,
        };await createSite(siteData);
        
        showToast({
          type: 'success',
          title: t('homeSetup.success.title'),
          message: t('homeSetup.success.message', { homeName: values.homeName }),
          duration: 3000,
        });
        
        setHome({
          name: values.homeName,
          address: {
            line1: values.addressLine1,
            line2: values.addressLine2 || '',
            city,
            state: values.state || '',
            country,
          },
        });

        navigation.navigate('Home');
      } catch (error) {showToast({
          type: 'error',
          title: t('homeSetup.createError.title'),
          message: t('homeSetup.createError.message'),
          duration: 5000,
        });
      }
    },
    [navigation, setHome, createSite, selectedCity, selectedCountry, getCoordinatesFromAddress, getCurrentLocation, getTimezoneForCountry, requestLocationPermission, t],
  );

  useEffect(() => {
    getCurrentLocation();
  }, [getCurrentLocation]);

  return (
    <MainLayout>
      <TouchableWithoutFeedback onPress={Keyboard.dismiss} accessible={false}>
        <View style={styles.root}>
          <View style={styles.topBar}>
            <ThemeToggle style={styles.themeToggle} />
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
                      <Controller
                        control={control}
                        name="addressLine1"
                        render={({ field }) => (
                          <Input
                            label={t('homeSetup.streetAddress')}
                            placeholder={t('homeSetup.streetAddressPlaceholder')}
                            autoCapitalize="words"
                            value={field.value}
                            onChangeText={field.onChange}
                            onBlur={field.onBlur}
                            error={errors.addressLine1?.message}
                          />
                        )}
                      />
                      <Controller
                        control={control}
                        name="addressLine2"
                        render={({ field }) => (
                          <Input
                            label={t('homeSetup.apartment')}
                            placeholder={t('homeSetup.apartmentPlaceholder')}
                            autoCapitalize="words"
                            value={field.value ?? ''}
                            onChangeText={field.onChange}
                            onBlur={field.onBlur}
                            error={errors.addressLine2?.message}
                          />
                        )}
                      />

                      <View style={styles.dualRow}>
                        <View style={styles.grow}>
                          <CountrySelector
                            selectedCountry={selectedCountry}
                            onCountrySelect={(country) => {
                              setSelectedCountry(country);
                              setSelectedCity(null); 
                              setValue('country', country.name);
                            }}
                            error={errors.country?.message}
                            placeholder={t('homeSetup.countryPlaceholder')}
                            label={t('homeSetup.country')}
                          />
                        </View>
                        <View style={styles.grow}>
                          <CitySelector
                            selectedCity={selectedCity}
                            onCitySelect={(city) => {
                              setSelectedCity(city);
                              setValue('city', city.name);
                            }}
                            selectedCountryId={selectedCountry?.id}
                            error={errors.city?.message}
                            placeholder={t('homeSetup.cityPlaceholder')}
                            label={t('homeSetup.city')}
                          />
                        </View>
                      </View>

                      <Controller
                        control={control}
                        name="state"
                        render={({ field }) => (
                          <Input
                            label={t('homeSetup.state')}
                            placeholder={t('homeSetup.statePlaceholder')}
                            autoCapitalize="words"
                            value={field.value ?? ''}
                            onChangeText={field.onChange}
                            onBlur={field.onBlur}
                            error={errors.state?.message}
                          />
                        )}
                      />

                      <Button
                        title={
                          isSubmitting
                            ? t('homeSetup.saving')
                            : t('homeSetup.save')
                        }
                        onPress={handleSubmit(onSubmit)}
                        disabled={isSubmitting || isGettingLocation}
                        style={styles.primaryButton}
                        textStyle={styles.primaryButtonText}
                      />
                      <Button
                        title={t('homeSetup.goBack')}
                        onPress={() => navigation.goBack()}
                        variant="secondary"
                        style={[styles.secondaryButton, { marginTop: 12 }]}
                        textStyle={styles.secondaryButtonText}
                      />
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
