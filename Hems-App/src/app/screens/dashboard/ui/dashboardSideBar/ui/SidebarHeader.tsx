import React from "react";
import { Text, View } from "react-native";
import { useTranslation } from "react-i18next";

export function SidebarHeader({ styles }: { styles: any }) {
  const { t } = useTranslation();

  return (
    <View style={styles.headerContainer}>
      <Text style={styles.header}>{t("dashboardSidebar.header")}</Text>
    </View>
  );
}
