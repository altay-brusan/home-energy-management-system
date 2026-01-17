/**
 * Update Progress Modal (Simplified)
 * Shows loading state during save and result dialog if there are failures
 * Now uses synchronous REST API - no SignalR progress tracking needed
 */

import React, { useEffect, useRef } from 'react';
import {
  View,
  Text,
  Modal,
  Pressable,
  Animated,
  ScrollView,
  ActivityIndicator,
} from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { CheckCircle, XCircle, AlertTriangle, X } from 'lucide-react-native';
import { useTranslation } from 'react-i18next';
import { useThemeStore } from '../../store/themeStore';
import { getDesignTokens } from '../../theme/design-tokens';
import { useDeviceSettingsUpdateStore } from '../../store/deviceSettingsUpdateStore';
import { makeUpdateProgressModalStyles } from './UpdateProgressModal.styles';

export interface UpdateProgressModalProps {
  visible: boolean;
  onClose: () => void;
}

export const UpdateProgressModal: React.FC<UpdateProgressModalProps> = ({
  visible,
  onClose,
}) => {
  const { t } = useTranslation();
  const insets = useSafeAreaInsets();
  const mode = useThemeStore((s) => s.mode);
  const isDark = mode === 'dark';
  const tokens = getDesignTokens(isDark);
  const styles = makeUpdateProgressModalStyles(isDark);

  // Get simplified store state
  const { isSaving, lastResult, hideResultDialog } = useDeviceSettingsUpdateStore();

  // Animation values
  const sheetY = useRef(new Animated.Value(400)).current;
  const overlayOpacity = useRef(new Animated.Value(0)).current;

  // Animate modal in/out
  useEffect(() => {
    if (visible) {
      Animated.parallel([
        Animated.spring(sheetY, {
          toValue: 0,
          useNativeDriver: true,
          bounciness: 5,
          speed: 14,
        }),
        Animated.timing(overlayOpacity, {
          toValue: 1,
          duration: 200,
          useNativeDriver: true,
        }),
      ]).start();
    } else {
      Animated.parallel([
        Animated.timing(sheetY, {
          toValue: 400,
          duration: 200,
          useNativeDriver: true,
        }),
        Animated.timing(overlayOpacity, {
          toValue: 0,
          duration: 150,
          useNativeDriver: true,
        }),
      ]).start();
    }
  }, [visible, sheetY, overlayOpacity]);

  // Determine state
  const isCompleted = !isSaving && lastResult !== null;
  const hasFailures = (lastResult?.failedCount ?? 0) > 0;
  const hasSuccesses = (lastResult?.successCount ?? 0) > 0;

  // Get status icon based on current state
  const renderStatusIcon = () => {
    if (isSaving) {
      return <ActivityIndicator size="large" color={tokens.brand.accent} />;
    }

    if (hasFailures) {
      return <AlertTriangle size={48} color={tokens.semantic.warning} />;
    }

    if (hasSuccesses) {
      return <CheckCircle size={48} color={tokens.semantic.success} />;
    }

    return <XCircle size={48} color={tokens.semantic.error} />;
  };

  // Get status text based on current state
  const getStatusText = (): string => {
    if (isSaving) {
      return t('deviceSettings.update.inProgress', 'Updating parameters...');
    }

    if (!lastResult) {
      return '';
    }

    if (lastResult.errorMessage === 'Device busy') {
      return t('deviceSettings.update.deviceBusy', 'Device is busy');
    }

    if (hasFailures && hasSuccesses) {
      return t('deviceSettings.update.completedWithErrors', 'Completed with errors');
    }

    if (hasFailures && !hasSuccesses) {
      return t('deviceSettings.update.failed', 'Update failed');
    }

    return t('deviceSettings.update.completed', 'Update completed!');
  };

  // Handle close
  const handleClose = () => {
    if (!isSaving) {
      hideResultDialog();
      onClose();
    }
  };

  return (
    <Modal
      transparent
      visible={visible}
      onRequestClose={isSaving ? undefined : handleClose}
      statusBarTranslucent
      animationType="none"
    >
      {/* Overlay */}
      <Animated.View
        style={[
          styles.overlay,
          { opacity: overlayOpacity },
        ]}
      >
        {/* Allow tap to dismiss only when completed */}
        {!isSaving && (
          <Pressable style={{ flex: 1 }} onPress={handleClose} />
        )}
      </Animated.View>

      {/* Modal Content */}
      <Animated.View
        style={[
          styles.modalContent,
          {
            transform: [{ translateY: sheetY }],
            paddingBottom: insets.bottom + 16,
          },
        ]}
      >
        {/* Handle */}
        <View style={styles.handleContainer}>
          <View style={styles.handle} />
        </View>

        {/* Header */}
        <View style={styles.header}>
          <Text style={styles.title}>
            {isSaving
              ? t('deviceSettings.update.title', 'Updating Settings')
              : t('deviceSettings.update.resultTitle', 'Update Result')}
          </Text>
          {!isSaving && (
            <Pressable onPress={handleClose} style={styles.closeButton}>
              <X size={24} color={tokens.text.secondary} />
            </Pressable>
          )}
        </View>

        {/* Content */}
        <View style={styles.content}>
          {/* Status Icon */}
          <View style={styles.statusIconContainer}>
            {renderStatusIcon()}
          </View>

          {/* Status Text */}
          <Text style={styles.statusText}>
            {getStatusText()}
          </Text>

          {/* Summary (when completed) */}
          {isCompleted && lastResult && (
            <View style={styles.summaryContainer}>
              {lastResult.successCount > 0 && (
                <View style={styles.summaryRow}>
                  <CheckCircle size={16} color={tokens.semantic.success} />
                  <Text style={styles.summaryText}>
                    {lastResult.successCount === 1
                      ? t('deviceSettings.update.successCountSingular', '1 parameter updated')
                      : t('deviceSettings.update.successCount', '{{count}} parameters updated', {
                          count: lastResult.successCount,
                        })}
                  </Text>
                </View>
              )}
              {hasFailures && (
                <View style={styles.summaryRow}>
                  <XCircle size={16} color={tokens.semantic.error} />
                  <Text style={styles.summaryTextError}>
                    {lastResult.failedCount === 1
                      ? t('deviceSettings.update.failedCountSingular', '1 parameter failed')
                      : t('deviceSettings.update.failedCount', '{{count}} parameters failed', {
                          count: lastResult.failedCount,
                        })}
                  </Text>
                </View>
              )}
            </View>
          )}

          {/* Error Message (when no specific failed parameters) */}
          {isCompleted && lastResult && !hasFailures && !hasSuccesses && lastResult.errorMessage && (
            <View style={styles.errorContainer}>
              <Text style={styles.errorMessage}>
                {lastResult.errorMessage}
              </Text>
            </View>
          )}

          {/* Failed Parameters List */}
          {hasFailures && lastResult?.failedParameters && lastResult.failedParameters.length > 0 && (
            <ScrollView
              style={styles.failedListContainer}
              showsVerticalScrollIndicator={false}
            >
              <Text style={styles.failedTitle}>
                {t('deviceSettings.update.failedParameters', 'Failed Parameters:')}
              </Text>
              {lastResult.failedParameters.map((param, index) => (
                <View key={`${param.parameterId}-${index}`} style={styles.failedItem}>
                  <Text style={styles.failedParamName}>
                    {param.parameterName}
                  </Text>
                  <Text style={styles.failedParamError}>
                    {param.errorMessage}
                  </Text>
                </View>
              ))}
            </ScrollView>
          )}
        </View>

        {/* Actions */}
        <View style={styles.actionsContainer}>
          {/* Close button (when completed) */}
          {isCompleted && (
            <Pressable
              style={[styles.button, styles.primaryButton]}
              onPress={handleClose}
            >
              <Text style={styles.primaryButtonText}>
                {t('common.close', 'Close')}
              </Text>
            </Pressable>
          )}
        </View>
      </Animated.View>
    </Modal>
  );
};

export default UpdateProgressModal;
