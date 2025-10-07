import { zodResolver } from '@hookform/resolvers/zod';
import { useNavigation } from '@react-navigation/native';
import { NativeStackNavigationProp } from '@react-navigation/native-stack';
import React, { useCallback, useMemo, useState } from 'react';
import { Controller, useForm } from 'react-hook-form';
import {
  Keyboard,
  KeyboardAvoidingView,
  ScrollView,
  Text,
  TouchableWithoutFeedback,
  View,
} from 'react-native';

import { Card, ThemeToggle, CitySelector, CountrySelector } from '../../../components';
import Input from '../../../components/forms/Input';
import Button from '../../../components/ui/Button';
import { usePlatform } from '../../../hooks/usePlatform';
import MainLayout from '../../layouts/MainLayout';
import { useThemeStore } from '../../../store/themeStore';
import { getThemeColors } from '../../../theme/constants';
import { getOnboardingStyles } from './Onboarding.styles';
import { homeSetupSchema, type HomeSetupForm } from './home.schema';
import { useOnboardingStore } from '../../../store/onboardingStore';
import type { City } from '../../../components/ui/CitySelector/CitySelector';
import type { Country } from '../../../components/ui/CountrySelector/CountrySelector';

type RootStackParamList = {
  Home: undefined;
  HomeSetup: undefined;
  DeviceSetup: undefined;
  MainTabs: undefined;
};

export default function HomeSetupScreen() {
  const navigation =
    useNavigation<NativeStackNavigationProp<RootStackParamList>>();
  const { setHome } = useOnboardingStore();
  const { isIOS } = usePlatform();
  const mode = useThemeStore(s => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);
  const styles = useMemo(
    () => getOnboardingStyles(COLORS, isDark),
    [COLORS, isDark],
  );

  const [selectedCity, setSelectedCity] = useState<City | null>(null);
  const [selectedCountry, setSelectedCountry] = useState<Country | null>(null);

  const {
    control,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<HomeSetupForm>({
    resolver: zodResolver(homeSetupSchema),
    defaultValues: {
      homeName: '',
      addressLine1: '',
      addressLine2: '',
      city: '',
      state: '',
      postalCode: '',
      country: '',
    },
  });

  const onSubmit = useCallback(
    (values: HomeSetupForm) => {
      setHome({
        name: values.homeName,
        address: {
          line1: values.addressLine1,
          line2: values.addressLine2,
          city: selectedCity?.name || values.city,
          state: values.state,
          postalCode: values.postalCode,
          country: selectedCountry?.name || values.country || 'Turkey',
        },
      });
      navigation.navigate('Home');
    },
    [navigation, setHome, selectedCity, selectedCountry],
  );

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
                    <Text style={styles.title}>Create your home</Text>
                    <Text style={styles.subtitle}>
                      Tell us how we should refer to this location and where
                      it's situated so we can link your devices correctly.
                    </Text>

                    <View style={styles.formWrapper}>
                      <Controller
                        control={control}
                        name="homeName"
                        render={({ field: { onChange, value, onBlur } }) => (
                          <Input
                            label="Home name"
                            placeholder="e.g. Istanbul Apartment"
                            autoCapitalize="words"
                            value={value}
                            onChangeText={onChange}
                            onBlur={onBlur}
                            error={errors.homeName?.message}
                            returnKeyType="next"
                          />
                        )}
                      />
                      <Controller
                        control={control}
                        name="addressLine1"
                        render={({ field: { onChange, value, onBlur } }) => (
                          <Input
                            label="Street address"
                            placeholder="Street and number"
                            autoCapitalize="words"
                            value={value}
                            onChangeText={onChange}
                            onBlur={onBlur}
                            error={errors.addressLine1?.message}
                            returnKeyType="next"
                          />
                        )}
                      />
                      <Controller
                        control={control}
                        name="addressLine2"
                        render={({ field: { onChange, value, onBlur } }) => (
                          <Input
                            label="Apartment, suite (optional)"
                            placeholder="Floor, unit, etc."
                            autoCapitalize="words"
                            value={value ?? ''}
                            onChangeText={onChange}
                            onBlur={onBlur}
                            error={errors.addressLine2?.message}
                            returnKeyType="next"
                          />
                        )}
                      />

                      <View style={styles.dualRow}>
                        <View style={styles.grow}>
                          <CountrySelector
                            selectedCountry={selectedCountry}
                            onCountrySelect={setSelectedCountry}
                            error={errors.country?.message}
                            placeholder="Select Country"
                            label="Country"
                          />
                        </View>
                        <View style={styles.grow}>
                          <CitySelector
                            selectedCity={selectedCity}
                            onCitySelect={setSelectedCity}
                            error={errors.city?.message}
                            placeholder="Select City"
                            label="City"
                          />
                        </View>
                      </View>

                      <Controller
                        control={control}
                        name="state"
                        render={({ field }) => (
                          <Input
                            label="State / Province"
                            placeholder="State or province"
                            autoCapitalize="words"
                            value={field.value ?? ''}
                            onChangeText={field.onChange}
                            onBlur={field.onBlur}
                            error={errors.state?.message}
                          />
                        )}
                      />

                      <Controller
                        control={control}
                        name="postalCode"
                        render={({ field }) => (
                          <Input
                            label="Postal code"
                            placeholder="Postal / ZIP"
                            autoCapitalize="characters"
                            value={field.value}
                            onChangeText={field.onChange}
                            onBlur={field.onBlur}
                            error={errors.postalCode?.message}
                          />
                        )}
                      />

                      <Button
                        title={isSubmitting ? 'Saving…' : 'Save'}
                        onPress={handleSubmit(onSubmit)}
                        disabled={isSubmitting}
                        style={styles.primaryButton}
                        textStyle={styles.primaryButtonText}
                      />
                      <Button
                        title="Go Back"
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
