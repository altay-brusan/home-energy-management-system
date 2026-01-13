/**
 * SettingPicker Component
 * Dropdown seçim ayarı
 */

import React, { useState } from 'react';
import {
  View,
  Text,
  TouchableOpacity,
  Modal,
  FlatList,
  StyleSheet,
  Pressable,
} from 'react-native';
import MaterialIcons from '@react-native-vector-icons/material-icons';
import { useThemeStore } from '../../store/themeStore';
import { getThemeColors } from '../../theme/constants';

interface PickerOption {
  value: string;
  label: string;
}

interface SettingPickerProps {
  label: string;
  value: string;
  options: PickerOption[];
  onChange: (value: string) => void;
  description?: string;
  disabled?: boolean;
}

const SettingPicker: React.FC<SettingPickerProps> = ({
  label,
  value,
  options,
  onChange,
  description,
  disabled = false,
}) => {
  const mode = useThemeStore((s) => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);
  const [modalVisible, setModalVisible] = useState(false);

  const selectedOption = options.find((opt) => opt.value === value);

  const handleSelect = (optionValue: string) => {
    onChange(optionValue);
    setModalVisible(false);
  };

  return (
    <>
      <TouchableOpacity
        onPress={() => !disabled && setModalVisible(true)}
        activeOpacity={disabled ? 1 : 0.7}
        style={[
          styles.container,
          { borderBottomColor: isDark ? COLORS.border : COLORS.gray200 },
        ]}
      >
        <View style={styles.labelContainer}>
          <Text
            style={[
              styles.label,
              { color: disabled ? COLORS.textSoft : COLORS.text },
            ]}
          >
            {label}
          </Text>
          {description && (
            <Text style={[styles.description, { color: COLORS.textSoft }]}>
              {description}
            </Text>
          )}
        </View>
        <View style={styles.valueContainer}>
          <Text style={[styles.value, { color: COLORS.accent }]}>
            {selectedOption?.label || value}
          </Text>
          <MaterialIcons
            name="chevron-right"
            size={20}
            color={COLORS.textSoft}
          />
        </View>
      </TouchableOpacity>

      <Modal
        visible={modalVisible}
        transparent
        animationType="fade"
        onRequestClose={() => setModalVisible(false)}
      >
        <Pressable
          style={styles.modalOverlay}
          onPress={() => setModalVisible(false)}
        >
          <View
            style={[
              styles.modalContent,
              { backgroundColor: isDark ? COLORS.surface : COLORS.white },
            ]}
          >
            <Text style={[styles.modalTitle, { color: COLORS.text }]}>
              {label}
            </Text>
            <FlatList
              data={options}
              keyExtractor={(item) => item.value}
              renderItem={({ item }) => {
                const isSelected = item.value === value;
                return (
                  <TouchableOpacity
                    onPress={() => handleSelect(item.value)}
                    style={[
                      styles.option,
                      {
                        backgroundColor: isSelected
                          ? `${COLORS.accent}15`
                          : 'transparent',
                        borderColor: isSelected ? COLORS.accent : COLORS.border,
                      },
                    ]}
                  >
                    <Text
                      style={[
                        styles.optionText,
                        { color: isSelected ? COLORS.accent : COLORS.text },
                      ]}
                    >
                      {item.label}
                    </Text>
                    {isSelected && (
                      <MaterialIcons
                        name="check"
                        size={20}
                        color={COLORS.accent}
                      />
                    )}
                  </TouchableOpacity>
                );
              }}
            />
          </View>
        </Pressable>
      </Modal>
    </>
  );
};

const styles = StyleSheet.create({
  container: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingVertical: 12,
    borderBottomWidth: 1,
  },
  labelContainer: {
    flex: 1,
  },
  label: {
    fontSize: 14,
    fontWeight: '500',
  },
  description: {
    fontSize: 12,
    marginTop: 2,
  },
  valueContainer: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  value: {
    fontSize: 14,
    fontWeight: '600',
    marginRight: 4,
  },
  modalOverlay: {
    flex: 1,
    backgroundColor: 'rgba(0,0,0,0.5)',
    justifyContent: 'center',
    alignItems: 'center',
    padding: 20,
  },
  modalContent: {
    width: '100%',
    maxHeight: '70%',
    borderRadius: 16,
    padding: 20,
  },
  modalTitle: {
    fontSize: 18,
    fontWeight: '600',
    marginBottom: 16,
    textAlign: 'center',
  },
  option: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: 14,
    borderRadius: 10,
    borderWidth: 1,
    marginBottom: 8,
  },
  optionText: {
    fontSize: 15,
    fontWeight: '500',
  },
});

export default SettingPicker;
