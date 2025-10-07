import React from "react";
import { View } from "react-native";
import Button from "../../../../../../components/ui/Button/Button";

export function SidebarAddButton({ styles }: { styles: any }) {
  return (
    <View>
      <Button
        title="Add House"
        onPress={() => console.log("Add house pressed")}
        variant="primary"
        style={styles.addButton}
      />
    </View>
  );
}
