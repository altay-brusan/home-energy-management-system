import React, { useState } from 'react';
import { ScrollView, RefreshControl, Text, View } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import Input from '../../../../components/forms/Input';
import Button from '../../../../components/ui/Button/Button';
import Card from '../../../../components/ui/Card/Card';
import { useThemeStore } from '../../../../store/themeStore';
import { getThemeColors } from '../../../../theme/constants';
import { makeCompanyInfoStyles } from './CompanyInfoScreen.styles';
import { useNetInfo } from '../../../../hooks/useNetInfo';
import { useI18n } from '../../../../hooks/useI18n';

export default function CompanyInfoScreen({ navigation }: any) {
  const mode = useThemeStore(s => s.mode);
  const isDark = mode === 'dark';
  const COLORS: Record<string, string> = getThemeColors(isDark);
  const { t } = useI18n();

  const insets = useSafeAreaInsets();
  const styles = makeCompanyInfoStyles(COLORS, isDark, insets.top);
  const { isConnected } = useNetInfo();

  const [companyName, setCompanyName] = useState('N2N ENERGY');
  const [address, setAddress] = useState('İstanbul, Türkiye');
  const [email, setEmail] = useState('info@info.com');
  const [phone, setPhone] = useState('+90 555 555 55 55');

  const region = {
    id: 'Europe',
    label: 'Europe',
    emoji: '🇪🇺',
    timezone: 'GMT+1',
    description: 'Frankfurt, London',
  };

  const [refreshing, setRefreshing] = useState(false);

  const onRefresh = async () => {
    if (!isConnected) return;
    setRefreshing(true);
    try {
    } finally {
      setRefreshing(false);
    }
  };

  const handleSave = () => {navigation?.goBack?.();
  };

  const handleCancel = () => {
    navigation?.goBack?.();
  };

  return (
    <ScrollView
      style={styles.container}
      contentContainerStyle={styles.pageScroll}
      refreshControl={
        <RefreshControl
          refreshing={refreshing}
          onRefresh={onRefresh}
          enabled={!!isConnected}
          colors={[COLORS.accent]}
          tintColor={COLORS.accent}
        />
      }
    >
      <Text style={styles.title}>{t('companyInfo.title')}</Text>
      <Card>
        <View style={styles.inputGroup}>
          <Input
            placeholder={t('companyInfo.companyName')}
            placeholderTextColor={COLORS.textMuted}
            value={companyName}
            onChangeText={setCompanyName}
          />
          <Input
            placeholder={t('companyInfo.address')}
            placeholderTextColor={COLORS.textMuted}
            value={address}
            onChangeText={setAddress}
          />
          <Input
            placeholder={t('companyInfo.email')}
            placeholderTextColor={COLORS.textMuted}
            value={email}
            onChangeText={setEmail}
          />
          <Input
            placeholder={t('companyInfo.phone')}
            placeholderTextColor={COLORS.textMuted}
            value={phone}
            onChangeText={setPhone}
          />
        </View>
      </Card>

 <Text style={styles.regionTitle}>{t('companyInfo.regionTitle')}</Text>
<Card style={styles.regionCard}>
  <View style={styles.regionRow}>
    <Text style={styles.regionEmoji}>{region.emoji}</Text>
    <View>
      <Text style={styles.regionLabel}>{region.label}</Text>
      <Text style={styles.regionMeta}>
        {region.timezone} • {region.description}
      </Text>
    </View>
  </View>
</Card>
      <View style={styles.buttonRow}>
        <Button
          title={t('companyInfo.save')}
          style={styles.saveBtn}
          textStyle={styles.saveBtnText}
          onPress={handleSave}
        />
        <Button
          title={t('companyInfo.cancel')}
          style={styles.cancelBtn}
          textStyle={styles.cancelBtnText}
          onPress={handleCancel}
        />
      </View>
    </ScrollView>
  );
}
