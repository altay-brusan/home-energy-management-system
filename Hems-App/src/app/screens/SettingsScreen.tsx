import React, { JSX } from 'react';
import { View, Text, StyleSheet } from 'react-native';
import Animated, {
  interpolate,
  useAnimatedStyle,
  useSharedValue,
  withSpring,
} from 'react-native-reanimated';
import Button from '../../components/ui/Button';
import { useNavigation } from '@react-navigation/native';
import { NativeStackNavigationProp } from '@react-navigation/native-stack';
import useAuth from '../../hooks/useAuth';
import { useI18n } from '../../hooks/useI18n';

type RootStackParamList = { Login: undefined; MainTabs: undefined };

export default function SettingsScreen(): JSX.Element {
  const progress = useSharedValue(0);
  const navigation =
    useNavigation<NativeStackNavigationProp<RootStackParamList>>();
  const { logout } = useAuth();
  const { t } = useI18n();

  const animatedStyle = useAnimatedStyle(() => {
    const translateX = interpolate(progress.value, [0, 1], [-200, 0]);
    return {
      transform: [{ translateX }],
    };
  });

  React.useEffect(() => {
    progress.value = withSpring(1);
  }, []);

  const handleLogout = async () => {
    await logout();
  };

  return (
    <View style={styles.container}>
      <Animated.View style={animatedStyle}>
        <Text style={styles.title}>{t('settings.settingsScreen')}</Text>
        <View style={styles.settingItem}>
          <Text>{t('settings.setting1')}</Text>
        </View>
        <View style={styles.settingItem}>
          <Text>{t('settings.setting2')}</Text>
        </View>
        <Button
          title={t('settings.logout')}
          onPress={handleLogout}
          style={{ marginTop: 24 }}
        />
      </Animated.View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: '#fff',
  },
  title: {
    fontSize: 24,
    fontWeight: 'bold',
    marginBottom: 20,
    color: '#1f2937',
  },
  settingItem: {
    padding: 16,
    backgroundColor: '#f9fafb',
    marginVertical: 8,
    borderRadius: 8,
    width: 256,
  },
});
