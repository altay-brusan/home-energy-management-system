import React, { useState } from 'react';
import { ScrollView, Text, View, Pressable, LayoutAnimation, Platform, UIManager } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useThemeStore } from '../../../../store/themeStore';
import { getThemeColors } from '../../../../theme/constants';
import { makeTroubleShootingStyles } from './TroubleShootingScreen.styles';
import Input from '../../../../components/forms/Input';
import Button from '../../../../components/ui/Button';
import MainLayout from '../../../layouts/MainLayout';
import FontAwesome from '@react-native-vector-icons/fontawesome';
import { useFaultsStore } from '../../../../store/faultsStore';

const faultTypes = [
  { label: "Alarm-Related", value: "alarm" },
  { label: "Decoration Cover Damage", value: "decoration" },
  { label: "Inquiry", value: "inquiry" },
  { label: "Other", value: "other" },
];

if (Platform.OS === 'android' && UIManager.setLayoutAnimationEnabledExperimental) {
  UIManager.setLayoutAnimationEnabledExperimental(true);
}

export default function TroubleShootingScreen({ navigation }: any) {
  const mode = useThemeStore(s => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);
  const styles = makeTroubleShootingStyles(COLORS, isDark);
  const insets = useSafeAreaInsets();

  const addFault = useFaultsStore(s => s.addFault);

  const [form, setForm] = useState({
    faultType: '',
    description: '',
    name: '',
    phone: '',
    address: '',
    zip: '',
    email: '',
  });

  const [showDropdown, setShowDropdown] = useState(false);

  const handleChange = (field: string, value: string) => {
    setForm(f => ({ ...f, [field]: value }));
  };

  const toggleDropdown = () => {
    LayoutAnimation.configureNext(LayoutAnimation.Presets.easeInEaseOut);
    setShowDropdown(!showDropdown);
  };

  const handleConfirm = () => {
    addFault(form);
    navigation.navigate('Faults'); 
  };

  return (
    <MainLayout>
      <ScrollView
        style={styles.container}
        contentContainerStyle={{ paddingBottom: insets.bottom + 24 }}
        showsVerticalScrollIndicator={false}
      >
        <View style={styles.headerRow}>
          <Pressable style={styles.backButton} onPress={() => navigation.goBack()}>
            <FontAwesome name="chevron-left" size={18} color={COLORS.textSoft} />
          </Pressable>
          <Text style={styles.screenTitle}>Troubleshooting</Text>
          <View style={{ width: 32 }} />
        </View>

        <Text style={styles.title}>Issue Details</Text>

        <View style={{ marginBottom: 12, position: 'relative' }}>
          <Pressable onPress={toggleDropdown}>
            <Input
              placeholder="Fault Type*"
              placeholderTextColor={COLORS.slate400}
              value={form.faultType}
              editable={false}
              pointerEvents="none"
            />
            <FontAwesome
              name={showDropdown ? "chevron-up" : "chevron-down"}
              size={16}
              color={COLORS.slate400}
              style={{ position: 'absolute', right: 16, top: '35%' }}
            />
          </Pressable>

          {showDropdown && (
            <View style={styles.dropdownOverlay}>
              <View style={styles.dropdownList}>
                {faultTypes.map((item, idx) => (
                  <Pressable
                    key={idx}
                    style={styles.dropdownItem}
                    onPress={() => {
                      handleChange("faultType", item.label);
                      toggleDropdown();
                    }}
                  >
                    <Text style={styles.dropdownItemText}>{item.label}</Text>
                  </Pressable>
                ))}
              </View>
            </View>
          )}
        </View>

        <Input
          placeholder="Description*"
          placeholderTextColor={COLORS.slate400}
          value={form.description}
          onChangeText={v => handleChange('description', v)}
          multiline
        />

        <Text style={[styles.title, { marginTop: 24 }]}>Contact Information</Text>
        <Input placeholder="Name*" value={form.name} onChangeText={v => handleChange('name', v)} />
        <Input placeholder="Phone*" value={form.phone} onChangeText={v => handleChange('phone', v)} />
        <Input placeholder="Address*" value={form.address} onChangeText={v => handleChange('address', v)} />
        <Input placeholder="Zip Code*" value={form.zip} onChangeText={v => handleChange('zip', v)} />
        <Input placeholder="Email*" value={form.email} onChangeText={v => handleChange('email', v)} />

        <Button
          title="Confirm"
          style={styles.confirmBtn}
          textStyle={styles.confirmBtnText}
          onPress={handleConfirm}
        />
      </ScrollView>
    </MainLayout>
  );
}
