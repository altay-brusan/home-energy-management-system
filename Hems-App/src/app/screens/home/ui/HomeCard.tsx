import React from 'react';
import { View, Text, Pressable, Image } from 'react-native';
import MaterialIcons from '@react-native-vector-icons/material-icons';
import { Card } from '../../../../components';
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
    <View style={styles.cardWrapper}>
      <Pressable
        onPress={async () => {
          await selectSiteById(home.id);
          if (home.hasDevice) {
            // Dashboard tab'ına özel yönlendirme
            navigation.navigate('MainTabs', { screen: 'Dashboard' });
          } else {
            navigation.navigate('DeviceAdd', { siteId: home.id });
          }
        }}
        android_ripple={{ color: COLORS.accentGlow }}
        style={({ pressed }) => [
          styles.pressable,
          pressed && styles.pressablePressed,
        ]}
        hitSlop={8}
      >
        <Card variant="elevated" style={styles.card}>
          <View style={styles.imageCard}>
            <Image
              source={
                isDark
                  ? require('../../../../assets/images/energy_home.bg.png')
                  : require('../../../../assets/images/energy_home.png')
              }
              style={styles.homeImage}
            />
            <View style={styles.imageOverlay}>
              <View style={styles.statusIndicator}>
                <View
                  style={[
                    styles.statusDot,
                    {
                      backgroundColor: home.hasDevice
                        ? COLORS.success
                        : COLORS.warning,
                    },
                  ]}
                />
                <Text style={styles.statusText}>
                  {home.hasDevice
                    ? t('homeCard.statusOnline')
                    : t('homeCard.statusNoDevice')}
                </Text>
              </View>
            </View>
          </View>

          <View style={styles.cardContent}>
            <View style={styles.cardHeader}>
              <View style={styles.cardTitleSection}>
                <Text style={styles.cardTitle}>{home.name}</Text>
                <Text style={styles.cardSubtitle}>{home.address}</Text>
              </View>
              <Pressable
                hitSlop={10}
                onPress={() =>
                  setMenuVisibleId(menuVisibleId === home.id ? null : home.id)
                }
                style={styles.cardMenuButton}
              >
                <MaterialIcons
                  name="more-vert"
                  size={20}
                  color={COLORS.textSoft}
                />
              </Pressable>
            </View>

            <View style={styles.cardStats}>
              <View style={styles.cardStatItem}>
                <MaterialIcons
                  name="device-hub"
                  size={16}
                  color={COLORS.accent}
                />
                <Text style={styles.cardStatText}>
                  {home.hasDevice
                    ? t('homeCard.deviceCount')
                    : t('homeCard.noDevice')}
                </Text>
              </View>
              <View style={styles.cardStatItem}>
                <MaterialIcons
                  name="flash-on"
                  size={16}
                  color={home.hasDevice ? COLORS.success : COLORS.disabled}
                />
                <Text
                  style={[
                    styles.cardStatText,
                    {
                      color: home.hasDevice
                        ? COLORS.textSoft
                        : COLORS.disabled,
                    },
                  ]}
                >
                  {home.hasDevice ? home.power : '0kW'}
                </Text>
              </View>
            </View>
          </View>

          {menuVisibleId === home.id && (
            <View style={styles.dropdownMenu}>
              {home.hasDevice ? (
                <Pressable
                  onPress={async () => {
                    await selectSiteById(home.id);
                    // Dashboard tab'ına özel yönlendirme
                    navigation.navigate('MainTabs', { screen: 'Dashboard' });
                    setMenuVisibleId(null);
                  }}
                >
                  <Text style={styles.dropdownItem}>{t('homeCard.goToDashboard')}</Text>
                </Pressable>
              ) : (
                <Pressable
                  onPress={async () => {
                    await selectSiteById(home.id);
                    navigation.navigate('DeviceAdd', { siteId: home.id });
                    setMenuVisibleId(null);
                  }}
                >
                  <Text style={styles.dropdownItem}>{t('homeCard.addDevice')}</Text>
                </Pressable>
              )}
              <Pressable onPress={() => onDelete(home.id)}>
                <Text style={[styles.dropdownItem, { color: 'red' }]}>
                  {t('homeCard.delete')}
                </Text>
              </Pressable>
            </View>
          )}
        </Card>
      </Pressable>
    </View>
  );
}
