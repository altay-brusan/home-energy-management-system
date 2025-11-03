import React from 'react';
import { View, Text, ActivityIndicator } from 'react-native';
import Input from '../../../../../components/forms/Input';
import Button from '../../../../../components/ui/Button/Button';
import { useI18n } from '../../../../../hooks/useI18n';
import { useInvitation } from '../../../../../hooks/useInvitation';

type Props = {
  COLORS: any;
  styles: any;
  navigation?: any;
};

export function DeviceSharingForm({ COLORS, styles, navigation }: Props) {
  const { t } = useI18n();
  const {
    email,
    setEmail,
    error,
    message,
    loading,
    sendInvitation,
    reload,
  } = useInvitation();

  const handleSendInvitation = async () => {
    await sendInvitation();
  };

  return (
    <View style={styles.inputGroup}>
      <Input
        placeholder={t('deviceSharing.enterEmail')}
        placeholderTextColor={COLORS.slate400}
        keyboardType="email-address"
        autoCapitalize="none"
        autoCorrect={false}
        autoComplete="email"
        value={email}
        onChangeText={(text) => setEmail(text.toLowerCase())}
      />

      <View style={styles.buttonRow}>
        <Button
          title={loading ? t('deviceSharing.sending') : t('deviceSharing.invite')}
          style={styles.saveBtn}
          textStyle={styles.saveBtnText}
          disabled={loading}
          onPress={handleSendInvitation}
        />
        <Button
          title={t('deviceSharing.cancel')}
          style={styles.cancelBtn}
          textStyle={styles.cancelBtnText}
          onPress={() => {
            setEmail('');
            navigation?.goBack?.();
          }}
        />
      </View>

      {(!!error || !!message) && (
        <View style={styles.messageBox}>
          <Text
            style={[
              styles.messageText,
              error ? styles.errorText : styles.successText,
            ]}
          >
            {error || message}
          </Text>
        </View>
      )}

      {loading && (
        <View style={{ marginTop: 10 }}>
          <ActivityIndicator color={COLORS.accent} />
        </View>
      )}
    </View>
  );
}

export default DeviceSharingForm;
