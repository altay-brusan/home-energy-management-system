import { createBottomTabNavigator } from '@react-navigation/bottom-tabs';
import {
  DarkTheme,
  DefaultTheme,
  NavigationContainer,
  NavigationContainerRef,
} from '@react-navigation/native';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import React, { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { ActivityIndicator, View } from 'react-native';
import { Home, Smartphone, User, Shield, Settings, Bell } from 'lucide-react-native';
import { apiClient } from '../../api/client/apiClient';
import { invitationsService } from '../../api/services/devicesharingService';
import { InvitationModal } from '../../components/InvitationModal/InvitationModal';
import { useRoles } from '../../hooks/useRoles';
import { getUserData, removeUserData } from '../../lib/storage/token';
import { useAuthStore } from '../../store/authStore';
import { useInvitationStore } from '../../store/invitationStore';
import { useSitesStore } from '../../store/sitesStore';
import { useThemeStore } from '../../store/themeStore';
import { getThemeColors } from '../../theme/constants';
import { toast } from '../../utils/toast';

export const navigationRef = React.createRef<NavigationContainerRef<any>>();

export function navigate (name: string, params?: any) {
  navigationRef.current?.navigate(name, params);
}

export function replace (name: string, params?: any) {
  navigationRef.current?.reset({
    index: 0,
    routes: [{ name, params }],
  });
}

export default function Routes () {
  return <RootNavigator />;
}

import {
  ChangePasswordScreen,
  ForgotPasswordScreen,
  LoginScreen,
  ProfileInfoScreen,
  ProfileScreen,
  ResetPasswordScreen,
} from '../screens';
import { AdminPage } from '../screens/adminPage';
import DashboardScreen from '../screens/dashboard/DashboardScreen';
import PowerMetricsDetailScreen from '../screens/powerMetricsDetail/PowerMetricsDetailScreen';
import EnergyRevenueDetailScreen from '../screens/energyRevenueDetail/EnergyRevenueDetailScreen';
import DeviceDetail from '../screens/deviceDetail/DeviceDetail';
import GatewayDetailScreen from '../screens/deviceDetail/GatewayDetailScreen';
import PVPanelDetailScreen from '../screens/deviceDetail/PVPanelDetailScreen';
import BatteryListScreen from '../screens/deviceDetail/BatteryListScreen';
import BatteryPackDetailScreen from '../screens/deviceDetail/BatteryPackDetailScreen';
import InverterDetailScreen from '../screens/deviceDetail/InverterDetailScreen';
import SmartMeterDetailScreen from '../screens/deviceDetail/SmartMeterDetailScreen';
import DeviceAddScreen from '../screens/devices/DeviceAddScreen';
import { EnergyProfileScreen } from '../screens/energyProfile';
import { BatteryLevelSettingsScreen } from "../screens/energyProfile/ui/BatteryLevel";
import { SolarSourcePriorityScreen } from "../screens/energyProfile/ui/SolarSourcePriority";
import HomeScreen from '../screens/home/Home';
import NotificationScreen from '../screens/notification/NotificationScreen';
import NotificationDetailScreen from '../screens/notificationDetail/NotificationDetailScreen';
import DeviceSetupScreen from '../screens/onboarding/DeviceSetupScreen';
import HomeSetupScreen from '../screens/onboarding/HomeSetupScreen';
import CompanyInfoScreen from '../screens/profile/CompanyInfo/CompanyInfoScreen';
import DeviceSharingDetailScreen from '../screens/profile/DeviceSharing/DeviceSharingDetail/DeviceSharingDetail';
import DeviceSharingScreen from '../screens/profile/DeviceSharing/DeviceSharingScreen';
import FaultsDetail from '../screens/profile/FaultsDetail/FaultDetailScreen';
import FaultsScreen from '../screens/profile/FaultsScreen/FaultsScreen';
import TroubleShootingScreen from '../screens/profile/TroubleShooting/TroubleShootingScreen';
import SignUpScreen from '../screens/signup/SignUpScreen';
import TrashScreen from '../screens/trashscreen/TrashScreen';
import { CustomTabBar } from '../../components/CustomTabBar';
const Tab = createBottomTabNavigator();
const Stack = createNativeStackNavigator();

function TabNavigator () {
  const mode = useThemeStore(s => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);
  const { t } = useTranslation();
  const { isAdmin, hasRole } = useRoles();
  const { setSelectedSite, selectedSite } = useSitesStore();
  const { user } = useAuthStore();

  // Admin'den gelen site seçimini handle et
  useEffect(() => {
    const unsubscribe = navigationRef.current?.addListener('state', () => {
      const state = navigationRef.current?.getRootState();
      const route: any = state?.routes[state.index];
      if (route?.name === 'MainTabs' && route.params?.siteId) {
        const { siteId, siteData } = route.params;
        if (siteData) {
          setSelectedSite(siteData);
        }
      }
    });

    return unsubscribe;
  }, [setSelectedSite]);

  // User state değiştiğinde TabNavigator yeniden mount olmalı
  // User ID ve admin rolünü key'e dahil et
  const isAdminUser = (isAdmin || hasRole('Administrator')) && user;
  const navigatorKey = user ? (isAdminUser ? `admin-${user.id}` : `user-${user.id}`) : 'default';

  // Admin kullanıcı ev seçmemişse diğer tab'ları göster
  const hasSelectedSite = !!selectedSite;

  return (
    <Tab.Navigator
      key={navigatorKey}
      initialRouteName={isAdminUser ? 'Admin' : 'Dashboard'}
      tabBar={(props) => <CustomTabBar {...props} />}
      screenOptions={{
        headerShown: false,
        tabBarActiveTintColor: COLORS.accent,
        tabBarInactiveTintColor: COLORS.slate400,
      }}
    >
      {/* Admin kullanıcı ev seçmemişse sadece Admin ve Profile tab'larını göster */}
      {(!isAdminUser || hasSelectedSite) && (
        <>
          <Tab.Screen
            name="Dashboard"
            component={DashboardScreen}
            options={{
              tabBarLabel: t('tabs.dashboard'),
              tabBarIcon: ({ color, size }) => (
                <Home color={color} size={size} strokeWidth={2} />
              ),
            }}
          />
          <Tab.Screen
            name="Device"
            component={DeviceDetail}
            options={{
              tabBarLabel: t('tabs.device'),
              tabBarIcon: ({ color, size }) => (
                <Smartphone color={color} size={size} strokeWidth={2} />
              ),
            }}
          />
        </>
      )}

      {/* Profile her zaman göster */}
      <Tab.Screen
        name="Profile"
        component={ProfileScreen}
        options={{
          tabBarLabel: t('tabs.profile'),
          tabBarIcon: ({ color, size }) => (
            <User color={color} size={size} strokeWidth={2} />
          ),
        }}
      />

      {/* Settings tab'ı her zaman göster */}
      <Tab.Screen
        name="Settings"
        component={EnergyProfileScreen}
        options={{
          tabBarLabel: t('tabs.settings'),
          tabBarIcon: ({ color, size }) => (
            <Settings color={color} size={size} strokeWidth={2} />
          ),
        }}
      />

      {/* Admin tab'ı sadece admin kullanıcılar için */}
      {(isAdmin || hasRole('Administrator')) && (
        <Tab.Screen
          name="Admin"
          component={AdminPage}
          options={{
            tabBarLabel: ('Admin'),
            tabBarIcon: ({ color, size }) => (
              <Shield color={color} size={size} strokeWidth={2} />
            ),
          }}
        />
      )}

    </Tab.Navigator>
  );
}

export function RootNavigator () {
  const { t } = useTranslation();
  const mode = useThemeStore(s => s.mode);
  const isDark = mode === 'dark';
  const navTheme = isDark ? DarkTheme : DefaultTheme;
  const COLORS = getThemeColors(isDark);
  const { user, setUser, clearAuth } = useAuthStore();
  const { getUserSites } = useSitesStore();
  const {
    pendingInvitationCode,
    isInvitationModalVisible,
    showInvitationModal,
    hideInvitationModal,
    clearPendingInvitation
  } = useInvitationStore();
  const [isLoading, setIsLoading] = useState(true);
  const [initialRoute, setInitialRoute] = useState<'Login' | 'Home' | 'MainTabs'>('Login');

  // Invitation modal logic
  const handleAcceptInvitation = async () => {
    if (!pendingInvitationCode) return;
    try {
      if (!user) {
        toast.error(t('invitation.notLoggedIn'), t('auth.loginTitle'));
        hideInvitationModal();
        return;
      }

      await invitationsService.acceptInvitation({ token: pendingInvitationCode });

      // Ev listesini yenile
      await getUserSites();

      toast.success(t('invitation.accepted'), t('invitation.homeListUpdated'));
      clearPendingInvitation();
    } catch (error: any) {
      toast.error(t('invitation.acceptFailed'), error?.response?.data?.message || t('common.error'));
    }
  };

  const handleDeclineInvitation = () => {
    clearPendingInvitation();
  };

  useEffect(() => {
    let timer: ReturnType<typeof setTimeout> | null = null;

    timer = setTimeout(() => {
      if (user && pendingInvitationCode && !isInvitationModalVisible) {
        showInvitationModal(pendingInvitationCode);
      } else if (!user && pendingInvitationCode) {
        toast.warning(t('invitation.codeReceived'), t('invitation.notLoggedIn'));
      }
    }, 400);

    return () => {
      if (timer) clearTimeout(timer);
    };
  }, [user, pendingInvitationCode, isInvitationModalVisible, showInvitationModal, t]);

  useEffect(() => {
    const checkAuthAndHomes = async () => {
      try {
        const accessToken = await apiClient.getAccessToken();
        const storedUser = await getUserData();

        if (accessToken && storedUser) {
          const isExpired = await apiClient.isTokenExpired();

          if (!isExpired) {
            setUser(storedUser);
            const hasAdminRole = storedUser?.roles?.includes('Administrator');
            setInitialRoute('MainTabs');
          } else {
            await apiClient.clearTokens();
            await removeUserData();
            await clearAuth();
            setInitialRoute('Login');
          }
        } else {
          setInitialRoute('Login');
        }
      } catch (error) {
        setInitialRoute('Login');
      } finally {
        setIsLoading(false);
      }
    };

    checkAuthAndHomes();
  }, [setUser]);

  if (isLoading) {
    return (
      <View style={{ flex: 1, justifyContent: 'center', alignItems: 'center', backgroundColor: COLORS.dark }}>
        <ActivityIndicator size="large" color={COLORS.accent} />
      </View>
    );
  }

  // Auth durumuna göre screens
  const isAuthenticated = !!user;

  return (
    <NavigationContainer ref={navigationRef} theme={navTheme}>
      <Stack.Navigator
        screenOptions={{ headerShown: false }}
        initialRouteName={initialRoute}
      >
        {/* Public screens - Login olmayanlar da erişebilir */}
        <Stack.Screen name="Login" component={LoginScreen} />
        <Stack.Screen name="SignUp" component={SignUpScreen} />
        <Stack.Screen name="ForgotPassword" component={ForgotPasswordScreen} />
        <Stack.Screen name="ResetPassword" component={ResetPasswordScreen} />

        {/* Protected screens - Sadece authenticated kullanıcılar */}
        {isAuthenticated && (
          <>
            <Stack.Screen name="ProfileInfo" component={ProfileInfoScreen} />
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
            <Stack.Screen name="DeviceAdd" component={DeviceAddScreen} />
            <Stack.Screen
              name="PowerMetricsDetail"
              component={PowerMetricsDetailScreen}
              options={{ animation: 'slide_from_right' }}
            />
            <Stack.Screen
              name="EnergyRevenueDetail"
              component={EnergyRevenueDetailScreen}
              options={{ animation: 'slide_from_right' }}
            />
            <Stack.Screen
              name="Trash"
              component={TrashScreen}
              options={{ animation: 'slide_from_right' }}
            />
            <Stack.Screen
              name="Notifications"
              component={NotificationScreen}
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
              name="BatteryList"
              component={BatteryListScreen}
              options={{ animation: 'slide_from_right' }}
            />
            <Stack.Screen
              name="BatteryPackDetail"
              component={BatteryPackDetailScreen}
              options={{ animation: 'slide_from_right' }}
            />
            <Stack.Screen
              name="InverterDetail"
              component={InverterDetailScreen}
              options={{ animation: 'slide_from_right' }}
            />
            <Stack.Screen
              name="SmartMeterDetail"
              component={SmartMeterDetailScreen}
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
            <Stack.Screen
              name="BatteryLevelSettings"
              component={BatteryLevelSettingsScreen}
              options={{ animation: 'slide_from_right' }}
            />
            <Stack.Screen
              name="SolarSourcePriority"
              component={SolarSourcePriorityScreen}
              options={{ animation: 'slide_from_right' }}
            />
          </>
        )}
      </Stack.Navigator>

      {/* Invitation Modal */}
      <InvitationModal
        visible={isInvitationModalVisible}
        invitationCode={pendingInvitationCode || ''}
        onAccept={handleAcceptInvitation}
        onDecline={handleDeclineInvitation}
      />
    </NavigationContainer>
  );
}
