import React, { useState } from "react";
import {
  View,
  Text,
  TouchableOpacity,
  FlatList,
  Modal,
  Pressable,
  TextInput,
} from "react-native";
import MaterialIcons from "@react-native-vector-icons/material-icons";
import { useI18n } from "../../../../hooks/useI18n";

export default function AdminLocationSelector({
  label,
  options,
  selected,
  onSelect,
  COLORS,
  styles,
}: any) {
  const [isVisible, setIsVisible] = useState(false);
  const [search, setSearch] = useState("");
  const { t } = useI18n();

  const filteredOptions = options.filter((opt: string) =>
    opt.toLowerCase().includes(search.toLowerCase())
  );

  return (
    <View style={{ flex: 1 }}>
      <TouchableOpacity
        style={styles.selectorBox}
        activeOpacity={0.8}
        onPress={() => setIsVisible(true)}
      >
        <Text style={styles.selectorText}>{selected || label}</Text>
        <MaterialIcons
          name="keyboard-arrow-down"
          size={22}
          color={COLORS.textSoft}
        />
      </TouchableOpacity>

      <Modal
        visible={isVisible}
        transparent
        animationType="slide"
        onRequestClose={() => setIsVisible(false)}
      >
        <Pressable
          style={styles.modalOverlay}
          onPress={() => setIsVisible(false)}
        >
          <Pressable
            style={styles.modalContainer}
            onPress={(e) => e.stopPropagation()}
          >
            <View style={styles.modalHeader}>
              <Text style={styles.modalTitle}>
                {label} {t("adminLocationSelector.select")}
              </Text>
              <TextInput
                placeholder={`${label} ${t("adminLocationSelector.search")}...`}
                placeholderTextColor={COLORS.textSoft}
                style={styles.modalSearchInput}
                value={search}
                onChangeText={setSearch}
              />
            </View>

            <FlatList
              data={filteredOptions}
              keyExtractor={(item) => item}
              showsVerticalScrollIndicator={false}
              renderItem={({ item }) => {
                const isSelected = item === selected;
                return (
                  <TouchableOpacity
                    onPress={() => {
                      if (isSelected) {
                        onSelect(null);
                      } else {
                        onSelect(item);
                      }
                      setIsVisible(false);
                      setSearch("");
                    }}
                    style={[
                      styles.modalItem,
                      {
                        backgroundColor: isSelected
                          ? COLORS.accent + "15"
                          : "transparent",
                      },
                    ]}
                  >
                    <View
                      style={{
                        flexDirection: "row",
                        alignItems: "center",
                        justifyContent: "space-between",
                      }}
                    >
                      <Text
                        style={[
                          styles.modalItemText,
                          {
                            color: isSelected ? COLORS.accent : COLORS.textSoft,
                            fontWeight: isSelected ? "700" : "500",
                          },
                        ]}
                      >
                        {item}
                      </Text>

                      {isSelected && (
                        <MaterialIcons
                          name="check"
                          size={20}
                          color={COLORS.accent}
                        />
                      )}
                    </View>
                  </TouchableOpacity>
                );
              }}
              ListEmptyComponent={
                <Text style={styles.modalEmptyText}>
                  {t("adminLocationSelector.noResults")}
                </Text>
              }
            />

            <TouchableOpacity
              onPress={() => setIsVisible(false)}
              style={styles.modalCloseButton}
            >
              <Text style={styles.modalCloseButtonText}>
                {t("adminLocationSelector.close")}
              </Text>
            </TouchableOpacity>
          </Pressable>
        </Pressable>
      </Modal>
    </View>
  );
}
