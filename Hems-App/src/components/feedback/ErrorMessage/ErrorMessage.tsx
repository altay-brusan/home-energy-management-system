import React, { useEffect } from 'react';
import { Text, TouchableOpacity, View } from 'react-native';
import { useTranslation } from 'react-i18next';
import { toast } from '../../../utils/toast';
import { getErrorMessageStyles } from './ErrorMessage.styles';

interface ErrorMessageProps {
  message: string;
  onRetry?: () => void;
  useToast?: boolean;
}

export default function ErrorMessage ({ message, onRetry, useToast = true }: ErrorMessageProps) {
  const styles = getErrorMessageStyles();

  const { t } = useTranslation();
  
  useEffect(() => {
    if (useToast && message) {
      toast.error(message, t('common.error'));
    }
  }, [message, useToast, t]);

  if (useToast) {
    return null;
  }

  return (
    <View style={styles.container}>
      <Text style={styles.icon}>⚠️</Text>
      <Text style={styles.message}>{message}</Text>
      {onRetry && (
        <TouchableOpacity style={styles.button} onPress={onRetry}>
          <Text style={styles.buttonText}>{t('common.retry')}</Text>
        </TouchableOpacity>
      )}
    </View>
  );
}
