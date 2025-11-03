import React, { useEffect, useState } from 'react';
import { Alert, Image, Text, TouchableOpacity, View } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import Input from '../../../../components/forms/Input';
import Button from '../../../../components/ui/Button';
import useAuth from '../../../../hooks/useAuth';
import { useI18n } from '../../../../hooks/useI18n';
import useProfile from '../../../../hooks/useProfile';
import { useThemeStore } from '../../../../store/themeStore';
import { getThemeColors } from '../../../../theme/constants';
import { makeProfileInfoStyles } from './ProfileInfoScreen.styles';

const AVATAR_PLACEHOLDER =
  'https://ui-avatars.com/api/?name=User&background=2563eb&color=fff&size=128';

export default function ProfileInfoScreen ({ navigation }: any) {
  const mode = useThemeStore(s => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);
  const styles = makeProfileInfoStyles(COLORS);
  const insets = useSafeAreaInsets();
  const { t } = useI18n();
  const { user } = useAuth();
  const { getUserInfo, updateProfile, profileLoading, profileError, profileSuccess, clearProfileError, clearProfileSuccess } = useProfile();

  const [form, setForm] = useState({
    firstName: '',
    lastName: '',
    email: '',
    phone: '',
    avatar: AVATAR_PLACEHOLDER,
  });
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');
  const [avatar, setAvatar] = useState(AVATAR_PLACEHOLDER);

  useEffect(() => {
    const loadUserInfo = async () => {
      if (user) {
        const userAvatar = user.name && user.surname
          ? `https://ui-avatars.com/api/?name=${user.name}+${user.surname}&background=2563eb&color=fff&size=128`
          : user.email
            ? `https://ui-avatars.com/api/?name=${user.email.split('@')[0]}&background=2563eb&color=fff&size=128`
            : AVATAR_PLACEHOLDER;

        try {
          const userInfo = await getUserInfo();
          let formattedPhone = '';
          if (userInfo.phoneNumber) {
            let digits = userInfo.phoneNumber.replace(/\D/g, '');
            if (digits.startsWith('90')) {
              digits = digits.slice(2);
            }
            if (digits.length === 10 && digits.startsWith('5')) {
              formattedPhone = `+90 ${digits.substring(0, 3)} ${digits.substring(3, 6)} ${digits.substring(6, 8)} ${digits.substring(8, 10)}`;
            }
          }

          setForm({
            firstName: userInfo.name || user.name || '',
            lastName: userInfo.surname || user.surname || '',
            email: userInfo.email || user.email || '',
            phone: formattedPhone,
            avatar: userAvatar,
          });
        } catch (error) {
          setForm({
            firstName: user.name || '',
            lastName: user.surname || '',
            email: user.email || '',
            phone: '',
            avatar: userAvatar,
          });
        }
        setAvatar(userAvatar);
      }
    };
    loadUserInfo();
  }, [user, getUserInfo]);

  const isRequired = (v: string) => v.trim().length >= 2;
  const validatePhone = (v: string) =>
    /^\+90 5\d{2} \d{3} \d{2} \d{2}$/.test(v);

  const handleSave = async () => {
    setError('');
    setMessage('');
    clearProfileError();
    clearProfileSuccess();

    if (!isRequired(form.firstName) || !isRequired(form.lastName)) {
      setError(t('profileInfo.fillAllFields'));
      return;
    }
    if (form.phone && !validatePhone(form.phone)) {
      setError(t('profileInfo.invalidPhone'));
      return;
    }

    try {
      let cleanedPhone = '';
      if (form.phone) {
        let digits = form.phone.replace(/\D/g, '');
        if (digits.startsWith('90')) {
          digits = digits.slice(2);
        }
        cleanedPhone = digits;
      }

      await updateProfile({
        firstName: form.firstName,
        lastName: form.lastName,
        phone: cleanedPhone,
        email: form.email,
      });
      setMessage(t('profileInfo.profileUpdated'));
    } catch (error: any) {setError(error.message || t('profileInfo.profileUpdateError'));
    }
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

  const handleAvatarPress: any = () => {
    Alert.alert(t('profileInfo.avatar'), t('profileInfo.avatarNotImplemented'));
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
      <Text style={styles.title}>{t('profileInfo.title')}</Text>
      <View style={styles.inputGroup}>
        <Input
          placeholder={t('profileInfo.firstName')}
          placeholderTextColor={COLORS.slate400}
          value={form.firstName}
          onChangeText={v => setForm(f => ({ ...f, firstName: v }))}
        />
        <Input
          placeholder={t('profileInfo.lastName')}
          placeholderTextColor={COLORS.slate400}
          value={form.lastName}
          onChangeText={v => setForm(f => ({ ...f, lastName: v }))}
        />
        <Input
          placeholder={t('profileInfo.email')}
          placeholderTextColor={COLORS.slate400}
          value={form.email}
          editable={false}
        />
        <Input
          placeholder={t('profileInfo.phone')}
          placeholderTextColor={COLORS.slate400}
          value={form.phone}
          keyboardType="phone-pad"
          onChangeText={handlePhoneChange}
          maxLength={17}
        />
      </View>
      {message && (
        <View style={styles.messageBox}>
          <Text style={[styles.messageText, styles.successText]}>
            {message}
          </Text>
        </View>
      )}
      <View style={styles.buttonRow}>
        <Button
          title={profileLoading ? t('common.loading') : t('profileInfo.save')}
          style={styles.saveBtn}
          textStyle={styles.saveBtnText}
          onPress={handleSave}
          disabled={profileLoading}
        />
        <Button
          title={t('profileInfo.cancel')}
          style={styles.cancelBtn}
          textStyle={styles.cancelBtnText}
          onPress={handleCancel}
          disabled={profileLoading}
        />
      </View>
    </View>
  );
}
