import React from 'react';
import { Text, View, ScrollView, TextInput, TouchableOpacity, Pressable } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { Controller, useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { ArrowLeft } from 'lucide-react-native';
import { useThemeStore } from '../../../../store/themeStore';
import { getThemeColors } from '../../../../theme/constants';
import { makeChangePasswordStyles } from './ProfileInfoScreen.styles';
import { useI18n } from '../../../../hooks/useI18n';
import useProfile from '../../../../hooks/useProfile';
import { ChangePasswordForm, createChangePasswordSchema } from './changePassword.schema';

export default function ChangePasswordScreen({ navigation }: any) {
  const mode = useThemeStore(s => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);
  const styles = makeChangePasswordStyles(COLORS, isDark);
  const insets = useSafeAreaInsets();
  const { t } = useI18n();
  const { changePassword, changePasswordLoading, changePasswordSuccess, clearChangePasswordSuccess } = useProfile();

  const {
    control,
    handleSubmit,
    formState: { errors },
    reset,
  } = useForm<ChangePasswordForm>({
    resolver: zodResolver(createChangePasswordSchema(t)),
    defaultValues: { oldPassword: '', newPassword: '', confirmPassword: '' },
  });

  const onSubmit = async (values: ChangePasswordForm) => {
    try {
      await changePassword({
        oldPassword: values.oldPassword,
        newPassword: values.newPassword,
      });
      reset();
      clearChangePasswordSuccess();
    } catch (error) {}
  };

  return (
    <View style={[styles.container, { paddingTop: insets.top }]}>
      {/* Header */}
      <View style={styles.header}>
        <TouchableOpacity onPress={() => navigation.goBack()} style={styles.backButton}>
          <ArrowLeft size={24} color={COLORS.text} />
        </TouchableOpacity>
        <Text style={styles.headerTitle}>{t('changePassword.title')}</Text>
        <View style={{ width: 40 }} />
      </View>

      <ScrollView
        contentContainerStyle={[styles.scrollContent, { paddingBottom: insets.bottom + 100 }]}
        showsVerticalScrollIndicator={false}
      >
        {/* Password Fields Section */}
        <View style={styles.section}>
          <Text style={styles.sectionTitle}>{t('changePassword.securitySettings')}</Text>

          <View style={styles.inputRow}>
            <Text style={styles.inputLabel}>{t('changePassword.oldPassword')}</Text>
            <Controller
              control={control}
              name="oldPassword"
              render={({ field: { onChange, onBlur, value } }) => (
                <TextInput
                  style={[styles.inputField, errors.oldPassword && { borderColor: COLORS.error }]}
                  value={value}
                  onChangeText={onChange}
                  onBlur={onBlur}
                  secureTextEntry
                  placeholder="********"
                  placeholderTextColor={COLORS.textSoft}
                />
              )}
            />
            {errors.oldPassword && (
              <Text style={[styles.errorText, { fontSize: 12, marginTop: 4 }]}>
                {errors.oldPassword.message}
              </Text>
            )}
          </View>

          <View style={styles.inputRow}>
            <Text style={styles.inputLabel}>{t('changePassword.newPassword')}</Text>
            <Controller
              control={control}
              name="newPassword"
              render={({ field: { onChange, onBlur, value } }) => (
                <TextInput
                  style={[styles.inputField, errors.newPassword && { borderColor: COLORS.error }]}
                  value={value}
                  onChangeText={onChange}
                  onBlur={onBlur}
                  secureTextEntry
                  placeholder="********"
                  placeholderTextColor={COLORS.textSoft}
                />
              )}
            />
            {errors.newPassword && (
              <Text style={[styles.errorText, { fontSize: 12, marginTop: 4 }]}>
                {errors.newPassword.message}
              </Text>
            )}
          </View>

          <View style={styles.inputRow}>
            <Text style={styles.inputLabel}>{t('changePassword.repeatPassword')}</Text>
            <Controller
              control={control}
              name="confirmPassword"
              render={({ field: { onChange, onBlur, value } }) => (
                <TextInput
                  style={[styles.inputField, errors.confirmPassword && { borderColor: COLORS.error }]}
                  value={value}
                  onChangeText={onChange}
                  onBlur={onBlur}
                  secureTextEntry
                  placeholder="********"
                  placeholderTextColor={COLORS.textSoft}
                />
              )}
            />
            {errors.confirmPassword && (
              <Text style={[styles.errorText, { fontSize: 12, marginTop: 4 }]}>
                {errors.confirmPassword.message}
              </Text>
            )}
          </View>
        </View>

        {/* Password Rules Card */}
        <View style={styles.passwordRulesCard}>
          <Text style={styles.passwordRulesTitle}>{t('passwordRules.title')}</Text>
          <Text style={styles.passwordRule}>• {t('passwordRules.minLength')}</Text>
          <Text style={styles.passwordRule}>• {t('passwordRules.uppercase')}</Text>
          <Text style={styles.passwordRule}>• {t('passwordRules.lowercase')}</Text>
          <Text style={styles.passwordRule}>• {t('passwordRules.specialChar')}</Text>
        </View>

        {/* Success Message */}
        {changePasswordSuccess && (
          <View style={[styles.messageBox, styles.successBox]}>
            <Text style={[styles.messageText, styles.successText]}>
              {t('changePassword.passwordUpdated')}
            </Text>
          </View>
        )}
      </ScrollView>

      {/* Bottom Buttons */}
      <View style={[styles.buttonContainer, { paddingBottom: insets.bottom + 16 }]}>
        <Pressable
          style={[styles.saveButton, changePasswordLoading && { opacity: 0.6 }]}
          onPress={handleSubmit(onSubmit)}
          disabled={changePasswordLoading}
        >
          <Text style={styles.saveButtonText}>
            {changePasswordLoading ? t('common.loading') : t('changePassword.save')}
          </Text>
        </Pressable>
        <Pressable style={styles.cancelButton} onPress={() => navigation.goBack()}>
          <Text style={styles.cancelButtonText}>{t('changePassword.cancel')}</Text>
        </Pressable>
      </View>
    </View>
  );
}
