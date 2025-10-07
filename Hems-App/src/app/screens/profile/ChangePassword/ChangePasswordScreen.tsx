import React, { useState } from 'react';
import { Text, View } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import Input from '../../../../components/forms/Input';
import Button from '../../../../components/ui/Button';
import { useThemeStore } from '../../../../store/themeStore';
import { getThemeColors } from '../../../../theme/constants';
import { makeChangePasswordStyles } from './ProfileInfoScreen.styles';

export default function ChangePasswordScreen({ navigation }: any) {
  const mode = useThemeStore(s => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);
  const styles = makeChangePasswordStyles(COLORS);
  const insets = useSafeAreaInsets();
  const [form, setForm] = useState({
    oldPassword: '',
    newPassword: '',
    repeatPassword: '',
  });
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');

  const validatePassword = (pw: string) =>
    pw.length >= 8 && /[A-Z]/.test(pw) && /[a-z]/.test(pw) && /[0-9]/.test(pw);

  const handleSave = () => {
    setError('');
    setMessage('');
    if (!form.oldPassword || !form.newPassword || !form.repeatPassword) {
      setError('Please fill in all fields.');
      return;
    }
    if (form.newPassword !== form.repeatPassword) {
      setError('New passwords do not match.');
      return;
    }
    if (!validatePassword(form.newPassword)) {
      setError(
        'Password must be at least 8 characters, include uppercase, lowercase, and a number.',
      );
      return;
    }
    if (form.oldPassword !== '123456') {
      setError('Old password is incorrect.');
      return;
    }
    setMessage('Password updated successfully.');
    setForm({ oldPassword: '', newPassword: '', repeatPassword: '' });
  };

  const handleCancel = () => {
    setError('');
    setMessage('');
    if (navigation && navigation.goBack) {
      navigation.goBack();
    }
  };

  return (
    <View
      style={[
        styles.container,
        { paddingTop: insets.top + 24, paddingBottom: 24 },
      ]}
    >
      <Text style={styles.title}>Change Password</Text>
      <View style={styles.inputGroup}>
        <Input
          placeholder="Old Password"
          placeholderTextColor={COLORS.slate400}
          value={form.oldPassword}
          secureTextEntry
          onChangeText={v => setForm(f => ({ ...f, oldPassword: v }))}
        />
        <Input
          placeholder="New Password"
          placeholderTextColor={COLORS.slate400}
          value={form.newPassword}
          secureTextEntry
          onChangeText={v => setForm(f => ({ ...f, newPassword: v }))}
        />
        <Input
          placeholder="Repeat New Password"
          placeholderTextColor={COLORS.slate400}
          value={form.repeatPassword}
          secureTextEntry
          onChangeText={v => setForm(f => ({ ...f, repeatPassword: v }))}
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
      <View style={styles.buttonRow}>
        <Button
          title="Save"
          style={styles.saveBtn}
          textStyle={styles.saveBtnText}
          onPress={handleSave}
        />
        <Button
          title="Cancel"
          style={styles.cancelBtn}
          textStyle={styles.cancelBtnText}
          onPress={handleCancel}
        />
      </View>
    </View>
  );
}
