import React from 'react';
import { View, Text, TouchableOpacity } from 'react-native';
import MaterialIcons from '@react-native-vector-icons/material-icons';
import { useThemeStore } from '../../../../store/themeStore';
import { getThemeColors } from '../../../../theme/constants';
import { getSectionCardStyles } from './SectionCard.styles';
import { useI18n } from "../../../../hooks/useI18n";

type SectionCardProps = {
  title: string;
  items: { label: string; description?: string; locked?: boolean; active?: boolean }[];
  showIcon?: boolean;
  isExpanded?: boolean;
};

export default function SectionCard({ title, items, showIcon, isExpanded }: SectionCardProps) {
  const mode = useThemeStore(s => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);
  const { t } = useI18n();
  const styles = getSectionCardStyles(COLORS, isDark);

  return (
    <View style={styles.card}>
      {title !== "Current Mode" && (
        <View style={styles.headerRow}>
          <Text style={styles.title}>{title}</Text>
          {showIcon && <MaterialIcons name="settings" size={20} color={COLORS.textSoft} />}
        </View>
      )}

      {items.map((item, i) => (
        <TouchableOpacity key={i} style={styles.row} activeOpacity={0.7}>
          <View style={{ flex: 1 }}>
            <View style={{ flexDirection: 'row', alignItems: 'center' }}>
              <Text
                style={[
                  styles.label,
                  item.active && { color: COLORS.accent, fontWeight: '700', fontSize: 20 }
                ]}
              >
                {item.label}
              </Text>
              {item.active && (
                <MaterialIcons name="check" size={22} color={COLORS.accent} style={{ marginLeft: 6 }} />
              )}
            </View>
            {item.description && <Text style={styles.desc}>{item.description}</Text>}
          </View>
          {!item.locked && !item.active && showIcon && (
            <MaterialIcons name="chevron-right" size={20} color={COLORS.textSoft} />
          )}
          {item.locked && <MaterialIcons name="lock" size={18} color={COLORS.textSoft} />}
        </TouchableOpacity>
      ))}

      {title === 'Current Mode' && (
        <View style={styles.changeModeRow}>
          <Text style={styles.changeModeText}>Change the mode</Text>
          {showIcon ? (
            <>
              {isExpanded ? (
                <MaterialIcons name="keyboard-arrow-down" size={20} color={COLORS.accent} />
              ) : (
                <MaterialIcons name="keyboard-arrow-right" size={20} color={COLORS.accent} />
              )}
            </>
          ) : null}

        </View>
      )}
    </View>
  );
}
