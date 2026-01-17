/**
 * Update Progress Modal
 * Shows real-time progress when updating device settings via SignalR
 */

import React, { useEffect, useRef, useState } from 'react';
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
import { ParameterUpdateCompletedDto } from '../../api/types/deviceSettingsHub.types';

export interface UpdateProgressModalProps {
  visible: boolean;
  onClose: () => void;
  onContinueInBackground?: () => void;
}

export const UpdateProgressModal: React.FC<UpdateProgressModalProps> = ({
  visible,
  onClose,
  onContinueInBackground,
}) => {
  const { t } = useTranslation();
  const insets = useSafeAreaInsets();
  const mode = useThemeStore((s) => s.mode);
  const isDark = mode === 'dark';
  const tokens = getDesignTokens(isDark);
  const styles = makeUpdateProgressModalStyles(isDark);

  const { batchState, isUpdateActive } = useDeviceSettingsUpdateStore();

  // Timeout state - if stuck on "pending" for too long, allow closing
  const [isTimedOut, setIsTimedOut] = useState(false);
  const timeoutRef = useRef<NodeJS.Timeout | null>(null);

  // Timeout mechanism: if stuck on "pending" for 10 seconds, assume completion
  // This handles the race condition where SignalR events are missed
  useEffect(() => {
    if (visible && batchState && batchState.status === 'pending') {
      // Capture current values for the timeout callback
      const currentCorrelationId = batchState.correlationId;
      const currentDeviceId = batchState.deviceId || '';
      const currentTotalParams = batchState.totalParameters;

      timeoutRef.current = setTimeout(() => {
        // Get fresh state to double-check we should still auto-complete
        const freshState = useDeviceSettingsUpdateStore.getState();
        if (freshState.batchState?.status !== 'pending') {
          console.log('[UpdateProgressModal] Status changed, skipping auto-complete');
          return;
        }

        console.log('[UpdateProgressModal] ⚠️ Timeout reached - connection lost, status unknown');
        setIsTimedOut(true);

        // Mark as completed but with UNKNOWN status (not false success!)
        // successCount=0 and skippedCount=total indicates we don't know the actual result
        // This allows the user to close the modal without showing misleading "success" message
        const completionResult: ParameterUpdateCompletedDto = {
          correlationId: currentCorrelationId,
          deviceId: currentDeviceId,
          totalParameters: currentTotalParams,
          successCount: 0,  // Unknown - don't assume success
          failedCount: 0,   // Unknown - don't assume failure
          skippedCount: currentTotalParams, // All parameters have unknown status
          completedAt: new Date().toISOString(),
          duration: '00:00:10',
          wasCancelled: false,
          failedParameters: [],
        };
        freshState.completeUpdate(completionResult);
      }, 10000); // 10 second timeout
    }

    // Clear timeout if status changes from pending
    if (batchState?.status !== 'pending' && timeoutRef.current) {
      clearTimeout(timeoutRef.current);
      timeoutRef.current = null;
      setIsTimedOut(false);
    }

    return () => {
      if (timeoutRef.current) {
        clearTimeout(timeoutRef.current);
      }
    };
  }, [visible, batchState?.status, batchState?.correlationId]);

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

  // Calculate progress percentage
  const progress = batchState && batchState.totalParameters > 0
    ? (batchState.currentIndex / batchState.totalParameters) * 100
    : 0;

  // Determine completion status
  const isCompleted = batchState?.status === 'completed' || batchState?.status === 'failed';
  const hasFailures = (batchState?.failedCount ?? 0) > 0;

  // Get status icon based on current state
  const renderStatusIcon = () => {
    if (!isCompleted) {
      return <ActivityIndicator size="large" color={tokens.brand.accent} />;
    }

    // Timeout case - show warning icon
    if (isTimedOut) {
      return <AlertTriangle size={48} color={tokens.semantic.warning} />;
    }

    if (hasFailures) {
      return <AlertTriangle size={48} color={tokens.semantic.warning} />;
    }

    return <CheckCircle size={48} color={tokens.semantic.success} />;
  };

  // Get status text based on current state
  const getStatusText = (): string => {
    if (!batchState) {
      return t('deviceSettings.update.preparing', 'Preparing...');
    }

    // Timeout case - show connection timeout message
    if (isTimedOut) {
      return t('deviceSettings.update.connectionTimeout', 'Connection timeout - updates may still be processing');
    }

    switch (batchState.status) {
      case 'pending':
        return t('deviceSettings.update.connecting', 'Connecting...');
      case 'in_progress':
        return t('deviceSettings.update.inProgress', 'Updating parameters...');
      case 'completed':
        return t('deviceSettings.update.completed', 'Update completed!');
      case 'failed':
        return t('deviceSettings.update.completedWithErrors', 'Completed with errors');
      default:
        return '';
    }
  };

  // Handle close - only allow when not updating
  const handleClose = () => {
    console.log('[UpdateProgressModal] handleClose called, isUpdateActive:', isUpdateActive, 'status:', batchState?.status);
    if (!isUpdateActive) {
      console.log('[UpdateProgressModal] Calling onClose');
      onClose();
    } else {
      console.log('[UpdateProgressModal] ⚠️ Cannot close - update still active');
    }
  };

  // Handle continue in background
  const handleContinueInBackground = () => {
    onContinueInBackground?.();
  };

  return (
    <Modal
      transparent
      visible={visible}
      onRequestClose={isUpdateActive ? undefined : handleClose}
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
        {!isUpdateActive && (
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
            {t('deviceSettings.update.title', 'Updating Settings')}
          </Text>
          {!isUpdateActive && (
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

          {/* Progress Bar (shown during update) */}
          {!isCompleted && batchState && batchState.totalParameters > 0 && (
            <View style={styles.progressContainer}>
              <View style={styles.progressBar}>
                <View
                  style={[
                    styles.progressFill,
                    { width: `${Math.min(progress, 100)}%` },
                  ]}
                />
              </View>
              <Text style={styles.progressText}>
                {batchState.currentIndex} / {batchState.totalParameters}
              </Text>
            </View>
          )}

          {/* Summary (when completed) */}
          {isCompleted && batchState && (
            <View style={styles.summaryContainer}>
              {isTimedOut ? (
                // Timeout case - show warning about unknown status
                <View style={styles.summaryRow}>
                  <AlertTriangle size={16} color={tokens.semantic.warning} />
                  <Text style={styles.summaryText}>
                    {t('deviceSettings.update.statusUnknown', 'Status unknown - please check device settings')}
                  </Text>
                </View>
              ) : (
                // Normal completion - show success/failure counts
                <>
                  <View style={styles.summaryRow}>
                    <CheckCircle size={16} color={tokens.semantic.success} />
                    <Text style={styles.summaryText}>
                      {t('deviceSettings.update.successCount', '{{count}} parameters updated', {
                        count: batchState.successCount,
                      })}
                    </Text>
                  </View>
                  {hasFailures && (
                    <View style={styles.summaryRow}>
                      <XCircle size={16} color={tokens.semantic.error} />
                      <Text style={styles.summaryTextError}>
                        {t('deviceSettings.update.failedCount', '{{count}} parameters failed', {
                          count: batchState.failedCount,
                        })}
                      </Text>
                    </View>
                  )}
                </>
              )}
            </View>
          )}

          {/* Failed Parameters List */}
          {hasFailures && batchState?.failedParameters && batchState.failedParameters.length > 0 && (
            <ScrollView
              style={styles.failedListContainer}
              showsVerticalScrollIndicator={false}
            >
              <Text style={styles.failedTitle}>
                {t('deviceSettings.update.failedParameters', 'Failed Parameters:')}
              </Text>
              {batchState.failedParameters.map((param, index) => (
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
          {/* Continue in Background button (during update) */}
          {isUpdateActive && onContinueInBackground && (
            <Pressable
              style={[styles.button, styles.secondaryButton]}
              onPress={handleContinueInBackground}
            >
              <Text style={styles.secondaryButtonText}>
                {t('deviceSettings.update.continueInBackground', 'Continue in Background')}
              </Text>
            </Pressable>
          )}

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
