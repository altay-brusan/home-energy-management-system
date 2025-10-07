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
import { makeCitySelectorStyles } from './CitySelector.styles';

export interface City {
    code: string;
    name: string;
    country: string;
}

interface CitySelectorProps {
    selectedCity: City | null;
    onCitySelect: (city: City) => void;
    error?: string;
    placeholder?: string;
    label?: string;
}

const CITIES: City[] = [
  { code: 'IST', name: 'Istanbul', country: 'Turkey' },
  { code: 'ANK', name: 'Ankara', country: 'Turkey' },
  { code: 'IZM', name: 'Izmir', country: 'Turkey' },
  { code: 'ANT', name: 'Antalya', country: 'Turkey' },
  { code: 'BUR', name: 'Bursa', country: 'Turkey' },
  { code: 'ADN', name: 'Adana', country: 'Turkey' },
  { code: 'KON', name: 'Konya', country: 'Turkey' },
  { code: 'GAZ', name: 'Gaziantep', country: 'Turkey' },
  { code: 'MRS', name: 'Mersin', country: 'Turkey' },
  { code: 'DYN', name: 'Diyarbakır', country: 'Turkey' },
  { code: 'KAY', name: 'Kayseri', country: 'Turkey' },
  { code: 'ESK', name: 'Eskişehir', country: 'Turkey' },
  { code: 'URF', name: 'Urfa', country: 'Turkey' },
  { code: 'MAL', name: 'Malatya', country: 'Turkey' },
  { code: 'ERZ', name: 'Erzurum', country: 'Turkey' },
];

export default function CitySelector ({
    selectedCity,
    onCitySelect,
    error,
    placeholder = 'Select City',
    label = 'City',
}: CitySelectorProps) {
    const [isVisible, setIsVisible] = useState(false);
    const [searchQuery, setSearchQuery] = useState('');
    const mode = useThemeStore(s => s.mode);
    const isDark = mode === 'dark';
    const COLORS = getThemeColors(isDark);

  const filteredCities = CITIES.filter(city =>
    city.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
    city.code.toLowerCase().includes(searchQuery.toLowerCase())
  );

  console.log('CITIES:', CITIES.length);
  console.log('filteredCities:', filteredCities.length);
  console.log('isVisible:', isVisible);
  console.log('searchQuery:', searchQuery);

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
              <Text style={styles.debugText}>
                Debug: {filteredCities.length} cities found
              </Text>
            </View>

            <FlatList
              data={filteredCities}
              keyExtractor={(item) => item.code}
              style={styles.listContainer}
              showsVerticalScrollIndicator={false}
              ListEmptyComponent={
                <View style={styles.emptyContainer}>
                  <Text style={styles.emptyText}>
                    No cities found
                  </Text>
                </View>
              }
              renderItem={({ item, index }) => {
                console.log(`Rendering item ${index}:`, item.name);
                return (
                  <TouchableOpacity
                    style={[
                      styles.cityItem,
                      selectedCity?.code === item.code && styles.selectedItem,
                    ]}
                    onPress={() => handleSelect(item)}
                  >
                    <Text style={styles.cityName}>{item.name}</Text>
                    <Text style={styles.cityCode}>{item.code}</Text>
                    <Text style={styles.cityCountry}>{item.country}</Text>
                  </TouchableOpacity>
                );
              }}
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
