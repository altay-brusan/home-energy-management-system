import React from "react";
import { Image, Text, TouchableOpacity, View } from "react-native";
import { Card } from "../../../../components";

export default function AdminHomeCard ({ home, COLORS, styles, onPress }: any) {
  const isOnline = home.hasDevice;

  return (
    <TouchableOpacity
      activeOpacity={isOnline ? 0.85 : 1}
      onPress={() => {
        onPress && onPress();
      }}
      disabled={!isOnline}
      style={{
        borderRadius: 14,
        overflow: "hidden",
        opacity: isOnline ? 1 : 0.6,
      }}
    >
      <Card variant="elevated" style={styles.homeCard}>
        <View style={styles.homeCardHeader}>
          <View style={styles.imageWrapper}>
            <Image
              source={require("../../../../assets/images/energy_home.bg.png")}
              style={styles.homeCardImage}
              resizeMode="contain"
            />
          </View>

          <View style={styles.homeCardContent}>
            <Text 
              style={styles.homeCardTitle} 
              numberOfLines={1}
              ellipsizeMode="tail"
            >
              {home.name}
            </Text>

            <Text 
              style={styles.homeCardAddress} 
              numberOfLines={1}
              ellipsizeMode="tail"
            >
              {[home?.city, home?.country]
                .filter(Boolean)
                .join(", ")}
            </Text>

            {home?.addressLine ? (
              <Text 
                style={styles.homeCardAddressSmall} 
                numberOfLines={2}
                ellipsizeMode="tail"
              >
                {home.addressLine}
              </Text>
            ) : null}
          </View>

          <View
            style={[
              styles.statusBadge,
              { backgroundColor: isOnline ? COLORS.success : COLORS.warning },
              styles.statusBadgeContainer,
            ]}
          >
            <Text style={styles.statusBadgeText}>
              {isOnline ? "Online" : "Offline"}
            </Text>
          </View>
        </View>

        <View style={styles.homeCardFooter}>
          <View style={styles.ownerInfoRight}>
            {home?.owner?.name && (
              <Text style={styles.homeOwnerText}>
                {`${home.owner.name || ""} ${home.owner.surname || ""}`.trim()}
              </Text>
            )}
            {home?.owner?.email && (
              <Text style={styles.homeOwnerText}>{home.owner.email}</Text>
            )}
          </View>
        </View>
      </Card>
    </TouchableOpacity>
  );
}
