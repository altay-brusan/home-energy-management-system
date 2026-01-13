import React, { useState, useEffect } from 'react';
import {
  View,
  Text,
  TouchableOpacity,
  Modal,
  FlatList,
  Pressable,
  TextInput,
  ActivityIndicator,
} from 'react-native';
import { getThemeColors } from '../../../theme/constants';
import { useThemeStore } from '../../../store/themeStore';
import { makeCitySelectorStyles } from '../CitySelector/CitySelector.styles';
import { locationsService } from '../../../api/services/locationsService';
import { CountryDto } from '../../../api/types/locations.types';

export interface Country {
  id: string;
  name: string;
}

interface CountrySelectorProps {
  selectedCountry: Country | null;
  onCountrySelect: (country: Country) => void;
  error?: string;
  placeholder?: string;
  label?: string;
}

export default function CountrySelector({
  selectedCountry,
  onCountrySelect,
  error,
  placeholder = 'Select Country',
  label = 'Country',
}: CountrySelectorProps) {
  const [isVisible, setIsVisible] = useState(false);
  const [searchQuery, setSearchQuery] = useState('');
  const [countries, setCountries] = useState<Country[]>([]);
  const [loading, setLoading] = useState(false);
  const mode = useThemeStore(s => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);

  useEffect(() => {
    loadCountries();
  }, []);

  const loadCountries = async () => {
    try {
      setLoading(true);
      const data = await locationsService.getCountries();
      const mappedCountries = data.map((country: CountryDto) => ({
        id: country.id,
        name: country.name,
      }));
      setCountries(mappedCountries);
    } catch (error) {setCountries([]);
    } finally {
      setLoading(false);
    }
  };

  const filteredCountries = countries.filter(country =>
    country.name.toLowerCase().includes(searchQuery.toLowerCase())
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

            {loading ? (
              <View style={styles.emptyContainer}>
                <ActivityIndicator color={COLORS.accent} />
                <Text style={styles.emptyText}>Loading countries...</Text>
              </View>
            ) : (
              <FlatList
                data={filteredCountries}
                keyExtractor={(item) => item.id}
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
                      selectedCountry?.id === item.id && styles.selectedItem,
                    ]}
                    onPress={() => handleSelect(item)}
                  >
                    <Text style={styles.cityName}>{item.name}</Text>
                  </TouchableOpacity>
                )}
              />
            )}

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
