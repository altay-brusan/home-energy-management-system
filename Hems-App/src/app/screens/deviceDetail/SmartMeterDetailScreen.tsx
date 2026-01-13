import { useNavigation } from "@react-navigation/native";
import { ArrowLeft, Zap } from 'lucide-react-native';
import React from "react";
import {
  Pressable,
  ScrollView,
  StyleSheet,
  Text,
  View
} from "react-native";
import { useSafeAreaInsets } from "react-native-safe-area-context";
import { useI18n } from "../../../hooks/useI18n";
import { useThemeStore } from "../../../store/themeStore";
import { getThemeColors, SPACING, TYPOGRAPHY } from "../../../theme/constants";

export default function SmartMeterDetailScreen() {
  const navigation = useNavigation<any>();
  const isDark = useThemeStore((s) => s.mode) === "dark";
  const COLORS = getThemeColors(isDark);
  const insets = useSafeAreaInsets();
  const { t } = useI18n();

  return (
    <View
      style={[
        styles.container,
        {
          backgroundColor: COLORS.dark,
          paddingTop: insets.top,
        },
      ]}
    >
      {/* Header */}
      <View
        style={[
          styles.header,
          {
            backgroundColor: COLORS.dark,
          },
        ]}
      >
        <Pressable
          style={styles.backButton}
          onPress={() => navigation.goBack()}
        >
          <ArrowLeft size={24} color={COLORS.textSoft} strokeWidth={2} />
        </Pressable>
        <Text
          style={[
            styles.headerTitle,
            {
              color: COLORS.textSoft,
            },
          ]}
        >
          {t("smartMeterDetail.title")}
        </Text>
        <View style={{ width: 40 }} />
      </View>

      {/* Content */}
      <ScrollView
        contentContainerStyle={[
          styles.content,
          {
            paddingBottom: insets.bottom + 20,
          },
        ]}
        showsVerticalScrollIndicator={false}
      >
        {/* Placeholder Card */}
        <View
          style={[
            styles.placeholderCard,
            {
              backgroundColor: COLORS.surface,
              borderColor: COLORS.border,
            },
          ]}
        >
          <View
            style={[
              styles.iconContainer,
              {
                backgroundColor: `${COLORS.success}15`,
              },
            ]}
          >
            <Zap size={64} color={COLORS.success} opacity={0.5} />
          </View>
          <Text
            style={[
              styles.placeholderTitle,
              {
                color: COLORS.text,
              },
            ]}
          >
            {t("smartMeterDetail.comingSoon")}
          </Text>
          <Text
            style={[
              styles.placeholderText,
              {
                color: COLORS.textSoft,
              },
            ]}
          >
            {t("smartMeterDetail.description")}
          </Text>
        </View>

        {/* Info Section */}
        <View
          style={[
            styles.section,
            {
              backgroundColor: COLORS.surface,
              borderColor: COLORS.border,
            },
          ]}
        >
          <Text
            style={[
              styles.sectionTitle,
              {
                color: COLORS.text,
              },
            ]}
          >
            {t("smartMeterDetail.features.title")}
          </Text>

          <View style={styles.featureItem}>
            <View
              style={[
                styles.featureDot,
                {
                  backgroundColor: COLORS.accent,
                },
              ]}
            />
            <Text
              style={[
                styles.featureText,
                {
                  color: COLORS.textSoft,
                },
              ]}
            >
              {t("smartMeterDetail.features.realTimeMonitoring")}
            </Text>
          </View>

          <View style={styles.featureItem}>
            <View
              style={[
                styles.featureDot,
                {
                  backgroundColor: COLORS.accent,
                },
              ]}
            />
            <Text
              style={[
                styles.featureText,
                {
                  color: COLORS.textSoft,
                },
              ]}
            >
              {t("smartMeterDetail.features.energyConsumption")}
            </Text>
          </View>

          <View style={styles.featureItem}>
            <View
              style={[
                styles.featureDot,
                {
                  backgroundColor: COLORS.accent,
                },
              ]}
            />
            <Text
              style={[
                styles.featureText,
                {
                  color: COLORS.textSoft,
                },
              ]}
            >
              {t("smartMeterDetail.features.costAnalysis")}
            </Text>
          </View>

          <View style={styles.featureItem}>
            <View
              style={[
                styles.featureDot,
                {
                  backgroundColor: COLORS.accent,
                },
              ]}
            />
            <Text
              style={[
                styles.featureText,
                {
                  color: COLORS.textSoft,
                },
              ]}
            >
              {t("smartMeterDetail.features.historicalData")}
            </Text>
          </View>
        </View>
      </ScrollView>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
  },
  header: {
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
    paddingHorizontal: SPACING.md,
    paddingVertical: SPACING.sm,
  },
  backButton: {
    padding: 8,
  },
  headerTitle: {
    fontSize: TYPOGRAPHY.lg,
    fontWeight: "700",
    flex: 1,
    textAlign: "center",
  },
  content: {
    padding: SPACING.md,
  },
  placeholderCard: {
    borderRadius: 16,
    borderWidth: 0,
    padding: SPACING.xl,
    alignItems: "center",
    marginBottom: SPACING.md,
  },
  iconContainer: {
    width: 120,
    height: 120,
    borderRadius: 60,
    alignItems: "center",
    justifyContent: "center",
    marginBottom: SPACING.md,
  },
  placeholderTitle: {
    fontSize: TYPOGRAPHY.xl,
    fontWeight: "800",
    marginBottom: SPACING.sm,
    textAlign: "center",
  },
  placeholderText: {
    fontSize: TYPOGRAPHY.sm,
    textAlign: "center",
    lineHeight: 20,
  },
  section: {
    borderRadius: 14,
    borderWidth: 0,
    padding: SPACING.md,
    marginBottom: SPACING.sm,
  },
  sectionTitle: {
    fontSize: TYPOGRAPHY.md,
    fontWeight: "700",
    marginBottom: SPACING.md,
  },
  featureItem: {
    flexDirection: "row",
    alignItems: "center",
    marginBottom: SPACING.sm,
  },
  featureDot: {
    width: 8,
    height: 8,
    borderRadius: 4,
    marginRight: SPACING.sm,
  },
  featureText: {
    fontSize: TYPOGRAPHY.sm,
    flex: 1,
  },
});
