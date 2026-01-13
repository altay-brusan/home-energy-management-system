import React from "react";
import { View } from "react-native";
import Button from "../../../../../../components/ui/Button/Button";
import { useTranslation } from "react-i18next";

type Props = {
  styles: any;
  navigation: any;
};

export function SidebarAddButton({ styles, navigation }: Props) {
  const { t } = useTranslation();

  return (
    <View>
      <Button
        title={t("dashboardSidebar.addButton")}
        onPress={() => navigation.navigate("HomeSetup")}
        variant="primary"
        style={styles.addButton}
      />
    </View>
  );
}
