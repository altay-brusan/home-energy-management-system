import React from 'react';
import { View, Text, SafeAreaView } from 'react-native';
import { useTranslation } from 'react-i18next';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { Bot } from 'lucide-react-native';
import { useThemeStore } from '../../../store/themeStore';
import { getThemeColors, SPACING } from '../../../theme/constants';

export default function AIScreen() {
  const { t } = useTranslation();
  const insets = useSafeAreaInsets();
  const mode = useThemeStore(s => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);

  return (
    <SafeAreaView style={{ flex: 1, backgroundColor: COLORS.dark }}>
      <View
        style={{
          flex: 1,
          justifyContent: 'center',
          alignItems: 'center',
          paddingHorizontal: SPACING.lg,
        }}
      >
        <Bot size={64} color={COLORS.accent} strokeWidth={1.5} />
        <Text
          style={{
            fontSize: 24,
            fontWeight: '700',
            color: COLORS.text,
            marginTop: SPACING.lg,
          }}
        >
          AI Assistant
        </Text>
        <Text
          style={{
            fontSize: 14,
            color: COLORS.textSoft,
            marginTop: SPACING.sm,
            textAlign: 'center',
          }}
        >
          {t('ai.comingSoon', 'Coming soon...')}
        </Text>
      </View>
    </SafeAreaView>
  );
}
