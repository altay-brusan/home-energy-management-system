import React from 'react';
import { View, Text, Pressable } from 'react-native';
import { Home, MapPin, MoreVertical, Zap, Plus, Trash2 } from 'lucide-react-native';
import { useTranslation } from 'react-i18next';
import { useSitesStore } from '../../../../store/sitesStore';

type Props = {
  home: { id: string; name: string; address: string; hasDevice: boolean; power: string };
  styles: any;
  COLORS: any;
  isDark: boolean;
  navigation: any;
  menuVisibleId: string | null;
  setMenuVisibleId: (id: string | null) => void;
  onDelete: (id: string) => void;
};

export function HomeCard({
  home,
  styles,
  COLORS,
  isDark,
  navigation,
  menuVisibleId,
  setMenuVisibleId,
  onDelete,
}: Props) {
  const { t } = useTranslation();
  const { selectSiteById } = useSitesStore();

  return (
    <Pressable
      onPress={async () => {
        await selectSiteById(home.id);
        if (home.hasDevice) {
          navigation.navigate('MainTabs', { screen: 'Dashboard' });
        } else {
          navigation.navigate('DeviceAdd', { siteId: home.id });
        }
      }}
      android_ripple={{ color: COLORS.accentGlow }}
      style={({ pressed }) => [
        styles.card,
        pressed && styles.cardPressed,
      ]}
    >
      {/* Left Icon */}
      <View style={[styles.cardIcon, { backgroundColor: home.hasDevice ? `${COLORS.accent}15` : `${COLORS.warning}15` }]}>
        <Home size={22} color={home.hasDevice ? COLORS.accent : COLORS.warning} />
      </View>

      {/* Content */}
      <View style={styles.cardContent}>
        <View style={styles.cardTitleRow}>
          <Text style={styles.cardTitle} numberOfLines={1}>{home.name}</Text>
          {!home.hasDevice && (
            <View style={styles.noDeviceBadge}>
              <Text style={styles.noDeviceBadgeText}>{t('homeCard.noDevice')}</Text>
            </View>
          )}
        </View>
        {home.address ? (
          <View style={styles.addressRow}>
            <MapPin size={12} color={COLORS.textSoft} />
            <Text style={styles.cardSubtitle} numberOfLines={1}>{home.address}</Text>
          </View>
        ) : null}
      </View>

      {/* Right Actions */}
      <View style={styles.cardActions}>
        <Pressable
          hitSlop={12}
          onPress={() => setMenuVisibleId(menuVisibleId === home.id ? null : home.id)}
          style={styles.menuButton}
        >
          <MoreVertical size={18} color={COLORS.textSoft} />
        </Pressable>
      </View>

      {/* Dropdown Menu */}
      {menuVisibleId === home.id && (
        <View style={styles.dropdownMenu}>
          {home.hasDevice ? (
            <Pressable
              style={styles.dropdownItem}
              onPress={async () => {
                await selectSiteById(home.id);
                navigation.navigate('MainTabs', { screen: 'Dashboard' });
                setMenuVisibleId(null);
              }}
            >
              <Zap size={16} color={COLORS.accent} />
              <Text style={styles.dropdownItemText}>{t('homeCard.goToDashboard')}</Text>
            </Pressable>
          ) : (
            <Pressable
              style={styles.dropdownItem}
              onPress={async () => {
                await selectSiteById(home.id);
                navigation.navigate('DeviceAdd', { siteId: home.id });
                setMenuVisibleId(null);
              }}
            >
              <Plus size={16} color={COLORS.accent} />
              <Text style={styles.dropdownItemText}>{t('homeCard.addDevice')}</Text>
            </Pressable>
          )}
          <View style={styles.dropdownDivider} />
          <Pressable style={styles.dropdownItem} onPress={() => onDelete(home.id)}>
            <Trash2 size={16} color={COLORS.error} />
            <Text style={[styles.dropdownItemText, { color: COLORS.error }]}>
              {t('homeCard.delete')}
            </Text>
          </Pressable>
        </View>
      )}
    </Pressable>
  );
}
