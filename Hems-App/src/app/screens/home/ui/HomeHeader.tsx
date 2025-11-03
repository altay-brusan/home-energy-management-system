import React from 'react';
import { View, Text, Pressable } from 'react-native';
import MaterialIcons from '@react-native-vector-icons/material-icons';
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
  <MaterialIcons name="person-outline" size={26} style={styles.profileIcon} />
</Pressable>
</View>

      <View style={styles.actionButtons}>
        <Pressable
          onPress={() => navigation.navigate('HomeSetup')}
          style={[styles.primaryButton, styles.headerButton]}
          hitSlop={8}
        >
          <MaterialIcons
            name="home"
            size={18}
            color={COLORS.white}
            style={{ marginRight: 6 }}
          />
          <Text style={styles.primaryButtonText}>{t('homeHeader.addButton')}</Text>
        </Pressable>
      </View>
    </View>
  );
}
