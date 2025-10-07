import React, { useState } from 'react';
import { Alert, Image, Text, TouchableOpacity, View } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import Button from '../../../../components/ui/Button';
import Input from '../../../../components/forms/Input';
import { useThemeStore } from '../../../../store/themeStore';
import { getThemeColors } from '../../../../theme/constants';
import { makeProfileInfoStyles } from './ProfileInfoScreen.styles';

const AVATAR_PLACEHOLDER =
  'https://ui-avatars.com/api/?name=User&background=2563eb&color=fff&size=128';

export default function ProfileInfoScreen({ navigation }: any) {
  const mode = useThemeStore(s => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);
  const styles = makeProfileInfoStyles(COLORS);
  const insets = useSafeAreaInsets();
  const [form, setForm] = useState({
    firstName: '',
    lastName: '',
    email: 'user@example.com',
    phone: '',
    avatar: AVATAR_PLACEHOLDER,
  });
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');
  const [avatar, setAvatar] = useState(AVATAR_PLACEHOLDER);

  const isRequired = (v: string) => v.trim().length >= 2;
  const validatePhone = (v: string) =>
    /^\+90 5\d{2} \d{3} \d{2} \d{2}$/.test(v);

  const handleSave = () => {
    setError('');
    setMessage('');
    if (!isRequired(form.firstName) || !isRequired(form.lastName)) {
      setError('Please fill in all required fields.');
      return;
    }
    if (!validatePhone(form.phone)) {
      setError('Please enter a valid phone number.');
      return;
    }
    setMessage('Profile updated successfully.');
  };

  const handleCancel = () => {
    setError('');
    setMessage('');
    if (navigation && navigation.goBack) {
      navigation.goBack();
    }
  };

  const handlePhoneChange = (v: string) => {
    let digits = v.replace(/\D/g, '');
    if (digits.startsWith('90')) digits = digits.slice(2);
    if (!digits.startsWith('5')) digits = '5' + digits;
    let formatted = '+90 ';
    if (digits.length > 0) formatted += digits.substring(0, 3);
    if (digits.length >= 4) formatted += ' ' + digits.substring(3, 6);
    if (digits.length >= 7) formatted += ' ' + digits.substring(6, 8);
    if (digits.length >= 9) formatted += ' ' + digits.substring(8, 10);
    setForm(f => ({ ...f, phone: formatted.trim() }));
  };

  const handleAvatarPress:any = () => {
    Alert.alert('Avatar', 'Avatar upload/change not implemented in this demo.');
  };

  return (
    <View
      style={[
        styles.container,
        { paddingTop: insets.top + 24, paddingBottom: 24 },
      ]}
    >
      <View style={styles.avatarWrap}>
        <TouchableOpacity
          onPress={handleAvatarPress}
          style={styles.avatarTouchable}
          activeOpacity={0.8}
        >
          <Image source={{ uri: avatar }} style={styles.avatar} />
        </TouchableOpacity>
      </View>
      <Text style={styles.title}>Personal Information</Text>
      <View style={styles.inputGroup}>
        <Input
          placeholder="First Name*"
          placeholderTextColor={COLORS.slate400}
          value={form.firstName}
          onChangeText={v => setForm(f => ({ ...f, firstName: v }))}
        />
        <Input
          placeholder="Last Name*"
          placeholderTextColor={COLORS.slate400}
          value={form.lastName}
          onChangeText={v => setForm(f => ({ ...f, lastName: v }))}
        />
        <Input
          placeholder="Email"
          placeholderTextColor={COLORS.slate400}
          value={form.email}
          editable={false}
        />
        <Input
          placeholder="Phone*"
          placeholderTextColor={COLORS.slate400}
          value={form.phone}
          keyboardType="phone-pad"
          onChangeText={handlePhoneChange}
          maxLength={17}
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
