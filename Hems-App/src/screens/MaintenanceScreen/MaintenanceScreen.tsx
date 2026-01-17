import React from 'react';
import { View, Text, StyleSheet, TouchableOpacity } from 'react-native';
import { AlertCircle, RefreshCw } from 'lucide-react-native';
import { useTranslation } from 'react-i18next';
import { useThemeStore } from '../../store/themeStore';
import { getThemeColors } from '../../theme/constants';
import { useErrorStore } from '../../store/errorStore';
import type { AppError } from '../../lib/http/error';

interface MaintenanceScreenProps {
  error?: AppError;
}

export default function MaintenanceScreen({ error }: MaintenanceScreenProps) {
  const { t } = useTranslation();
  const mode = useThemeStore(s => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);
  const clearCritical = useErrorStore(s => s.clearCritical);

  const styles = StyleSheet.create({
    container: {
      flex: 1,
      backgroundColor: COLORS.dark,
      justifyContent: 'center',
      alignItems: 'center',
      paddingHorizontal: 24,
    },
    iconContainer: {
      width: 120,
      height: 120,
      borderRadius: 60,
      backgroundColor: isDark ? 'rgba(239, 68, 68, 0.1)' : 'rgba(239, 68, 68, 0.08)',
      justifyContent: 'center',
      alignItems: 'center',
      marginBottom: 32,
    },
    title: {
      fontSize: 24,
      fontWeight: '700',
      color: COLORS.textSoft,
      textAlign: 'center',
      marginBottom: 12,
      letterSpacing: -0.5,
    },
    message: {
      fontSize: 16,
      fontWeight: '500',
      color: COLORS.textMuted,
      textAlign: 'center',
      lineHeight: 24,
      maxWidth: 320,
    },
    errorCode: {
      fontSize: 13,
      fontWeight: '600',
      color: COLORS.textMuted,
      textAlign: 'center',
      marginTop: 20,
      opacity: 0.6,
    },
    retryButton: {
      flexDirection: 'row',
      alignItems: 'center',
      justifyContent: 'center',
      gap: 8,
      backgroundColor: COLORS.accent,
      paddingHorizontal: 32,
      paddingVertical: 14,
      borderRadius: 12,
      marginTop: 32,
    },
    retryButtonText: {
      fontSize: 16,
      fontWeight: '600',
      color: COLORS.white,
    },
  });

  const getErrorMessage = () => {
    if (error?.code === 'NETWORK') {
      return t('api.maintenance.networkError.message', 'Bağlantı sorunu yaşanıyor. Lütfen internet bağlantınızı kontrol edin.');
    }
    if (error?.code === 'SERVER' || (error?.status && error.status >= 500)) {
      return t('api.maintenance.serverError.message', 'Sunucularımızda şu anda bir sorun var. Lütfen daha sonra tekrar deneyin.');
    }
    return t('api.maintenance.genericError.message', 'Beklenmeyen bir hata oluştu. Lütfen daha sonra tekrar deneyin.');
  };

  const getErrorTitle = () => {
    if (error?.code === 'NETWORK') {
      return t('api.maintenance.networkError.title', 'Bağlantı Hatası');
    }
    if (error?.code === 'SERVER' || (error?.status && error.status >= 500)) {
      return t('api.maintenance.serverError.title', 'Sunucu Bakımda');
    }
    return t('api.maintenance.genericError.title', 'Bir Sorun Oluştu');
  };

  const handleRetry = () => {
    // Kritik hatayı temizle, app normal duruma dönecek
    clearCritical();
  };

  return (
    <View style={styles.container}>
      <View style={styles.iconContainer}>
        <AlertCircle size={60} color="#ef4444" strokeWidth={2} />
      </View>

      <Text style={styles.title}>{getErrorTitle()}</Text>
      <Text style={styles.message}>{getErrorMessage()}</Text>

      {!!error?.status && (
        <Text style={styles.errorCode}>
          {t('common.error', 'Hata')} {t('common.code', 'Kodu')}: {error.code} {error.status ? `• ${error.status}` : ''}
        </Text>
      )}

      <TouchableOpacity style={styles.retryButton} onPress={handleRetry}>
        <RefreshCw size={20} color={COLORS.white} strokeWidth={2.5} />
        <Text style={styles.retryButtonText}>
          {t('common.retry', 'Yeniden Dene')}
        </Text>
      </TouchableOpacity>
    </View>
  );
}
