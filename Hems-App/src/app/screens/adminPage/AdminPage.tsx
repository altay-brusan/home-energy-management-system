import { useNavigation } from "@react-navigation/native";
import React, { useCallback, useEffect, useMemo, useState } from "react";
import { Alert, RefreshControl, ScrollView, Text, TouchableOpacity, View } from "react-native";
import { adminService } from "../../../api/services/adminService";
import { sitesService } from "../../../api/services/sitesService";
import { locationsService } from "../../../api/services/locationsService";
import { useI18n } from "../../../hooks/useI18n";
import { useRoles } from "../../../hooks/useRoles";
import { useThemeStore } from "../../../store/themeStore";
import { useSitesStore } from "../../../store/sitesStore";
import { getThemeColors } from "../../../theme/constants";
import MainLayout from "../../layouts/MainLayout";
import { getAdminPageStyles } from "./AdminPage.styles";
import AdminHomeCard from "./components/AdminHomeCard";
import AdminLocationSelector from "./components/AdminLocationSelector";
import AdminSearchBar from "./components/AdminSearchBar";
import AdminStats from "./components/AdminStats";
import { CountryDto, CityDto } from "../../../api/types/locations.types";

export default function AdminPage () {
  const navigation = useNavigation<any>();
  const mode = useThemeStore((s) => s.mode);
  const isDark = mode === "dark";
  const COLORS = getThemeColors(isDark);
  const { t } = useI18n();
  const { isAdmin, hasRole } = useRoles();
  const { setSelectedSite } = useSitesStore();
  const styles = useMemo(() => getAdminPageStyles(COLORS, isDark), [COLORS, isDark]);

  const [homes, setHomes] = useState<any[]>([]);
  const [refreshing, setRefreshing] = useState(false);
  const [selectedCountry, setSelectedCountry] = useState<string | null>(null);
  const [selectedCity, setSelectedCity] = useState<string | null>(null);
  const [selectedEmail, setSelectedEmail] = useState<string | null>(null);
  const [totalCount, setTotalCount] = useState(0);
  const [totalDeviceCount, setTotalDeviceCount] = useState(0);
  const [currentPage, setCurrentPage] = useState(1);
  const [countries, setCountries] = useState<CountryDto[]>([]);
  const [cities, setCities] = useState<CityDto[]>([]);
  const [loadingCities, setLoadingCities] = useState(false);
  const itemsPerPage = 4;

  // Hooks'ları conditional return'tan önce çağır
  const availableCountries = useMemo(() => {
    return countries.map(country => country.name);
  }, [countries]);

  const availableCities = useMemo(() => {
    return cities.map(city => city.name);
  }, [cities]);

  const loadCountries = useCallback(async () => {
    try {
      const countriesData = await locationsService.getCountries();
      setCountries(countriesData);
    } catch (err: any) {
      if (err?.name === 'CanceledError' || err?.message === 'canceled') {
        setCountries([]);
      } else if (err?.response?.status === 401) {
        setCountries([]);
      } else {
        setCountries([]);
      }
    }
  }, []);

  useEffect(() => {
    loadCountries();
  }, [loadCountries]);

  useEffect(() => {
    const loadCities = async () => {
      if (!selectedCountry) {
        setCities([]);
        return;
      }

      setLoadingCities(true);
      try {
        const countryData = countries.find(c => c.name === selectedCountry);
        if (!countryData) {
          setCities([]);
          setLoadingCities(false);
          return;
        }
        
        const citiesData = await locationsService.getCitiesByCountry(countryData.id);
        setCities(citiesData);
      } catch (err: any) {
        if (err?.name === 'CanceledError' || err?.message === 'canceled') {
          setCities([]);
        } else if (err?.response?.status === 401) {
          setCities([]);
        } else {
          setCities([]);
        }
      } finally {
        setLoadingCities(false);
      }
    };
    loadCities();
  }, [selectedCountry, countries]);

  const fetchSites = useCallback(async () => {
    if (!selectedCountry) {
      setHomes([]);
      setTotalCount(0);
      setTotalDeviceCount(0);
      return;
    }

    try {
      const res = await adminService.getAdminSites({
        pageNumber: currentPage,
        pageSize: itemsPerPage,
        filters: {
          UserEmail: selectedEmail || undefined,
          country: selectedCountry || undefined,
          city: selectedCity || undefined,
        },
      });

      const allFilteredHomes = res.items || [];
      setHomes(allFilteredHomes);
      setTotalCount(res.totalCount || 0);
      
      const totalDevices = allFilteredHomes.filter((home: any) => home.hasDevice).length;
      setTotalDeviceCount(totalDevices);
    } catch (err) {
      setHomes([]);
      setTotalCount(0);
      setTotalDeviceCount(0);
    }
  }, [selectedCountry, selectedCity, selectedEmail, currentPage, itemsPerPage]);

  useEffect(() => {
    if (selectedCountry) {
      setCurrentPage(1);
    } else {
      setHomes([]);
      setTotalCount(0);
      setTotalDeviceCount(0);
      setCurrentPage(1);
    }
  }, [selectedCountry, selectedCity, selectedEmail]);

  useEffect(() => {
    if (selectedCountry) {
      fetchSites();
    }
  }, [currentPage, fetchSites, selectedCountry]);

  const onRefresh = useCallback(async () => {
    setRefreshing(true);
    try {
      await Promise.all([
        loadCountries(),
        selectedCountry ? fetchSites() : Promise.resolve()
      ]);
    } finally {
      setRefreshing(false);
    }
  }, [selectedCountry, fetchSites, loadCountries]);

  if (!isAdmin && !hasRole("Administrator")) {
    return (
      <MainLayout>
        <View style={styles.container}>
          <Text style={styles.welcomeText}>{t("admin.accessDenied")}</Text>
          <Text style={styles.errorText}>{t("admin.adminOnly")}</Text>
        </View>
      </MainLayout>
    );
  }

  const totalPages = Math.ceil(totalCount / itemsPerPage);

  const paginatedHomes = useMemo(() => {
    return homes;
  }, [homes, currentPage, itemsPerPage, totalCount, totalPages]);

  return (
    <MainLayout>
      <ScrollView
        style={styles.container}
        contentContainerStyle={styles.scrollContent}
        showsVerticalScrollIndicator={false}
        refreshControl={
          <RefreshControl
            refreshing={refreshing}
            onRefresh={onRefresh}
            colors={[COLORS.accent]}
            tintColor={COLORS.accent}
          />
        }
      >
        <Text style={styles.welcomeText}>{t("admin.welcome")}</Text>
        <AdminSearchBar
          COLORS={COLORS}
          styles={styles}
          homes={[]}
          onUserSelect={(email: string | null) => {
            if (email) {
              setSelectedEmail(email);
              setSelectedCountry(null);
              setSelectedCity(null);
              setCurrentPage(1);
            } else {
              setSelectedEmail(null);
            }
          }}
        />

        <View style={styles.selectorRow}>
          <AdminLocationSelector
            label={t("admin.labels.country")}
            options={availableCountries}
            selected={selectedCountry}
            onSelect={(val: string) => {
              setSelectedCountry(val);
              setSelectedCity(null);
              setSelectedEmail(null);
            }}
            COLORS={COLORS}
            styles={styles}
          />

          <AdminLocationSelector
            label={t("admin.labels.city")}
            options={availableCities}
            selected={selectedCity}
            onSelect={(val: string) => {
              setSelectedCity(val);
              setSelectedEmail(null);
            }}
            COLORS={COLORS}
            styles={styles}
            disabled={!selectedCountry || loadingCities}
          />
        </View>

        {!selectedCountry && (
          <View style={{ 
            paddingVertical: 40, 
            paddingHorizontal: 20,
            alignItems: "center",
            justifyContent: "center",
            marginTop: 20
          }}>
            <Text
              style={{
                fontSize: 16,
                color: COLORS.textSoft,
                textAlign: "center",
                lineHeight: 24,
              }}
            >
              {t("admin.messages.selectFilters")}
            </Text>
          </View>
        )}

        {selectedCountry && (
          <AdminStats 
            homes={paginatedHomes} 
            totalCount={totalCount}
            totalDeviceCount={totalDeviceCount}
            COLORS={COLORS} 
            styles={styles} 
          />
        )}

        {selectedCountry && (
          <>
            <Text style={styles.sectionTitle}>{t("admin.homesTitle")}</Text>
            <View style={styles.cardsWrapper}>
              {paginatedHomes.length === 0 ? (
            <View style={{ paddingVertical: 40, alignItems: "center" }}>
              <Text
                style={{
                  fontSize: 14,
                  color: COLORS.textSoft,
                  textAlign: "center",
                }}
              >
                {t("admin.messages.noHomesFound")}
              </Text>
            </View>
          ) : (
            paginatedHomes.map((home, index) => {
              if (!home) {
                return null;
              }
              return (
              <AdminHomeCard
                key={home?.id || `home-${index}`}
                home={home}
                COLORS={COLORS}
                styles={styles}
                onPress={async () => {
                  try {
                    if (!home?.hasDevice) {
                      Alert.alert(
                        t("admin.noDeviceTitle") || "No Device Found",
                        t("admin.noDeviceMessage") || "This site has no registered device."
                      );
                      return;
                    }

                    setSelectedSite(home);

                    navigation.navigate("MainTabs", {
                      screen: "Dashboard",
                      params: { 
                        siteId: home.id, 
                        siteData: home,
                        key: Date.now()
                      },
                    });
                  } catch (err) {
                    Alert.alert(
                      t("admin.errorTitle") || "Error",
                      t("admin.errorFetchingSite") || "Failed to navigate."
                    );
                  }
                }}
              />
              );
            })
          )}

          {totalPages > 1 && selectedCountry && (
            <View
              style={{
                flexDirection: "row",
                justifyContent: "center",
                alignItems: "center",
                marginTop: 16,
              }}
            >
              <TouchableOpacity
                disabled={currentPage === 1}
                onPress={() => {
                  setCurrentPage(Math.max(currentPage - 1, 1));
                }}
                style={{
                  backgroundColor: currentPage === 1 ? COLORS.border : COLORS.accent,
                  paddingVertical: 8,
                  paddingHorizontal: 16,
                  borderRadius: 8,
                  marginHorizontal: 8,
                }}
              >
                <Text style={{ color: "#fff" }}>{t("admin.buttons.previous")}</Text>
              </TouchableOpacity>

              <Text style={{ color: COLORS.textSoft, marginHorizontal: 8 }}>
                {currentPage} / {totalPages}
              </Text>

              <TouchableOpacity
                disabled={currentPage >= totalPages}
                onPress={() => {
                  setCurrentPage(Math.min(currentPage + 1, totalPages));
                }}
                style={{
                  backgroundColor: currentPage >= totalPages ? COLORS.border : COLORS.accent,
                  paddingVertical: 8,
                  paddingHorizontal: 16,
                  borderRadius: 8,
                  marginHorizontal: 8,
                }}
              >
                <Text style={{ color: "#fff" }}>{t("admin.buttons.next")}</Text>
              </TouchableOpacity>
            </View>
          )}
            </View>
          </>
        )}
      </ScrollView>
    </MainLayout>
  );
}
