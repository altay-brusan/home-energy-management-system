import React from 'react';
import { View, Text } from 'react-native';
import Input from '../../../../../components/forms/Input';
import Button from '../../../../../components/ui/Button/Button';

type Props = {
  email: string;
  setEmail: (v: string) => void;
  error: string;
  message: string;
  COLORS: any;
  styles: any;
  onInvite: () => void;
  onCancel: () => void;
};

export function DeviceSharingForm({
  email,
  setEmail,
  error,
  message,
  COLORS,
  styles,
  onInvite,
  onCancel,
}: Props) {
  return (
    <View style={styles.inputGroup}>
      <Input
        placeholder="Enter email"
        placeholderTextColor={COLORS.slate400}
        value={email}
        onChangeText={setEmail}
      />
      <View style={styles.buttonRow}>
        <Button
          title="Invite"
          style={styles.saveBtn}
          textStyle={styles.saveBtnText}
          onPress={onInvite}
        />
        <Button
          title="Cancel"
          style={styles.cancelBtn}
          textStyle={styles.cancelBtnText}
          onPress={onCancel}
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
    </View>
  );
}

export default DeviceSharingForm;
