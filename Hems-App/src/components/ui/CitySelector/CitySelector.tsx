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
import { makeCitySelectorStyles } from './CitySelector.styles';
import { locationsService } from '../../../api/services/locationsService';
import { CityDto } from '../../../api/types/locations.types';

export interface City {
    id: string;
    name: string;
    countryId: string;
}

interface CitySelectorProps {
    selectedCity: City | null;
    onCitySelect: (city: City) => void;
    selectedCountryId?: string;
    error?: string;
    placeholder?: string;
    label?: string;
}

export default function CitySelector ({
    selectedCity,
    onCitySelect,
    selectedCountryId,
    error,
    placeholder = 'Select City',
    label = 'City',
}: CitySelectorProps) {
    const [isVisible, setIsVisible] = useState(false);
    const [searchQuery, setSearchQuery] = useState('');
    const [cities, setCities] = useState<City[]>([]);
    const [loading, setLoading] = useState(false);
    const mode = useThemeStore(s => s.mode);
    const isDark = mode === 'dark';
    const COLORS = getThemeColors(isDark);

    useEffect(() => {
      if (selectedCountryId) {
        loadCities();
      } else {
        setCities([]);
      }
    }, [selectedCountryId]);

    const loadCities = async () => {
      if (!selectedCountryId) return;
      
      try {
        setLoading(true);
        const data = await locationsService.getCitiesByCountry(selectedCountryId);
        const mappedCities = data.map((city: CityDto) => ({
          id: city.id,
          name: city.name,
          countryId: selectedCountryId,
        }));
        setCities(mappedCities);
      } catch (error) {setCities([]);
      } finally {
        setLoading(false);
      }
    };

  const filteredCities = cities.filter(city =>
    city.name.toLowerCase().includes(searchQuery.toLowerCase())
  );

    const handleSelect = (city: City) => {
        onCitySelect(city);
        setIsVisible(false);
        setSearchQuery('');
    };

  const styles = makeCitySelectorStyles(isDark, COLORS, error, selectedCity);

    return (
        <View style={styles.container}>
            {label && <Text style={styles.label}>{label}</Text>}

            <TouchableOpacity
                style={styles.selector}
                onPress={() => setIsVisible(true)}
                activeOpacity={0.7}
            >
        <Text style={styles.selectorText}>
          {selectedCity ? selectedCity.name : placeholder}
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
                placeholder="Search cities..."
                placeholderTextColor={COLORS.slate400}
                value={searchQuery}
                onChangeText={setSearchQuery}
                autoFocus
              />
            </View>

            {loading ? (
              <View style={styles.emptyContainer}>
                <ActivityIndicator color={COLORS.accent} />
                <Text style={styles.emptyText}>Loading cities...</Text>
              </View>
            ) : (
              <FlatList
                data={filteredCities}
                keyExtractor={(item) => item.id}
                style={styles.listContainer}
                showsVerticalScrollIndicator={false}
                ListEmptyComponent={
                  <View style={styles.emptyContainer}>
                    <Text style={styles.emptyText}>
                      {selectedCountryId ? 'No cities found' : 'Please select a country first'}
                    </Text>
                  </View>
                }
                renderItem={({ item }) => (
                  <TouchableOpacity
                    style={[
                      styles.cityItem,
                      selectedCity?.id === item.id && styles.selectedItem,
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
