import FontAwesome from '@react-native-vector-icons/fontawesome';
import React, { useState } from 'react';
import { Linking, Pressable, Share, Text, View } from 'react-native';
import { getShareButtonStyles } from './ShareButton.styles';
import type { ShareButtonProps, ShareOption } from './ShareButton.types';

const SHARE_LINK = 'https://n2nenergy.com/dashboard';

const SHARE_OPTIONS: ShareOption[] = [
  {
    label: 'Copy Link',
    icon: 'link',
    action: async () => {
      await Share.share({ message: SHARE_LINK });
    },
  },
  {
    label: 'WhatsApp',
    icon: 'whatsapp',
    action: () => {
      const url = `whatsapp://send?text=${encodeURIComponent(SHARE_LINK)}`;
      Linking.openURL(url);
    },
  },
  {
    label: 'Twitter',
    icon: 'twitter',
    action: () => {
      const url = `https://twitter.com/intent/tweet?url=${encodeURIComponent(
        SHARE_LINK,
      )}`;
      Linking.openURL(url);
    },
  },
  {
    label: 'Mail',
    icon: 'envelope',
    action: () => {
      const url = `mailto:?subject=Check%20this%20dashboard&body=${encodeURIComponent(
        SHARE_LINK,
      )}`;
      Linking.openURL(url);
    },
  },
];

export default function ShareButton({
  color,
  backgroundColor,
  top = 0,
  right = 0,
  COLORS,
}: ShareButtonProps) {
  const [showSheet, setShowSheet] = useState(false);
  const styles = getShareButtonStyles(
    COLORS,
    color,
    backgroundColor,
    top,
    right,
  );

  return (
    <>
      <Pressable
        onPress={() => setShowSheet(true)}
        style={styles.button}
        hitSlop={10}
      >
        <FontAwesome name="link" size={24} color={color} />
      </Pressable>
      {showSheet && (
        <View style={styles.overlay}>
          <Pressable style={{ flex: 1 }} onPress={() => setShowSheet(false)} />
          <View style={styles.sheet}>
            <Text style={styles.sheetTitle}>Share Dashboard</Text>
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
            <Pressable
              onPress={() => setShowSheet(false)}
              style={styles.cancelBtn}
            >
              <Text style={styles.cancelText}>Cancel</Text>
            </Pressable>
          </View>
        </View>
      )}
    </>
  );
}
