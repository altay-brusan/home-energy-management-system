import React, { useState } from 'react';
import { View, Text, Modal, TouchableOpacity, ActivityIndicator } from 'react-native';
import { Modal as RNModal } from 'react-native';
import { useI18n } from '../../hooks/useI18n';
// import { Button } from '../ui';
import { makeInvitationModalStyles } from './InvitationModal.styles';

export interface InvitationModalProps {
  visible: boolean;
  invitationCode: string;
  onAccept: () => Promise<void>;
  onDecline: () => void;
}

export function InvitationModal({
  visible,
  invitationCode,
  onAccept,
  onDecline,
}: InvitationModalProps) {
  const { t } = useI18n();
  const styles = makeInvitationModalStyles();
  const [loading, setLoading] = useState(false);

  const handleAccept = async () => {
    try {
      setLoading(true);
      await onAccept();
    } catch (error) {} finally {
      setLoading(false);
    }
  };

  return (
    <RNModal
      visible={visible}
      transparent
      animationType="fade"
      onRequestClose={onDecline}
    >
      <View style={styles.overlay}>
        <View style={styles.container}>
          <Text style={styles.title}>{t('invitation.title')}</Text>
          <Text style={styles.message}>
            {t('invitation.message')}
          </Text>
          <Text style={styles.code}>{invitationCode}</Text>

          <View style={styles.actions}>
            <TouchableOpacity
              style={[styles.button, styles.declineButton]}
              onPress={onDecline}
              disabled={loading}
            >
              <Text style={styles.declineButtonText}>{t('invitation.decline')}</Text>
            </TouchableOpacity>

            <TouchableOpacity
              style={[styles.button, styles.acceptButton]}
              onPress={handleAccept}
              disabled={loading}
            >
              {loading ? (
                <ActivityIndicator color="#fff" />
              ) : (
                <Text style={styles.acceptButtonText}>{t('invitation.accept')}</Text>
              )}
            </TouchableOpacity>
          </View>
        </View>
      </View>
    </RNModal>
  );
}

