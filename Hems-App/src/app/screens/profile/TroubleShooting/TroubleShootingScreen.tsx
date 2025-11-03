import React, { useState } from 'react';
import { ScrollView, Text, View, Pressable, LayoutAnimation, Platform, UIManager } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useThemeStore } from '../../../../store/themeStore';
import { getThemeColors } from '../../../../theme/constants';
import { makeTroubleShootingStyles } from './TroubleShootingScreen.styles';
import Input from '../../../../components/forms/Input';
import Button from '../../../../components/ui/Button';
import MainLayout from '../../../layouts/MainLayout';
import FontAwesome from '@react-native-vector-icons/fontawesome';
import { useFaultsStore } from '../../../../store/faultsStore';
import { useI18n } from '../../../../hooks/useI18n';
import { zodResolver } from '@hookform/resolvers/zod';
import { useForm, Controller } from 'react-hook-form';
import { useTroubleShootingSchema, TroubleShootingForm } from './troubleShooting.schema';

if (Platform.OS === 'android' && UIManager.setLayoutAnimationEnabledExperimental) {
  UIManager.setLayoutAnimationEnabledExperimental(true);
}

export default function TroubleShootingScreen({ navigation }: any) {
  const mode = useThemeStore(s => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);
  const styles = makeTroubleShootingStyles(COLORS, isDark);
  const insets = useSafeAreaInsets();
  const { t } = useI18n();
  const troubleShootingSchema = useTroubleShootingSchema();
  const addFault = useFaultsStore(s => s.addFault);

  const { control, handleSubmit, setValue, formState: { errors } } = useForm<TroubleShootingForm>({
    resolver: zodResolver(troubleShootingSchema),
    defaultValues: {
      faultType: '',
      description: '',
      name: '',
      phone: '',
      address: '',
      zip: '',
      email: '',
    },
  });

  const [showDropdown, setShowDropdown] = useState(false);
  const faultTypes = [
    { label: t('troubleshooting.faultTypes.alarm'), value: "alarm" },
    { label: t('troubleshooting.faultTypes.decoration'), value: "decoration" },
    { label: t('troubleshooting.faultTypes.inquiry'), value: "inquiry" },
    { label: t('troubleshooting.faultTypes.other'), value: "other" },
  ];

  const toggleDropdown = () => {
    LayoutAnimation.configureNext(LayoutAnimation.Presets.easeInEaseOut);
    setShowDropdown(!showDropdown);
  };

  const onSubmit = (data: TroubleShootingForm) => {
    addFault(data);
    navigation.navigate('Faults');
  };

  return (
    <MainLayout>
      <ScrollView
        style={styles.container}
        contentContainerStyle={{ paddingBottom: insets.bottom + 24 }}
        showsVerticalScrollIndicator={false}
      >
        <View style={styles.headerRow}>
          <Pressable style={styles.backButton} onPress={() => navigation.goBack()}>
            <FontAwesome name="chevron-left" size={18} color={COLORS.textSoft} />
          </Pressable>
          <Text style={styles.screenTitle}>{t('troubleshooting.title')}</Text>
          <View style={{ width: 32 }} />
        </View>

        <Text style={styles.title}>{t('troubleshooting.issueDetails')}</Text>

        <Controller
          control={control}
          name="faultType"
          render={({ field }) => (
            <View style={{ marginBottom: 12, position: 'relative' }}>
              <Pressable onPress={toggleDropdown}>
                <Input
                  placeholder={t('troubleshooting.faultType')}
                  placeholderTextColor={COLORS.slate400}
                  value={field.value}
                  editable={false}
                  pointerEvents="none"
                  error={errors.faultType?.message}
                />
                <FontAwesome
                  name={showDropdown ? "chevron-up" : "chevron-down"}
                  size={16}
                  color={COLORS.slate400}
                  style={{ position: 'absolute', right: 16, top: '35%' }}
                />
              </Pressable>
              {showDropdown && (
                <View style={styles.dropdownOverlay}>
                  <View style={styles.dropdownList}>
                    {faultTypes.map((item, idx) => (
                      <Pressable
                        key={idx}
                        style={styles.dropdownItem}
                        onPress={() => {
                          setValue("faultType", item.label);
                          toggleDropdown();
                        }}
                      >
                        <Text style={styles.dropdownItemText}>{item.label}</Text>
                      </Pressable>
                    ))}
                  </View>
                </View>
              )}
            </View>
          )}
        />

        <Controller
          control={control}
          name="description"
          render={({ field }) => (
            <Input
              placeholder={t('troubleshooting.description')}
              placeholderTextColor={COLORS.slate400}
              value={field.value}
              onChangeText={field.onChange}
              error={errors.description?.message}
              multiline
            />
          )}
        />

        <Text style={[styles.title, { marginTop: 24 }]}>{t('troubleshooting.contactInformation')}</Text>

        <Controller
          control={control}
          name="name"
          render={({ field }) => (
            <Input
              placeholder={t('troubleshooting.name')}
              value={field.value}
              onChangeText={field.onChange}
              error={errors.name?.message}
            />
          )}
        />

        <Controller
          control={control}
          name="phone"
          render={({ field }) => (
            <Input
              placeholder={t('troubleshooting.phone')}
              value={field.value}
              onChangeText={field.onChange}
              keyboardType="phone-pad"
              error={errors.phone?.message}
            />
          )}
        />

        <Controller
          control={control}
          name="address"
          render={({ field }) => (
            <Input
              placeholder={t('troubleshooting.address')}
              value={field.value}
              onChangeText={field.onChange}
              error={errors.address?.message}
            />
          )}
        />

        <Controller
          control={control}
          name="zip"
          render={({ field }) => (
            <Input
              placeholder={t('troubleshooting.zipCode')}
              value={field.value}
              onChangeText={field.onChange}
              keyboardType="number-pad"
              error={errors.zip?.message}
            />
          )}
        />

        <Controller
          control={control}
          name="email"
          render={({ field }) => (
            <Input
              placeholder={t('troubleshooting.email')}
              value={field.value}
              onChangeText={field.onChange}
              keyboardType="email-address"
              autoCapitalize="none"
              error={errors.email?.message}
            />
          )}
        />

        <Button
          title={t('troubleshooting.confirm')}
          style={styles.confirmBtn}
          textStyle={styles.confirmBtnText}
          onPress={handleSubmit(onSubmit)}
        />
      </ScrollView>
    </MainLayout>
  );
}
