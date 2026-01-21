import React, { useState, useCallback } from 'react';
import {
  View,
  Text,
  ScrollView,
  KeyboardAvoidingView,
  TouchableWithoutFeedback,
  Keyboard,
  Platform,
  Alert,
  SafeAreaView,
} from 'react-native';
import MaterialIcons from '@react-native-vector-icons/material-icons';
import { useNavigation } from '@react-navigation/native';
import { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { useTranslation } from 'react-i18next';
import { zodResolver } from '@hookform/resolvers/zod';
import { Controller, useForm } from 'react-hook-form';
import { z } from 'zod';
import { useSafeAreaInsets } from 'react-native-safe-area-context';

import { Card, Button, BackButton, TextScannerModal } from '../../../components';
import Input from '../../../components/forms/Input';
import { useThemeStore } from '../../../store/themeStore';
import { useSitesStore } from '../../../store/sitesStore';
import { setupsService } from '../../../api/services/setupsService';
import { getThemeColors, SPACING } from '../../../theme/constants';
import { showToast } from '../../../utils/toast';
import { getDeviceAddScreenStyles } from './DeviceAddScreen.styles';

type RootStackParamList = {
  MainTabs: undefined;
  DeviceAdd: { siteId: string };
};

const deviceAddSchema = z.object({
  setupName: z
    .string()
    .min(2, 'Setup name must be at least 2 characters')
    .max(64, 'Setup name is too long'),
  deviceSerialNumber: z
    .string()
    .min(6, 'Serial number must be at least 6 characters')
    .max(40, 'Serial number is too long'),
});

type DeviceAddForm = z.infer<typeof deviceAddSchema>;

export default function DeviceAddScreen() {
  const { t } = useTranslation();
  const navigation = useNavigation<NativeStackNavigationProp<RootStackParamList>>();
  const insets = useSafeAreaInsets();
  const { sites, getUserSites, loadSelectedSiteDevice, selectedSite } = useSitesStore();
  
  const [isTextScannerVisible, setIsTextScannerVisible] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const mode = useThemeStore(s => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);

  const currentSite = sites.find(site => site.id === selectedSite?.id);

  const {
    control,
    handleSubmit,
    setValue,
    watch,
    formState: { errors },
  } = useForm<DeviceAddForm>({
    resolver: zodResolver(deviceAddSchema),
    defaultValues: {
      setupName: currentSite?.name || '',
      deviceSerialNumber: '',
    },
  });

  const watchedSerialNumber = watch('deviceSerialNumber');

  const handleTextScan = useCallback(
    (serialNumber: string) => {
      setValue('deviceSerialNumber', serialNumber);
      setIsTextScannerVisible(false);
    },
    [setValue],
  );

  const onSubmit = useCallback(
    async (values: DeviceAddForm) => {
      setIsSubmitting(true);
      try {
        await setupsService.create({
          siteId: selectedSite?.id!,
          name: values.setupName,
          deviceQRCode: null,
          deviceSerialNumber: values.deviceSerialNumber,
        });

        // Refresh sites to update hasDevice status
        await getUserSites();
        
        // Load device information for the current site
        await loadSelectedSiteDevice();

        showToast({
          type: 'success',
          title: t('deviceAdd.success.title'),
          message: t('deviceAdd.success.message'),
          duration: 3000,
        });

        navigation.goBack();
      } catch (error: any) {showToast({
          type: 'error',
          title: t('deviceAdd.error.title'),
          message: error.message || t('deviceAdd.error.message'),
          duration: 5000,
        });
      } finally {
        setIsSubmitting(false);
      }
    },
    [selectedSite?.id, getUserSites, loadSelectedSiteDevice, navigation, t],
  );

  const styles = getDeviceAddScreenStyles(COLORS);

  return (
    <SafeAreaView style={[styles.container, { backgroundColor: COLORS.dark }]}>
      <KeyboardAvoidingView
        style={{ flex: 1 }}
        behavior={Platform.OS === 'ios' ? 'padding' : 'height'}
      >
        <TouchableWithoutFeedback onPress={Keyboard.dismiss}>
          <ScrollView
            style={{ flex: 1 }}
            contentContainerStyle={[
              styles.scrollContainer,
              { paddingTop: insets.top + SPACING.md }
            ]}
            showsVerticalScrollIndicator={false}
          >
            <Card variant="elevated" style={styles.mainCard}>
              {/* Header */}
              <View style={styles.header}>
                <View style={styles.headerContent}>
                  <Text style={styles.title}>{t('deviceAdd.title')}</Text>
                  <Text style={styles.subtitle}>{t('deviceAdd.subtitle')}</Text>
                </View>
              </View>

            {/* Site Info */}
            {currentSite && (
              <View style={styles.siteInfo}>
                <View style={styles.siteHeader}>
                  <View style={styles.siteIcon}>
                    <MaterialIcons name="home" size={18} color={COLORS.accent} />
                  </View>
                  <Text style={styles.siteName}>{currentSite.name}</Text>
                </View>
                <Text style={styles.siteAddress}>
                  {currentSite.city && currentSite.country 
                    ? `${currentSite.city}, ${currentSite.country}`
                    : t('deviceAdd.noAddress')
                  }
                </Text>
              </View>
            )}

            <View style={styles.formWrapper}>
              <Controller
                control={control}
                name="setupName"
                render={({ field }) => (
                  <Input
                    label={t('deviceAdd.setupName')}
                    placeholder={t('deviceAdd.setupNamePlaceholder')}
                    autoCapitalize="words"
                    value={field.value}
                    onChangeText={field.onChange}
                    onBlur={field.onBlur}
                    error={errors.setupName?.message}
                  />
                )}
              />

              <Controller
                control={control}
                name="deviceSerialNumber"
                render={({ field }) => (
                  <Input
                    label={t('deviceAdd.serialNumber')}
                    placeholder={t('deviceAdd.serialNumberPlaceholder')}
                    autoCapitalize="characters"
                    autoCorrect={false}
                    value={field.value}
                    onChangeText={field.onChange}
                    onBlur={field.onBlur}
                    error={errors.deviceSerialNumber?.message}
                  />
                )}
              />

              <Button
                title={t('textScanner.scanButton')}
                variant="secondary"
                onPress={() => setIsTextScannerVisible(true)}
                leftIcon={<MaterialIcons name="document-scanner" size={20} color="inherit" />}
              />
            </View>

            <View style={styles.buttonContainer}>
              <Button
                title={isSubmitting ? t('common.loading') : t('deviceAdd.addDevice')}
                onPress={handleSubmit(onSubmit)}
                disabled={isSubmitting || !watchedSerialNumber}
              />
              
              <Button
                title={t('common.back') || 'İptal'}
                variant="secondary"
                onPress={() => navigation.goBack()}
                disabled={isSubmitting}
              />
            </View>
          </Card>
        </ScrollView>
      </TouchableWithoutFeedback>
      </KeyboardAvoidingView>

      <TextScannerModal
        visible={isTextScannerVisible}
        onClose={() => setIsTextScannerVisible(false)}
        onTextSelected={handleTextScan}
      />
    </SafeAreaView>
  );
}
