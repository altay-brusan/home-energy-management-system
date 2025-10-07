import React from 'react';
import { View, Text, Pressable } from 'react-native';
import MaterialIcons from '@react-native-vector-icons/material-icons';

type Props = {
  navigation: any;
  styles: any;
  COLORS: any;
};

export function HomeHeader({ navigation, styles, COLORS }: Props) {
  return (
    <View style={styles.headerSection}>
      <View style={styles.welcomeSection}>
        <Text style={styles.welcomeText}>Welcome User!</Text>
        <Text style={styles.subtitleText}>Manage your home energy</Text>
      </View>

      <View style={styles.actionButtons}>
        <Pressable
          onPress={() => navigation.navigate('HomeSetup')}
          style={[styles.primaryButton, styles.headerButton]}
          hitSlop={8}
        >
          <MaterialIcons
            name="home"
            size={18}
            color={COLORS.white}
            style={{ marginRight: 6 }}
          />
          <Text style={styles.primaryButtonText}>Add Home</Text>
        </Pressable>
      </View>
    </View>
  );
}
