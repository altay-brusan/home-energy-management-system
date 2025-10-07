import React, { useState } from 'react';
import {
  View,
  Text,
  TouchableOpacity,
  Modal,
  FlatList,
  Pressable,
  TextInput,
} from 'react-native';
import { getThemeColors } from '../../../theme/constants';
import { useThemeStore } from '../../../store/themeStore';
import { makeCitySelectorStyles } from '../CitySelector/CitySelector.styles';

export interface Country {
  code: string;
  name: string;
}

interface CountrySelectorProps {
  selectedCountry: Country | null;
  onCountrySelect: (country: Country) => void;
  error?: string;
  placeholder?: string;
  label?: string;
}

const COUNTRIES: Country[] = [
  { code: 'TR', name: 'Turkey' },
  { code: 'DE', name: 'Germany' },
  { code: 'US', name: 'United States' },
  { code: 'GB', name: 'United Kingdom' },
  { code: 'FR', name: 'France' },
  { code: 'IT', name: 'Italy' },
  { code: 'ES', name: 'Spain' },
  { code: 'NL', name: 'Netherlands' },
  { code: 'RU', name: 'Russia' },
  { code: 'CN', name: 'China' },
  { code: 'JP', name: 'Japan' },
  // listeyi ihtiyacına göre genişletebilirsin
];

export default function CountrySelector({
  selectedCountry,
  onCountrySelect,
  error,
  placeholder = 'Select Country',
  label = 'Country',
}: CountrySelectorProps) {
  const [isVisible, setIsVisible] = useState(false);
  const [searchQuery, setSearchQuery] = useState('');
  const mode = useThemeStore(s => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);

  const filteredCountries = COUNTRIES.filter(country =>
    country.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
    country.code.toLowerCase().includes(searchQuery.toLowerCase())
  );

  const handleSelect = (country: Country) => {
    onCountrySelect(country);
    setIsVisible(false);
    setSearchQuery('');
  };

  const styles = makeCitySelectorStyles(isDark, COLORS, error, selectedCountry);

  return (
    <View style={styles.container}>
      {label && <Text style={styles.label}>{label}</Text>}

      <TouchableOpacity
        style={styles.selector}
        onPress={() => setIsVisible(true)}
        activeOpacity={0.7}
      >
        <Text style={styles.selectorText}>
          {selectedCountry ? selectedCountry.name : placeholder}
        </Text>
        <Text style={styles.arrowIcon}>▼</Text>
      </TouchableOpacity>

      {error && <Text style={styles.errorText}>{error}</Text>}

      <Modal
        visible={isVisible}
        transparent
        animationType="slide"
        onRequestClose={() => setIsVisible(false)}
      >
        <Pressable style={styles.modal} onPress={() => setIsVisible(false)}>
          <Pressable style={styles.bottomSheet} onPress={(e) => e.stopPropagation()}>
            <View style={styles.searchContainer}>
              <TextInput
                style={styles.searchInput}
                placeholder="Search countries..."
                placeholderTextColor={COLORS.slate400}
                value={searchQuery}
                onChangeText={setSearchQuery}
                autoFocus
              />
            </View>

            <FlatList
              data={filteredCountries}
              keyExtractor={(item) => item.code}
              style={styles.listContainer}
              showsVerticalScrollIndicator={false}
              ListEmptyComponent={
                <View style={styles.emptyContainer}>
                  <Text style={styles.emptyText}>No countries found</Text>
                </View>
              }
              renderItem={({ item }) => (
                <TouchableOpacity
                  style={[
                    styles.cityItem,
                    selectedCountry?.code === item.code && styles.selectedItem,
                  ]}
                  onPress={() => handleSelect(item)}
                >
                  <Text style={styles.cityName}>{item.name}</Text>
                  <Text style={styles.cityCode}>{item.code}</Text>
                </TouchableOpacity>
              )}
            />

            <TouchableOpacity
              style={styles.closeButton}
              onPress={() => setIsVisible(false)}
            >
              <Text style={styles.closeButtonText}>Close</Text>
            </TouchableOpacity>
          </Pressable>
        </Pressable>
      </Modal>
    </View>
  );
}
