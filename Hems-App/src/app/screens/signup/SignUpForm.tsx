import React, { memo } from 'react';
import { View } from 'react-native';
import { Control, Controller, FieldErrors } from 'react-hook-form';
import { useTranslation } from 'react-i18next';

import Input from '../../../components/forms/Input';
import Button from '../../../components/ui/Button';

import type { SignUpForm } from './signup.schema';
import type { SignUpStyles } from './SignUpScreen.styles';

type SignUpFormFieldsProps = {
  control: Control<SignUpForm>;
  errors: FieldErrors<SignUpForm>;
  isSubmitting: boolean;
  onSubmit: () => void;
  onGoBack: () => void;
  styles: SignUpStyles;
};

function SignUpFormFields({
  control,
  errors,
  isSubmitting,
  onSubmit,
  onGoBack,
  styles,
}: SignUpFormFieldsProps) {
  const { t } = useTranslation();
  
  return (
    <View style={styles.formWrapper}>
      <View style={styles.form}>
        <Controller
          control={control}
          name="name"
          render={({ field: { onChange, onBlur, value } }) => (
            <Input
              label={t('auth.name')}
              placeholder={t('auth.namePlaceholder')}
              value={value}
              onChangeText={onChange}
              onBlur={onBlur}
              error={errors.name?.message}
              autoCapitalize="words"
              autoComplete="name"
              textContentType="name"
              returnKeyType="next"
            />
          )}
        />
        <Controller
          control={control}
          name="email"
          render={({ field: { onChange, onBlur, value } }) => (
            <Input
              label={t('auth.email')}
              placeholder={t('auth.emailPlaceholder')}
              keyboardType="email-address"
              autoCapitalize="none"
              autoCorrect={false}
              autoComplete="email"
              textContentType="emailAddress"
              value={value}
              onChangeText={onChange}
              onBlur={onBlur}
              error={errors.email?.message}
              returnKeyType="next"
            />
          )}
        />
        <Controller
          control={control}
          name="password"
          render={({ field: { onChange, onBlur, value } }) => (
            <Input
              label={t('auth.password')}
              placeholder={t('auth.passwordPlaceholder')}
              secureTextEntry
              autoCorrect={false}
              autoComplete="password"
              textContentType="newPassword"
              value={value}
              onChangeText={onChange}
              onBlur={onBlur}
              error={errors.password?.message}
              returnKeyType="next"
            />
          )}
        />
        <Controller
          control={control}
          name="confirmPassword"
          render={({ field: { onChange, onBlur, value } }) => (
            <Input
              label={t('auth.confirmPassword')}
              placeholder={t('auth.confirmPasswordPlaceholder')}
              secureTextEntry
              autoCorrect={false}
              autoComplete="password"
              textContentType="password"
              value={value}
              onChangeText={onChange}
              onBlur={onBlur}
              error={errors.confirmPassword?.message}
              returnKeyType="done"
              onSubmitEditing={onSubmit}
            />
          )}
        />
        <Button
          title={isSubmitting ? t('auth.creatingAccount') : t('auth.createAccountButton')}
          onPress={onSubmit}
          variant="primary"
          disabled={isSubmitting}
          style={styles.primaryButton}
        />
        <Button
          title={t('auth.backToLogin')}
          onPress={onGoBack}
          variant="secondary"
          style={styles.secondaryButton}
        />
      </View>
    </View>
  );
}

export default memo(SignUpFormFields);
