import FontAwesome from '@react-native-vector-icons/fontawesome';
import React, { useState } from 'react';
import { Linking, Pressable, Share, Text, View } from 'react-native';
import { getShareButtonStyles } from './ShareButton.styles';
import type { ShareButtonProps, ShareOption } from './ShareButton.types';
import { useTranslation } from 'react-i18next';

const SHARE_LINK = 'https://n2nenergy.com/dashboard';

export default function ShareButton({
  color,
  backgroundColor,
  top = 0,
  right = 0,
  COLORS,
}: ShareButtonProps) {
  const { t } = useTranslation();
  const [showSheet, setShowSheet] = useState(false);
  const styles = getShareButtonStyles(COLORS, color, backgroundColor, top, right);

  const SHARE_OPTIONS: ShareOption[] = [
    {
      label: t('dashboardShare.options.copy'),
      icon: 'link',
      action: async () => {
        await Share.share({ message: SHARE_LINK });
      },
    },
    {
      label: t('dashboardShare.options.whatsapp'),
      icon: 'whatsapp',
      action: () => {
        const url = `whatsapp://send?text=${encodeURIComponent(SHARE_LINK)}`;
        Linking.openURL(url);
      },
    },
    {
      label: t('dashboardShare.options.twitter'),
      icon: 'twitter',
      action: () => {
        const url = `https://twitter.com/intent/tweet?url=${encodeURIComponent(SHARE_LINK)}`;
        Linking.openURL(url);
      },
    },
    {
      label: t('dashboardShare.options.mail'),
      icon: 'envelope',
      action: () => {
        const url = `mailto:?subject=Check%20this%20dashboard&body=${encodeURIComponent(SHARE_LINK)}`;
        Linking.openURL(url);
      },
    },
  ];

  return (
    <>
      <Pressable onPress={() => setShowSheet(true)} style={styles.button} hitSlop={10}>
        <FontAwesome name="link" size={24} color={color} />
      </Pressable>

      {showSheet && (
        <View style={styles.overlay}>
          <Pressable style={{ flex: 1 }} onPress={() => setShowSheet(false)} />
          <View style={styles.sheet}>
            <Text style={styles.sheetTitle}>{t('dashboardShare.title')}</Text>
            {SHARE_OPTIONS.map(opt => (
              <Pressable
                key={opt.label}
                onPress={async () => {
                  setShowSheet(false);
                  setTimeout(opt.action, 200);
                }}
                style={styles.sheetOption}
              >
                <FontAwesome
                  name={opt.icon}
                  size={22}
                  color={color}
                  style={styles.sheetIcon}
                />
                <Text style={styles.sheetOptionText}>{opt.label}</Text>
              </Pressable>
            ))}
            <Pressable onPress={() => setShowSheet(false)} style={styles.cancelBtn}>
              <Text style={styles.cancelText}>{t('dashboardShare.cancel')}</Text>
            </Pressable>
          </View>
        </View>
      )}
    </>
  );
}
