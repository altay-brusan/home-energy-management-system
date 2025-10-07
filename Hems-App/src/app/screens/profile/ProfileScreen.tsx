import FontAwesome from '@react-native-vector-icons/fontawesome';
import React from 'react';
import { Image, ScrollView, Text, TouchableOpacity, View } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import Button from '../../../components/ui/Button';
import RegionSelector, {
  type Region,
} from '../../../components/ui/RegionSelector/RegionSelector';
import ThemeToggle from '../../../components/ui/ThemeToggle';
import useAuth from '../../../hooks/useAuth';
import { useThemeStore } from '../../../store/themeStore';
import { getThemeColors, SPACING } from '../../../theme/constants';
import { makeProfileScreenStyles } from './ProfileScreenStyles';

const AVATAR_URL =
  'https://ui-avatars.com/api/?name=User&background=2563eb&color=fff&size=128';
const EMAIL = 'user@example.com';

export default function ProfileScreen ({ navigation }: any) {
  const mode = useThemeStore(s => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);
  const styles = makeProfileScreenStyles(COLORS);
  const insets = useSafeAreaInsets();
  const [region, setRegion] = React.useState<Region>('Europe');

  const { logout } = useAuth();

  const handleLogout = async () => {
    await logout();
    navigation.replace('Login');
  };

  return (
    <View style={{ flex: 1, backgroundColor: COLORS.dark }}>
      <View style={[styles.topBar, { marginTop: insets.top + 8 }]}>
        <ThemeToggle />
        <RegionSelector selectedRegion={region} onRegionChange={setRegion} />
      </View>
      <ScrollView
        contentContainerStyle={[
          styles.container,
          { paddingTop: 8, paddingBottom: insets.bottom + 24 },
        ]}
        showsVerticalScrollIndicator={false}
      >
        <View style={styles.avatarContainer}>
          <Image source={{ uri: AVATAR_URL }} style={styles.avatar} />
          <Text style={styles.email}>{EMAIL}</Text>
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
            <Text style={styles.sectionTitle}>Personal Info</Text>
          </TouchableOpacity>
          <TouchableOpacity style={styles.card}
            onPress={() => navigation.navigate('DeviceSharing')}>

            <FontAwesome
              name="share-alt"
              size={22}
              color={COLORS.accent}
              style={{ marginRight: 12 }}
            />
            <Text style={styles.sectionTitle}>
              Device Sharing & Authorization
            </Text>
          </TouchableOpacity>
          {/* <TouchableOpacity style={styles.card}
            onPress={() => navigation.navigate('CompanyInfo')}>
            <FontAwesome
              name="building"
              size={22}
              color={COLORS.accent}
              style={{ marginRight: 12 }}
            />
            <Text style={styles.sectionTitle}>Company / Region Info</Text>
          </TouchableOpacity> */}
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
  <Text style={styles.sectionTitle}>Troubleshooting</Text>
</TouchableOpacity>

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
            <Text style={styles.sectionTitle}>Change Password</Text>
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
            <Text style={styles.sectionTitle}>Logout</Text>
          </TouchableOpacity>
        </View>
      </ScrollView>
    </View>
  );
}
