import React, { useState, useMemo, useEffect } from "react";
import { View, TextInput, TouchableOpacity, ScrollView, Text, ActivityIndicator } from "react-native";
import MaterialIcons from "@react-native-vector-icons/material-icons";
import { useI18n } from "../../../../hooks/useI18n";
import { sitesService } from "../../../../api/services/sitesService"; 

interface AdminSearchBarProps {
  COLORS: any;
  styles: any;
  homes: { owner: { email: string } }[];
  onUserSelect: (email: string | null) => void;
}


export default function AdminSearchBar({
  COLORS,
  styles,
  homes,
  onUserSelect,
}: AdminSearchBarProps) {
  const [searchQuery, setSearchQuery] = useState("");
  const [results, setResults] = useState<string[]>([]);
  const [loading, setLoading] = useState(false);
  const [selectedEmail, setSelectedEmail] = useState<string | null>(null);
  const { t } = useI18n();

  const localUsers: string[] = useMemo(() => {
    const emails = homes.map((h) => h?.owner?.email).filter((e): e is string => !!e);
    return Array.from(new Set(emails));
  }, [homes]);

  const handleSearch = async (text: string) => {
    setSearchQuery(text);

    if (text.trim().length === 0) {
      setResults([]);
      setSelectedEmail(null);
      onUserSelect(null);
      return;
    }

    setLoading(true);

    try {
      const res = await sitesService.getAll({
        PageNumber: 1,
        PageSize: 5,
        UserEmail: text,
      } as any);

      const apiEmails =
        res?.items
          ?.map((i: any) => i?.owner?.email)
          ?.filter((e: any): e is string => !!e) || [];

      const merged = Array.from(new Set([...localUsers, ...apiEmails]));

      const filtered = merged.filter((u) =>
        u.toLowerCase().includes(text.toLowerCase())
      );

      setResults(filtered);
    } catch (err) {
    } finally {
      setLoading(false);
    }
  };

  const handleSelect = (email: string) => {
    setSelectedEmail(email);
    setSearchQuery(email);
    setResults([]);
    onUserSelect(email); 
  };

  return (
    <View style={styles.searchContainer}>
      <View style={styles.searchInputWrapper}>
    <TextInput
  style={styles.searchInput}
  placeholder={t("adminSearchBar.placeholder")}
  placeholderTextColor={COLORS.textSoft}
  value={searchQuery}
  onChangeText={handleSearch}
  onSubmitEditing={() => {
    if (searchQuery.trim().length > 0) {
      onUserSelect(searchQuery.trim());
    }
  }}
/>
      <TouchableOpacity
 style={styles.searchIconWrapper}
onPress={() => {
              if (searchQuery.trim().length > 0) {
                onUserSelect(searchQuery.trim());
              }
            }}
 >
<MaterialIcons name="search" size={22} color={COLORS.textSoft} />
</TouchableOpacity>
      </View>

      {loading && (
        <View
          style={{
            paddingVertical: 10,
            alignItems: "center",
          }}
        >
          <ActivityIndicator color={COLORS.accent} size="small" />
        </View>
      )}

      {!loading && results.length > 0 && (
        <ScrollView
          style={styles.searchResultList}
          keyboardShouldPersistTaps="handled"
          nestedScrollEnabled
        >
          {results.map((item) => {
            const isSelected = selectedEmail === item;
            return (
              <TouchableOpacity
                key={item}
                style={[
                  styles.searchResultItem,
                  {
                    backgroundColor: isSelected
                      ? COLORS.accent + "15"
                      : "transparent",
                  },
                ]}
                onPress={() => handleSelect(item)}
              >
                <Text
                  style={[
                    styles.searchResultText,
                    {
                      color: isSelected ? COLORS.accent : COLORS.textSoft,
                      fontWeight: isSelected ? "700" : "500",
                    },
                  ]}
                >
                  {item}
                </Text>
              </TouchableOpacity>
            );
          })}
        </ScrollView>
      )}
    </View>
  );
}
