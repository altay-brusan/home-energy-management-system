import FontAwesome from '@react-native-vector-icons/fontawesome';
import React, { useState, useEffect } from 'react';
import { Image, ScrollView, Text, TouchableOpacity, View, Pressable, Modal } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { CommonActions } from '@react-navigation/native';
import Button from '../../../components/ui/Button';
import RegionSelector, {
  type Region,
} from '../../../components/ui/RegionSelector/RegionSelector';
import ThemeToggle from '../../../components/ui/ThemeToggle';
import useAuth from '../../../hooks/useAuth';
import { useAuthStore } from '../../../store/authStore';
import { useSitesStore } from '../../../store/sitesStore';
import { useThemeStore } from '../../../store/themeStore';
import { getThemeColors, SPACING } from '../../../theme/constants';
import { makeProfileScreenStyles } from './ProfileScreenStyles';
import LanguageSelector from '../../../components/ui/LanguageSelector';
import { useI18n } from '../../../hooks/useI18n';
import pkg from '../../../../package.json';


export default function ProfileScreen ({ navigation }: any) {
  const mode = useThemeStore(s => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);
  const styles = makeProfileScreenStyles(COLORS, isDark);
  const insets = useSafeAreaInsets();
  const [region, setRegion] = React.useState<Region>('Europe');
  const { t } = useI18n();

  const { user, logout } = useAuth();
  const hasRole = useAuthStore(s => s.hasRole);
  const isAdmin = hasRole('Admin') || hasRole('Administrator');
  const { sites } = useSitesStore();

  // Site'ı olmayan kullanıcıları HomeSetup'a yönlendir
  useEffect(() => {
    if (!isAdmin && sites.length === 0) {
      navigation.dispatch(
        CommonActions.reset({
          index: 0,
          routes: [{ name: 'HomeSetup' }],
        })
      );
    }
  }, [sites, isAdmin, navigation]);
  const [showLogoutModal, setShowLogoutModal] = useState(false);

  const avatarUrl = user?.name && user?.surname 
    ? `https://ui-avatars.com/api/?name=${user.name}+${user.surname}&background=2563eb&color=fff&size=128`
    : user?.email 
    ? `https://ui-avatars.com/api/?name=${user.email.split('@')[0]}&background=2563eb&color=fff&size=128`
    : 'https://ui-avatars.com/api/?name=User&background=2563eb&color=fff&size=128';

const handleLogout = async () => {
  setShowLogoutModal(true);
};
const confirmLogout = async () => {
  setShowLogoutModal(false);
  await logout();
};

  return (
     <View style={{ flex: 1, backgroundColor: COLORS.dark }}>
      <View style={[styles.topBar, { marginTop: insets.top + 8 }]}>
        <ThemeToggle />
        <View style={styles.regionWrap}>
          <LanguageSelector />
        </View>
      </View>
      <ScrollView
        contentContainerStyle={[
          styles.container,
          { paddingTop: 8, paddingBottom: insets.bottom + 24 },
        ]}
        showsVerticalScrollIndicator={false}
      >
        <View style={styles.avatarContainer}>
          <Image source={{ uri: avatarUrl }} style={styles.avatar} />
          <Text style={styles.email}>{user?.email || ''}</Text>
        </View>
        <View style={styles.cardList}>
          <TouchableOpacity
            style={styles.card}
            onPress={() => navigation.navigate('ProfileInfo')}
          >
            <FontAwesome
              name="user"
              size={22}
              color={COLORS.accent}
              style={{ marginRight: 12 }}
            />
            <Text
              style={styles.sectionTitle}
              numberOfLines={1}
              ellipsizeMode="tail"
            >
              {t('profile.personalInfo')}
            </Text>
          </TouchableOpacity>
          {!isAdmin && (
          <TouchableOpacity style={styles.card}
            onPress={() => navigation.navigate('DeviceSharing')}>

            <FontAwesome
              name="share-alt"
              size={22}
              color={COLORS.accent}
              style={{ marginRight: 12 }}
            />
            <Text
              style={styles.sectionTitle}
              numberOfLines={1}
              ellipsizeMode="tail"
            >
              {t('profile.deviceSharing')}
            </Text>
          </TouchableOpacity>
          )}
{/* Troubleshooting - temporarily hidden
  <TouchableOpacity
  style={styles.card}
  onPress={() => navigation.navigate('Faults')}
>
  <FontAwesome
    name="wrench"
    size={22}
    color={COLORS.accent}
    style={{ marginRight: 12 }}
  />
            <Text
              style={styles.sectionTitle}
              numberOfLines={1}
              ellipsizeMode="tail"
            >
              {t('profile.troubleshooting')}
            </Text>
</TouchableOpacity>
*/}

          <TouchableOpacity
            style={styles.card}
            onPress={() => navigation.navigate('ChangePassword')}
          >
            <FontAwesome
              name="lock"
              size={22}
              color={COLORS.accent}
              style={{ marginRight: 12 }}
            />
            <Text
              style={styles.sectionTitle}
              numberOfLines={1}
              ellipsizeMode="tail"
            >
              {t('profile.changePassword')}
            </Text>
          </TouchableOpacity>
          <TouchableOpacity
            style={styles.card}
            onPress={handleLogout}
          >
            <FontAwesome
              name="sign-out"
              size={22}
              color={COLORS.accent}
              style={{ marginRight: 12 }}
            />
            <Text 
              style={styles.sectionTitle}
              numberOfLines={1}
              ellipsizeMode="tail"
            >
              {t('profile.logout')}
            </Text>
          </TouchableOpacity>
        </View>
        <Text style={styles.versionText}>
          {t('settings.version')}: {pkg.version}
        </Text>
        <Modal
  visible={showLogoutModal}
  transparent
  animationType="fade"
  onRequestClose={() => setShowLogoutModal(false)}
>
  <View
    style={{
      flex: 1,
      backgroundColor: 'rgba(0,0,0,0.5)',
      justifyContent: 'center',
      alignItems: 'center',
      padding: 24,
    }}
  >
    <View
      style={{
        backgroundColor: COLORS.surface,
        borderRadius: 16,
        padding: 20,
        width: '90%',
        maxWidth: 400,
      }}
    >
      <Text
        style={{
          color: COLORS.textSoft,
          fontSize: 16,
          fontWeight: '600',
          textAlign: 'center',
          marginBottom: 16,
        }}
      >
        {t('alerts.confirmLogout')}
      </Text>
      <View
        style={{
          flexDirection: 'row',
          justifyContent: 'space-around',
          marginTop: 8,
        }}
      >
        <Pressable
          onPress={() => setShowLogoutModal(false)}
          style={{
            backgroundColor: COLORS.border,
            paddingVertical: 10,
            paddingHorizontal: 20,
            borderRadius: 12,
            minWidth: 120,
          }}
        >
          <Text
            style={{
              color: COLORS.textSoft,
              fontWeight: '600',
              textAlign: 'center',
            }}
          >
            {t('common.cancel')}
          </Text>
        </Pressable>

        <Pressable
          onPress={confirmLogout}
          style={{
            backgroundColor: COLORS.accent,
            paddingVertical: 10,
            paddingHorizontal: 20,
            borderRadius: 12,
            minWidth: 120,
          }}
        >
          <Text
            style={{
              color: COLORS.white,
              fontWeight: '700',
              textAlign: 'center',
            }}
          >
            {t('common.confirm')}
          </Text>
        </Pressable>
      </View>
    </View>
  </View>
</Modal>
      </ScrollView>
    </View>
  );
}
