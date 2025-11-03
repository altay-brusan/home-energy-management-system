import React, { memo } from 'react';
import { Control, Controller, FieldErrors } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { Text, View } from 'react-native';

import Input from '../../../components/forms/Input';
import Button from '../../../components/ui/Button';
import ErrorMessage from '../../../components/feedback/ErrorMessage';

import type { SignUpForm } from './signup.schema';
import type { SignUpStyles } from './SignUpScreen.styles';

type SignUpFormFieldsProps = {
  control: Control<SignUpForm>;
  errors: FieldErrors<SignUpForm>;
  isSubmitting: boolean;
  registerLoading: boolean;
  registerError: string | null;
  onSubmit: () => void;
  onGoBack: () => void;
  styles: SignUpStyles;
};

function SignUpFormFields ({
  control,
  errors,
  isSubmitting,
  registerLoading,
  registerError,
  onSubmit,
  onGoBack,
  styles,
}: SignUpFormFieldsProps) {
  const { t } = useTranslation();

  return (
    <View style={styles.formWrapper}>
      <View style={styles.form}>
        {registerError && (
          <ErrorMessage message={registerError} useToast={true} />
        )}
        <Controller
          control={control}
          name="firstName"
          render={({ field: { onChange, onBlur, value } }) => (
            <Input
              label={t('auth.firstName')}
              placeholder={t('auth.firstNamePlaceholder')}
              value={value}
              onChangeText={onChange}
              onBlur={onBlur}
              error={errors.firstName?.message}
              autoCapitalize="words"
              autoComplete="given-name"
              textContentType="givenName"
              returnKeyType="next"
            />
          )}
        />
        <Controller
          control={control}
          name="surname"
          render={({ field: { onChange, onBlur, value } }) => (
            <Input
              label={t('auth.surname')}
              placeholder={t('auth.surnamePlaceholder')}
              value={value}
              onChangeText={onChange}
              onBlur={onBlur}
              error={errors.surname?.message}
              autoCapitalize="words"
              autoComplete="family-name"
              textContentType="familyName"
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
              onChangeText={(text) => onChange(text.toLowerCase())}
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

        <View style={styles.passwordRules}>
          <Text style={styles.passwordRulesTitle}>{t('passwordRules.title')}</Text>
          <Text style={styles.passwordRule}>• {t('passwordRules.minLength')}</Text>
          <Text style={styles.passwordRule}>• {t('passwordRules.uppercase')}</Text>
          <Text style={styles.passwordRule}>• {t('passwordRules.lowercase')}</Text>
          <Text style={styles.passwordRule}>• {t('passwordRules.specialChar')}</Text>
        </View>

        <Button
          title={registerLoading ? t('auth.creatingAccount') : t('auth.createAccountButton')}
          onPress={onSubmit}
          variant="primary"
          disabled={registerLoading || isSubmitting}
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
