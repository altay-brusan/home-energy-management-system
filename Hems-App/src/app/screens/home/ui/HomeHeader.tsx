import React from 'react';
import { View, Text, Pressable } from 'react-native';
import { Plus, User } from 'lucide-react-native';
import { useTranslation } from 'react-i18next';
import { useAuthStore } from '../../../../store/authStore';

type Props = {
  navigation: any;
  styles: any;
  COLORS: any;
};

export function HomeHeader({ navigation, styles, COLORS }: Props) {
  const { t } = useTranslation();
  const { user } = useAuthStore();
  const displayName = user?.name || user?.surname || user?.email || null;

  return (
    <View style={styles.headerSection}>
      <View style={[styles.welcomeSection, styles.welcomeRow]}>
        <View style={{ flex: 1 }}>
          <Text style={styles.welcomeText}>
            {displayName
              ? t('homeHeader.welcomeWithName', { name: displayName })
              : t('homeHeader.welcome')}
          </Text>
          <Text style={styles.subtitleText}>{t('homeHeader.subtitle')}</Text>
        </View>

        <Pressable
          onPress={() => navigation.navigate('MainTabs', { screen: 'Profile' })}
          hitSlop={8}
          style={styles.profileButton}
        >
          <User size={22} color={COLORS.textSoft} />
        </Pressable>
      </View>

      <View style={styles.actionButtons}>
        <Pressable
          onPress={() => navigation.navigate('HomeSetup')}
          style={[styles.primaryButton, styles.headerButton]}
          hitSlop={8}
        >
          <Plus size={18} color={COLORS.white} style={{ marginRight: 6 }} />
          <Text style={styles.primaryButtonText}>{t('homeHeader.addButton')}</Text>
        </Pressable>
      </View>
    </View>
  );
}
