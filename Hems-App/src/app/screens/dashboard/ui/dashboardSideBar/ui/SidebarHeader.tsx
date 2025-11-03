import React from "react";
import { Text, View } from "react-native";
import { useTranslation } from "react-i18next";
import { useSelectedSiteUtils } from "../../../../../../hooks/useSelectedSite";
export function SidebarHeader({ styles }: { styles: any }) {
  const { t } = useTranslation();
  const { selectedSite, getSelectedSiteName, getSelectedSiteAddress } = useSelectedSiteUtils();

  return (
    <View style={styles.headerContainer}>
      <Text style={styles.header}>{t("dashboardSidebar.header")}</Text>
      {selectedSite && (
        <View style={styles.selectedSiteInfo}>
          <Text style={styles.selectedSiteName}>{getSelectedSiteName()}</Text>
          <Text style={styles.selectedSiteAddress}>{getSelectedSiteAddress()}</Text>
        </View>
      )}
    </View>
  );
}
