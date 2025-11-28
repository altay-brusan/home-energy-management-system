import React from "react";
import { Text, View, Image } from "react-native";
import { useTranslation } from "react-i18next";
import { useSelectedSiteUtils } from "../../../../../../hooks/useSelectedSite";
import { useThemeStore } from "../../../../../../store/themeStore";

export function SidebarHeader({ styles }: { styles: any }) {
  const { t } = useTranslation();
  const { selectedSite, getSelectedSiteName, getSelectedSiteAddress } = useSelectedSiteUtils();
  const mode = useThemeStore((s) => s.mode);
  const isDark = mode === "dark";

  return (
    <View style={styles.headerContainer}>
      <Text style={styles.header}>{t("dashboardSidebar.header")}</Text>
      {selectedSite && (
        <View style={styles.selectedSiteInfo}>
          <View style={styles.selectedSiteTextContainer}>
            <Text style={styles.selectedSiteName}>{getSelectedSiteName()}</Text>
            <Text style={styles.selectedSiteAddress}>{getSelectedSiteAddress()}</Text>
          </View>
          <Image
            source={
              isDark
                ? require("../../../../../../assets/images/energy_home.bg.png")
                : require("../../../../../../assets/images/energy_home.png")
            }
            style={styles.selectedSiteImage}
            resizeMode="contain"
          />
        </View>
      )}
    </View>
  );
}
