import React, { useEffect, useState } from 'react';
import { Alert, Image, Text, TextInput, TouchableOpacity, View, ScrollView, Pressable } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { ArrowLeft, Camera, User, Mail, Phone } from 'lucide-react-native';
import useAuth from '../../../../hooks/useAuth';
import { useI18n } from '../../../../hooks/useI18n';
import useProfile from '../../../../hooks/useProfile';
import { useThemeStore } from '../../../../store/themeStore';
import { getThemeColors } from '../../../../theme/constants';
import { makeProfileInfoStyles } from './ProfileInfoScreen.styles';

const AVATAR_PLACEHOLDER =
  'https://ui-avatars.com/api/?name=User&background=2563eb&color=fff&size=128';

export default function ProfileInfoScreen({ navigation }: any) {
  const mode = useThemeStore(s => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);
  const styles = makeProfileInfoStyles(COLORS, isDark);
  const insets = useSafeAreaInsets();
  const { t } = useI18n();
  const { user } = useAuth();
  const { getUserInfo, updateProfile, profileLoading, clearProfileError, clearProfileSuccess } = useProfile();

  const [form, setForm] = useState({
    firstName: '',
    lastName: '',
    email: '',
    phone: '',
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
          });
        } catch {
          setForm({
            firstName: user.name || '',
            lastName: user.surname || '',
            email: user.email || '',
            phone: '',
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
    } catch (err: any) {
      setError(err.message || t('profileInfo.profileUpdateError'));
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

  const handleAvatarPress = () => {
    Alert.alert(t('profileInfo.avatar'), t('profileInfo.avatarNotImplemented'));
  };

  return (
    <View style={[styles.container, { paddingTop: insets.top }]}>
      {/* Header */}
      <View style={styles.header}>
        <TouchableOpacity onPress={() => navigation.goBack()} style={styles.backButton}>
          <ArrowLeft size={24} color={COLORS.text} />
        </TouchableOpacity>
        <Text style={styles.headerTitle}>{t('profileInfo.title')}</Text>
        <View style={{ width: 40 }} />
      </View>

      <ScrollView
        contentContainerStyle={[styles.scrollContent, { paddingBottom: insets.bottom + 100 }]}
        showsVerticalScrollIndicator={false}
      >
        {/* Avatar Section */}
        <View style={styles.avatarSection}>
          <TouchableOpacity onPress={handleAvatarPress} style={styles.avatarContainer} activeOpacity={0.8}>
            <Image source={{ uri: avatar }} style={styles.avatar} />
            <View style={styles.avatarEditBadge}>
              <Camera size={16} color={COLORS.white} />
            </View>
          </TouchableOpacity>
          <Text style={styles.avatarName}>
            {form.firstName} {form.lastName}
          </Text>
          <Text style={styles.avatarEmail}>{form.email}</Text>
        </View>

        {/* Personal Information Section */}
        <View style={styles.section}>
          <Text style={styles.sectionTitle}>{t('profileInfo.personalDetails')}</Text>

          <View style={styles.inputRow}>
            <Text style={styles.inputLabel}>{t('profileInfo.firstName')}</Text>
            <TextInput
              style={styles.inputField}
              value={form.firstName}
              onChangeText={v => setForm(f => ({ ...f, firstName: v }))}
              placeholderTextColor={COLORS.textSoft}
            />
          </View>

          <View style={styles.inputRow}>
            <Text style={styles.inputLabel}>{t('profileInfo.lastName')}</Text>
            <TextInput
              style={styles.inputField}
              value={form.lastName}
              onChangeText={v => setForm(f => ({ ...f, lastName: v }))}
              placeholderTextColor={COLORS.textSoft}
            />
          </View>
        </View>

        {/* Contact Information Section */}
        <View style={styles.section}>
          <Text style={styles.sectionTitle}>{t('profileInfo.contactDetails')}</Text>

          <View style={styles.inputRow}>
            <Text style={styles.inputLabel}>{t('profileInfo.email')}</Text>
            <TextInput
              style={[styles.inputField, styles.inputFieldDisabled]}
              value={form.email}
              editable={false}
              placeholderTextColor={COLORS.textSoft}
            />
          </View>

          <View style={styles.inputRow}>
            <Text style={styles.inputLabel}>{t('profileInfo.phone')}</Text>
            <TextInput
              style={styles.inputField}
              value={form.phone}
              onChangeText={handlePhoneChange}
              keyboardType="phone-pad"
              maxLength={17}
              placeholder="+90 5XX XXX XX XX"
              placeholderTextColor={COLORS.textSoft}
            />
          </View>
        </View>

        {/* Messages */}
        {message ? (
          <View style={[styles.messageBox, styles.successBox]}>
            <Text style={[styles.messageText, styles.successText]}>{message}</Text>
          </View>
        ) : null}

        {error ? (
          <View style={[styles.messageBox, styles.errorBox]}>
            <Text style={[styles.messageText, styles.errorText]}>{error}</Text>
          </View>
        ) : null}
      </ScrollView>

      {/* Bottom Buttons */}
      <View style={[styles.buttonContainer, { paddingBottom: insets.bottom + 16 }]}>
        <Pressable
          style={[styles.saveButton, profileLoading && { opacity: 0.6 }]}
          onPress={handleSave}
          disabled={profileLoading}
        >
          <Text style={styles.saveButtonText}>
            {profileLoading ? t('common.loading') : t('profileInfo.save')}
          </Text>
        </Pressable>
        <Pressable style={styles.cancelButton} onPress={() => navigation.goBack()}>
          <Text style={styles.cancelButtonText}>{t('profileInfo.cancel')}</Text>
        </Pressable>
      </View>
    </View>
  );
}
