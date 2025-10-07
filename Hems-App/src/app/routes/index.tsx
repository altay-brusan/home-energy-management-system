import FontAwesome from '@react-native-vector-icons/fontawesome';
import { createBottomTabNavigator } from '@react-navigation/bottom-tabs';
import {
  DarkTheme,
  DefaultTheme,
  NavigationContainer,
} from '@react-navigation/native';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import React from 'react';
import { useThemeStore } from '../../store/themeStore';
import { getThemeColors } from '../../theme/constants';

export default function Routes () {
  return <RootNavigator />;
}

import {
  ChangePasswordScreen,
  LoginScreen,
  ProfileInfoScreen,
  ProfileScreen,
} from '../screens';
import DashboardScreen from '../screens/dashboard/DashboardScreen';
import DeviceDetail from '../screens/deviceDetail/DeviceDetail';
import GatewayDetailScreen from '../screens/deviceDetail/GatewayDetailScreen';
import PVPanelDetailScreen from '../screens/deviceDetail/PVPanelDetailScreen';
import DeviceListScreen from '../screens/deviceList/DeviceListScreen';
import ForgotPasswordScreen from '../screens/forgotpassword/ForgotPasswordScreen';
import NotificationScreen from '../screens/notification/NotificationScreen';
import NotificationDetailScreen from '../screens/notificationDetail/NotificationDetailScreen';
import DeviceSharingScreen from '../screens/profile/DeviceSharing/DeviceSharingScreen';
import CompanyInfoScreen from '../screens/profile/CompanyInfo/CompanyInfoScreen';
import DeviceSharingDetailScreen from '../screens/profile/DeviceSharing/DeviceSharingDetail/DeviceSharingDetail';
import SignUpScreen from '../screens/signup/SignUpScreen';
import TrashScreen from '../screens/trashscreen/TrashScreen';
import HomeScreen from '../screens/home/Home';
import HomeSetupScreen from '../screens/onboarding/HomeSetupScreen';
import DeviceSetupScreen from '../screens/onboarding/DeviceSetupScreen';
import { EnergyProfileScreen } from '../screens/energyProfile';
import TroubleShootingScreen from '../screens/profile/TroubleShooting/TroubleShootingScreen';
import FaultsScreen from '../screens/profile/FaultsScreen/FaultsScreen';
import FaultsDetail from '../screens/profile/FaultsDetail/FaultDetailScreen';
const Tab = createBottomTabNavigator();
const Stack = createNativeStackNavigator();

function TabNavigator () {
  const mode = useThemeStore(s => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);

  return (
    <Tab.Navigator
      screenOptions={{
        headerShown: false,
        tabBarActiveTintColor: COLORS.accent,
        tabBarInactiveTintColor: COLORS.slate400,
        tabBarStyle: {
          backgroundColor: COLORS.surface,
          borderTopWidth: 1,
          borderTopColor: COLORS.border,
        },
      }}
    >
      <Tab.Screen
        name="Dashboard"
        component={DashboardScreen}
        options={{
          tabBarIcon: ({ color, size }) => (
            <FontAwesome name="home" color={color} size={size} />
          ),
        }}
      />
      <Tab.Screen
        name="Device"
        component={DeviceDetail}
        options={{
          tabBarIcon: ({ color, size }) => (
            <FontAwesome name="server" color={color} size={size} />
          ),
        }}
      />

      <Tab.Screen
        name="Notifications"
        component={NotificationScreen}
        options={{
          tabBarIcon: ({ color, size }) => (
            <FontAwesome name="bell" color={color} size={size} />
          ),
        }}
      />
      <Tab.Screen
        name="Profile"
        component={ProfileScreen}
        options={{
          tabBarIcon: ({ color, size }) => (
            <FontAwesome name="user" color={color} size={size} />
          ),
        }}
      />
    </Tab.Navigator>
  );
}

export function RootNavigator () {
  const mode = useThemeStore(s => s.mode);
  const isDark = mode === 'dark';
  const navTheme = isDark ? DarkTheme : DefaultTheme;

  return (
    <NavigationContainer theme={navTheme}>
      <Stack.Navigator
        screenOptions={{ headerShown: false }}
        initialRouteName="Login"
      >
        <Stack.Screen name="Login" component={LoginScreen} />
        <Stack.Screen name="SignUp" component={SignUpScreen} />
        <Stack.Screen name="ProfileInfo" component={ProfileInfoScreen} />
        <Stack.Screen name="ForgotPassword" component={ForgotPasswordScreen} />
        <Stack.Screen name="MainTabs" component={TabNavigator} />
        <Stack.Screen name="DeviceSharing" component={DeviceSharingScreen} />
        <Stack.Screen
          name="DeviceSharingDetail"
          component={DeviceSharingDetailScreen}
        />
        <Stack.Screen name="CompanyInfo" component={CompanyInfoScreen} />
        <Stack.Screen name="Home" component={HomeScreen} />
        <Stack.Screen name="HomeSetup" component={HomeSetupScreen} />
        <Stack.Screen name="DeviceSetup" component={DeviceSetupScreen} />
        <Stack.Screen
          name="Trash"
          component={TrashScreen}
          options={{ animation: 'slide_from_right' }}
        />
        <Stack.Screen
          name="NotificationDetail"
          component={NotificationDetailScreen}
          options={{ animation: 'slide_from_right' }}
        />
        <Stack.Screen
          name="PVPanelDetail"
          component={PVPanelDetailScreen}
          options={{ animation: 'slide_from_right' }}
        />
        <Stack.Screen
          name="GatewayDetail"
          component={GatewayDetailScreen}
          options={{ animation: 'slide_from_right' }}
        />
        <Stack.Screen
          name="DeviceDetail"
          component={DeviceDetail}
          options={{ animation: 'slide_from_right' }}
        />
        <Stack.Screen
          name="ChangePassword"
          component={ChangePasswordScreen}
        />
    
          <Stack.Screen
          name="EnergyProfile"
          component={EnergyProfileScreen}
          options={{ animation: 'slide_from_right' }}
        />
        <Stack.Screen
  name="Faults"
  component={FaultsScreen}
  options={{ animation: 'slide_from_right' }}
/>
<Stack.Screen
  name="FaultDetail"
  component={FaultsDetail}
  options={{ animation: 'slide_from_right' }}
/>
        <Stack.Screen
  name="TroubleShooting"
  component={TroubleShootingScreen}
  options={{ animation: 'slide_from_right' }}
/>
       
      </Stack.Navigator>
    </NavigationContainer>
  );
}
