/**
 * GridConfigConfirmDialog Component
 * Shows a confirmation dialog when user tries to save grid config changes.
 * Warns about device restart and load power interruption.
 */

import React from 'react';
import { View, Text, StyleSheet, Modal, TouchableOpacity, ActivityIndicator } from 'react-native';
import { useTranslation } from 'react-i18next';
import { useThemeStore } from '../../../store/themeStore';
import { getThemeColors } from '../../../theme/constants';
import { AlertTriangle, ZapOff, RefreshCw } from 'lucide-react-native';

interface GridConfigConfirmDialogProps {
  visible: boolean;
  onConfirm: () => void;
  onCancel: () => void;
  isLoading?: boolean;
}

const GridConfigConfirmDialog: React.FC<GridConfigConfirmDialogProps> = ({
  visible,
  onConfirm,
  onCancel,
  isLoading = false,
}) => {
  const { t } = useTranslation();
  const mode = useThemeStore((s) => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);

  return (
    <Modal
      visible={visible}
      transparent={true}
      animationType="fade"
      onRequestClose={isLoading ? undefined : onCancel}
      statusBarTranslucent
    >
      <View style={[styles.overlay, { backgroundColor: 'rgba(0, 0, 0, 0.6)' }]}>
        <View style={[styles.dialog, { backgroundColor: COLORS.surface }]}>
          {/* Warning Icon */}
          <View style={[styles.iconContainer, { backgroundColor: COLORS.warningLight }]}>
            <AlertTriangle size={32} color={COLORS.warning} />
          </View>

          {/* Title */}
          <Text style={[styles.title, { color: COLORS.text }]}>
            {t('inverterModes.gridConfigConfirm.title', 'Device Will Restart')}
          </Text>

          {/* Description */}
          <Text style={[styles.description, { color: COLORS.text, opacity: 0.8 }]}>
            {t(
              'inverterModes.gridConfigConfirm.description',
              'Changing grid configuration settings will cause the device to restart. This process takes approximately 2 minutes.'
            )}
          </Text>

          {/* Warning Box */}
          <View style={[styles.warningBox, { backgroundColor: COLORS.warningLight }]}>
            <ZapOff size={20} color={COLORS.warning} style={styles.warningIcon} />
            <Text style={[styles.warningText, { color: COLORS.warning }]}>
              {t(
                'inverterModes.gridConfigConfirm.powerWarning',
                'Load power will be temporarily unavailable during the restart.'
              )}
            </Text>
          </View>

          {/* Buttons */}
          <View style={styles.buttonContainer}>
            <TouchableOpacity
              style={[
                styles.button,
                styles.cancelButton,
                { borderColor: COLORS.border },
                isLoading && styles.buttonDisabled,
              ]}
              onPress={onCancel}
              disabled={isLoading}
            >
              <Text style={[styles.cancelButtonText, { color: COLORS.text }, isLoading && { opacity: 0.5 }]}>
                {t('common.cancel', 'Cancel')}
              </Text>
            </TouchableOpacity>

            <TouchableOpacity
              style={[
                styles.button,
                styles.confirmButton,
                { backgroundColor: COLORS.warning },
                isLoading && styles.buttonDisabled,
              ]}
              onPress={onConfirm}
              disabled={isLoading}
            >
              {isLoading ? (
                <View style={styles.loadingContainer}>
                  <ActivityIndicator size="small" color="#FFFFFF" style={styles.buttonIcon} />
                  <Text style={[styles.confirmButtonText, { color: '#FFFFFF' }]}>
                    {t('common.saving', 'Saving...')}
                  </Text>
                </View>
              ) : (
                <>
                  <RefreshCw size={16} color="#FFFFFF" style={styles.buttonIcon} />
                  <Text style={[styles.confirmButtonText, { color: '#FFFFFF' }]}>
                    {t('inverterModes.gridConfigConfirm.confirmButton', 'Confirm & Restart')}
                  </Text>
                </>
              )}
            </TouchableOpacity>
          </View>
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
  dialog: {
    width: '100%',
    maxWidth: 360,
    borderRadius: 16,
    padding: 24,
    alignItems: 'center',
  },
  iconContainer: {
    width: 64,
    height: 64,
    borderRadius: 32,
    justifyContent: 'center',
    alignItems: 'center',
    marginBottom: 16,
  },
  title: {
    fontSize: 18,
    fontWeight: '700',
    marginBottom: 12,
    textAlign: 'center',
  },
  description: {
    fontSize: 14,
    lineHeight: 20,
    textAlign: 'center',
    marginBottom: 16,
  },
  warningBox: {
    flexDirection: 'row',
    alignItems: 'center',
    padding: 12,
    borderRadius: 8,
    marginBottom: 20,
    width: '100%',
  },
  warningIcon: {
    marginRight: 8,
  },
  warningText: {
    flex: 1,
    fontSize: 13,
    fontWeight: '500',
  },
  buttonContainer: {
    flexDirection: 'row',
    width: '100%',
    gap: 12,
  },
  button: {
    flex: 1,
    height: 48,
    borderRadius: 8,
    justifyContent: 'center',
    alignItems: 'center',
    flexDirection: 'row',
  },
  cancelButton: {
    borderWidth: 1,
  },
  confirmButton: {},
  buttonDisabled: {
    opacity: 0.6,
  },
  loadingContainer: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  buttonIcon: {
    marginRight: 6,
  },
  cancelButtonText: {
    fontSize: 15,
    fontWeight: '600',
  },
  confirmButtonText: {
    fontSize: 15,
    fontWeight: '600',
  },
});

export default GridConfigConfirmDialog;
