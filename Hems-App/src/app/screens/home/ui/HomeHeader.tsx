import React from 'react';
import { View, Text, TouchableOpacity } from 'react-native';
import { Plus } from 'lucide-react-native';
import { useTranslation } from 'react-i18next';

type Props = {
  navigation: any;
  styles: any;
  COLORS: any;
};

export function HomeHeader({ navigation, styles, COLORS }: Props) {
  const { t } = useTranslation();

  return (
    <View style={styles.headerSection}>
      <View style={{
        flexDirection: 'row',
        alignItems: 'center',
        justifyContent: 'space-between',
        marginBottom: 16,
      }}>
        <Text style={{
          fontSize: 20,
          fontWeight: '600',
          color: COLORS.text,
        }}>
          {t('profile.myHomes')}
        </Text>

        {/* Add button on right */}
        <TouchableOpacity
          onPress={() => navigation.navigate('HomeSetup')}
          style={{
            padding: 8,
            marginRight: -8,
          }}
          hitSlop={8}
        >
          <Plus size={24} color={COLORS.accent} />
        </TouchableOpacity>
      </View>
    </View>
  );
}
