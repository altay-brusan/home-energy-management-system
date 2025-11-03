import React from 'react';
import { Text, View } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { Controller, useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import Input from '../../../../components/forms/Input';
import Button from '../../../../components/ui/Button';
import { useThemeStore } from '../../../../store/themeStore';
import { getThemeColors } from '../../../../theme/constants';
import { makeChangePasswordStyles } from './ProfileInfoScreen.styles';
import { useI18n } from '../../../../hooks/useI18n';
import useProfile from '../../../../hooks/useProfile';
import { ChangePasswordForm, createChangePasswordSchema } from './changePassword.schema';

export default function ChangePasswordScreen ({ navigation }: any) {
  const mode = useThemeStore(s => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);
  const styles = makeChangePasswordStyles(COLORS);
  const insets = useSafeAreaInsets();
  const { t } = useI18n();
  const { changePassword, changePasswordLoading, changePasswordSuccess, clearChangePasswordSuccess } = useProfile();

  const {
    control,
    handleSubmit,
    formState: { errors, isSubmitting },
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

  const handleCancel = () => {
    if (navigation && navigation.goBack) {
      navigation.goBack();
    }
  };

  return (
    <View
      style={[
        styles.container,
        { paddingTop: insets.top + 24, paddingBottom: 24 },
      ]}
    >
      <Text style={styles.title}>{t('changePassword.title')}</Text>
      <View style={styles.inputGroup}>
        <Controller
          control={control}
          name="oldPassword"
          render={({ field: { onChange, onBlur, value } }) => (
            <Input
              label={t('changePassword.oldPassword')}
              placeholder={t('changePassword.oldPassword')}
              placeholderTextColor={COLORS.slate400}
              value={value}
              secureTextEntry
              onChangeText={onChange}
              onBlur={onBlur}
              error={errors.oldPassword?.message}
            />
          )}
        />
        <Controller
          control={control}
          name="newPassword"
          render={({ field: { onChange, onBlur, value } }) => (
            <Input
              label={t('changePassword.newPassword')}
              placeholder={t('changePassword.newPassword')}
              placeholderTextColor={COLORS.slate400}
              value={value}
              secureTextEntry
              onChangeText={onChange}
              onBlur={onBlur}
              error={errors.newPassword?.message}
            />
          )}
        />
        <Controller
          control={control}
          name="confirmPassword"
          render={({ field: { onChange, onBlur, value } }) => (
            <Input
              label={t('changePassword.repeatPassword')}
              placeholder={t('changePassword.repeatPassword')}
              placeholderTextColor={COLORS.slate400}
              value={value}
              secureTextEntry
              onChangeText={onChange}
              onBlur={onBlur}
              error={errors.confirmPassword?.message}
            />
          )}
        />
      </View>

      <View style={styles.passwordRules}>
        <Text style={styles.passwordRulesTitle}>{t('passwordRules.title')}</Text>
        <Text style={styles.passwordRule}>• {t('passwordRules.minLength')}</Text>
        <Text style={styles.passwordRule}>• {t('passwordRules.uppercase')}</Text>
        <Text style={styles.passwordRule}>• {t('passwordRules.lowercase')}</Text>
        <Text style={styles.passwordRule}>• {t('passwordRules.specialChar')}</Text>
      </View>

      {changePasswordSuccess && (
        <View style={styles.messageBox}>
          <Text style={[styles.messageText, styles.successText]}>
            {t('changePassword.passwordUpdated')}
          </Text>
        </View>
      )}
      <View style={styles.buttonRow}>
        <Button
          title={changePasswordLoading ? t('common.loading') : t('changePassword.save')}
          style={styles.saveBtn}
          textStyle={styles.saveBtnText}
          onPress={handleSubmit(onSubmit)}
          disabled={changePasswordLoading}
        />
        <Button
          title={t('changePassword.cancel')}
          style={styles.cancelBtn}
          textStyle={styles.cancelBtnText}
          onPress={handleCancel}
        />
      </View>
    </View>
  );
}