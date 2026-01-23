/**
 * DeviceRestartingOverlay Component
 * Shows a full-screen overlay when the device is restarting after grid config change.
 * Displays a countdown timer and prevents user interaction during restart period.
 */

import React, { useEffect, useState } from 'react';
import { View, Text, StyleSheet, ActivityIndicator, Modal } from 'react-native';
import { useTranslation } from 'react-i18next';
import { useThemeStore } from '../../../store/themeStore';
import { getThemeColors } from '../../../theme/constants';
import { useDeviceSettingsStore } from '../../../store/deviceSettingsStore';

interface DeviceRestartingOverlayProps {
  onRestartComplete?: () => void;
}

const DeviceRestartingOverlay: React.FC<DeviceRestartingOverlayProps> = ({
  onRestartComplete,
}) => {
  const { t } = useTranslation();
  const mode = useThemeStore((s) => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);

  const deviceRestartingInfo = useDeviceSettingsStore((state) => state.deviceRestartingInfo);
  const clearDeviceRestarting = useDeviceSettingsStore((state) => state.clearDeviceRestarting);

  const [remainingSeconds, setRemainingSeconds] = useState(0);

  // Calculate remaining time
  useEffect(() => {
    if (!deviceRestartingInfo?.isRestarting || !deviceRestartingInfo.restartUntil) {
      setRemainingSeconds(0);
      return;
    }

    const calculateRemaining = () => {
      const now = Date.now();
      const remaining = Math.max(0, Math.ceil((deviceRestartingInfo.restartUntil! - now) / 1000));
      setRemainingSeconds(remaining);

      // If restart period is over, clear the state
      if (remaining <= 0) {
        clearDeviceRestarting();
        onRestartComplete?.();
      }
    };

    // Calculate immediately
    calculateRemaining();

    // Update every second
    const interval = setInterval(calculateRemaining, 1000);

    return () => clearInterval(interval);
  }, [deviceRestartingInfo, clearDeviceRestarting, onRestartComplete]);

  // Don't render if not restarting
  if (!deviceRestartingInfo?.isRestarting) {
    return null;
  }

  // Format remaining time as MM:SS
  const formatTime = (seconds: number): string => {
    const mins = Math.floor(seconds / 60);
    const secs = seconds % 60;
    return `${mins}:${secs.toString().padStart(2, '0')}`;
  };

  // Calculate progress percentage
  const totalSeconds = deviceRestartingInfo.gracePeriodSeconds || 120;
  const elapsedSeconds = totalSeconds - remainingSeconds;
  const progressPercentage = Math.min(100, Math.round((elapsedSeconds / totalSeconds) * 100));

  return (
    <Modal
      visible={true}
      transparent={true}
      animationType="fade"
      statusBarTranslucent
    >
      <View style={[styles.overlay, { backgroundColor: 'rgba(0, 0, 0, 0.85)' }]}>
        <View style={[styles.container, { backgroundColor: COLORS.surface }]}>
          <ActivityIndicator size="large" color={COLORS.accent} style={styles.spinner} />

          <Text style={[styles.title, { color: COLORS.text }]}>
            {t('inverterModes.restarting.title', 'Device Restarting')}
          </Text>

          <Text style={[styles.description, { color: COLORS.textSoft }]}>
            {t(
              'inverterModes.restarting.description',
              'The device is restarting to apply grid configuration changes. This may take up to 2 minutes.'
            )}
          </Text>

          {deviceRestartingInfo.restartReason && (
            <Text style={[styles.reason, { color: COLORS.textSoft }]}>
              {t('inverterModes.restarting.reason', 'Reason: {{reason}}', {
                reason: deviceRestartingInfo.restartReason,
              })}
            </Text>
          )}

          <View style={styles.timerContainer}>
            <Text style={[styles.timerLabel, { color: COLORS.textSoft }]}>
              {t('inverterModes.restarting.timeRemaining', 'Time remaining')}
            </Text>
            <Text style={[styles.timer, { color: COLORS.accent }]}>
              {formatTime(remainingSeconds)}
            </Text>
          </View>

          {/* Progress bar */}
          <View style={[styles.progressBarContainer, { backgroundColor: COLORS.border }]}>
            <View
              style={[
                styles.progressBar,
                {
                  backgroundColor: COLORS.accent,
                  width: `${progressPercentage}%`,
                },
              ]}
            />
          </View>

          <Text style={[styles.warning, { color: COLORS.warning }]}>
            {t(
              'inverterModes.restarting.warning',
              'Please do not close the app. Load power may be unavailable during restart.'
            )}
          </Text>
        </View>
      </View>
    </Modal>
  );
};

const styles = StyleSheet.create({
  overlay: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    padding: 24,
  },
  container: {
    width: '100%',
    maxWidth: 400,
    borderRadius: 16,
    padding: 24,
    alignItems: 'center',
  },
  spinner: {
    marginBottom: 16,
  },
  title: {
    fontSize: 20,
    fontWeight: '700',
    marginBottom: 12,
    textAlign: 'center',
  },
  description: {
    fontSize: 14,
    lineHeight: 20,
    textAlign: 'center',
    marginBottom: 8,
  },
  reason: {
    fontSize: 12,
    fontStyle: 'italic',
    textAlign: 'center',
    marginBottom: 16,
  },
  timerContainer: {
    alignItems: 'center',
    marginBottom: 16,
  },
  timerLabel: {
    fontSize: 12,
    marginBottom: 4,
  },
  timer: {
    fontSize: 48,
    fontWeight: '700',
    fontVariant: ['tabular-nums'],
  },
  progressBarContainer: {
    width: '100%',
    height: 8,
    borderRadius: 4,
    overflow: 'hidden',
    marginBottom: 16,
  },
  progressBar: {
    height: '100%',
    borderRadius: 4,
  },
  warning: {
    fontSize: 12,
    textAlign: 'center',
    fontWeight: '500',
  },
});

export default DeviceRestartingOverlay;
