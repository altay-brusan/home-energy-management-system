import { createBottomTabNavigator } from '@react-navigation/bottom-tabs';
import {
  DarkTheme,
  DefaultTheme,
  NavigationContainer,
  NavigationContainerRef,
} from '@react-navigation/native';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import React, { useEffect, useState, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { ActivityIndicator, View } from 'react-native';
import { Home, Smartphone, User, Shield, Settings, Bell } from 'lucide-react-native';
import { apiClient } from '../../api/client/apiClient';
import { invitationsService } from '../../api/services/devicesharingService';
import { versionService } from '../../api/services/versionService';
import { AppVersionCheckResponse } from '../../api/types/version.types';
import { InvitationModal } from '../../components/InvitationModal/InvitationModal';
import { useRoles } from '../../hooks/useRoles';
import { getUserData, removeUserData } from '../../lib/storage/token';
import { useAuthStore } from '../../store/authStore';
import { useInvitationStore } from '../../store/invitationStore';
import { useSitesStore, useHasValidSiteWithDevice } from '../../store/sitesStore';
import { useThemeStore } from '../../store/themeStore';
import { getThemeColors } from '../../theme/constants';
import { toast } from '../../utils/toast';
import { UpdateRequiredScreen } from '../screens/updateRequired';

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
import GridPerformanceDetailScreen from '../screens/gridPerformanceDetail/GridPerformanceDetailScreen';
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
import {
  InverterModesScreen,
  ModeDetailScreen,
  CustomModeScreen,
  TimeOfUseScreen,
} from '../screens/inverterModes';
import MaintenanceSettingsScreen from '../screens/settings/MaintenanceSettingsScreen';
import SettingsScreen from '../screens/settings/SettingsScreen';
import GeneralSettingsScreen from '../screens/settings/GeneralSettingsScreen';
import PricingSettingsScreen from '../screens/settings/PricingSettingsScreen';
import DeviceSettingsScreen from '../screens/settings/DeviceSettingsScreen';
import ResetSettingsScreen from '../screens/settings/ResetSettingsScreen';
import ProceduresScreen from '../screens/settings/ProceduresScreen';
import BatteryRecoveryScreen from '../screens/settings/BatteryRecoveryScreen';
import HomeScreen from '../screens/home/Home';
import NotificationScreen from '../screens/notification/NotificationScreen';
import NotificationDetailScreen from '../screens/notificationDetail/NotificationDetailScreen';
import DeviceSetupScreen from '../screens/onboarding/DeviceSetupScreen';
import HomeSetupScreen from '../screens/onboarding/HomeSetupScreen';
import JoinOrCreateScreen from '../screens/onboarding/JoinOrCreateScreen';
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
  const { setSelectedSite, selectedSite, sites } = useSitesStore();
  const { user } = useAuthStore();
  const hasValidSiteWithDevice = useHasValidSiteWithDevice();
  const isAdminUserCheck = isAdmin || hasRole('Administrator');

  // Site'ı olmayan kullanıcıları JoinOrCreate'e yönlendir
  useEffect(() => {
    if (!isAdminUserCheck && sites.length === 0) {
      navigationRef.current?.reset({
        index: 0,
        routes: [{ name: 'JoinOrCreate' }],
      });
    }
  }, [sites, isAdminUserCheck]);

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
  // Include hasValidSiteWithDevice in key to re-render tabs when user completes setup
  const navigatorKey = user
    ? (isAdminUser
        ? `admin-${user.id}-${selectedSite?.id || 'nosite'}`
        : `user-${user.id}-${hasValidSiteWithDevice ? 'valid' : 'setup'}`)
    : 'default';

  // Show Dashboard/Device tabs only if user has a valid site with device
  // For admin users, they need to select a site first
  // For regular users, they need to complete site+device setup
  const canShowDashboard = isAdminUser ? !!selectedSite : hasValidSiteWithDevice;

  // Show MyHomes tab if user has sites but no device yet
  const hasSiteWithoutDevice = !isAdminUserCheck && sites.length > 0 && !hasValidSiteWithDevice;

  // Determine initial route
  const getInitialRoute = () => {
    if (isAdminUser) return 'Admin';
    if (canShowDashboard) return 'Dashboard';
    if (hasSiteWithoutDevice) return 'MyHomes';
    return 'Profile';
  };

  return (
    <Tab.Navigator
      key={navigatorKey}
      initialRouteName={getInitialRoute()}
      tabBar={(props) => <CustomTabBar {...props} />}
      screenOptions={{
        headerShown: false,
        tabBarActiveTintColor: COLORS.accent,
        tabBarInactiveTintColor: COLORS.slate400,
      }}
    >
      {/* Show MyHomes tab if user has sites but no device */}
      {hasSiteWithoutDevice && (
        <Tab.Screen
          name="MyHomes"
          component={HomeScreen}
          options={{
            tabBarLabel: t('profile.myHomes'),
            tabBarIcon: ({ color, size }) => (
              <Home color={color} size={size} strokeWidth={2} />
            ),
          }}
        />
      )}

      {/* Show Dashboard/Device only if user has valid site with device */}
      {canShowDashboard && (
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
        component={SettingsScreen}
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
  const { user, setUser, clearAuth, hasRole } = useAuthStore();
  const { getUserSites, setSelectedSite, sites: currentSites } = useSitesStore();
  const {
    pendingInvitationCode,
    isInvitationModalVisible,
    showInvitationModal,
    hideInvitationModal,
    clearPendingInvitation
  } = useInvitationStore();
  const [isLoading, setIsLoading] = useState(true);
  const [initialRoute, setInitialRoute] = useState<'Login' | 'Home' | 'MainTabs' | 'HomeSetup' | 'JoinOrCreate'>('Login');

  // Version check state
  const [versionCheck, setVersionCheck] = useState<AppVersionCheckResponse | null>(null);
  const [showUpdateScreen, setShowUpdateScreen] = useState(false);

  // Skip update handler for optional updates
  const handleSkipUpdate = useCallback(() => {
    setShowUpdateScreen(false);
  }, []);

  // Invitation modal logic
  const handleAcceptInvitation = async () => {
    if (!pendingInvitationCode) return;
    try {
      if (!user) {
        toast.error(t('invitation.notLoggedIn'), t('auth.loginTitle'));
        hideInvitationModal();
        return;
      }

      // Store current site IDs to find the new one after refresh
      const previousSiteIds = new Set(currentSites.map(s => s.id));

      await invitationsService.acceptInvitation({ token: pendingInvitationCode });

      // Ev listesini yenile
      const updatedDashboard = await getUserSites();
      const updatedSites = updatedDashboard?.siteList || [];

      // Find the newly added site (the one that wasn't in the previous list)
      const newSite = updatedSites.find(s => !previousSiteIds.has(s.id));

      if (newSite) {
        // Select the newly added site
        setSelectedSite(newSite);

        // Navigate to MainTabs (Dashboard)
        navigationRef.current?.reset({
          index: 0,
          routes: [{ name: 'MainTabs' }],
        });
      } else if (updatedSites.length > 0) {
        // Fallback: select the first site if we couldn't find the new one
        setSelectedSite(updatedSites[0]);
        navigationRef.current?.reset({
          index: 0,
          routes: [{ name: 'MainTabs' }],
        });
      }

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
    const checkVersionAndAuth = async () => {
      try {
        // Step 1: Check app version (public endpoint, no auth required)
        try {
          const versionResult = await versionService.checkVersion();
          setVersionCheck(versionResult);

          // If update is required, show update screen and stop
          if (versionResult.isUpdateRequired) {
            setShowUpdateScreen(true);
            setIsLoading(false);
            return;
          }

          // If optional update is available, show update screen but allow skipping
          if (versionResult.isUpdateAvailable) {
            setShowUpdateScreen(true);
            // Continue with auth check - user can skip the update
          }
        } catch (versionError) {
          // Version check failed - continue without blocking
          console.log('[RootNavigator] Version check failed, continuing:', versionError);
        }

        // Step 2: Check authentication
        const accessToken = await apiClient.getAccessToken();
        const storedUser = await getUserData();

        if (accessToken && storedUser) {
          const isExpired = await apiClient.isTokenExpired();

          if (!isExpired) {
            setUser(storedUser);
            const hasAdminRole = storedUser?.roles?.includes('Administrator');

            // Pre-fetch sites to determine correct initial route
            try {
              const sitesDashboard = await getUserSites();
              const userSites = sitesDashboard?.siteList || [];

              // Admin users always go to MainTabs
              if (hasAdminRole) {
                setInitialRoute('MainTabs');
              } else if (userSites.length === 0) {
                // No sites - show JoinOrCreate screen to let user choose
                setInitialRoute('JoinOrCreate');
              } else {
                // Has sites - go to MainTabs
                setInitialRoute('MainTabs');
              }
            } catch (sitesError) {
              // Sites fetch failed - go to JoinOrCreate to be safe
              console.log('[RootNavigator] Sites fetch error:', sitesError);
              if (hasAdminRole) {
                setInitialRoute('MainTabs');
              } else {
                setInitialRoute('JoinOrCreate');
              }
            }
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

    checkVersionAndAuth();
  }, [setUser, getUserSites]);

  if (isLoading) {
    return (
      <View style={{ flex: 1, justifyContent: 'center', alignItems: 'center', backgroundColor: COLORS.dark }}>
        <ActivityIndicator size="large" color={COLORS.accent} />
      </View>
    );
  }

  // Show update required/available screen
  if (showUpdateScreen && versionCheck) {
    return (
      <UpdateRequiredScreen
        latestVersion={versionCheck.version}
        storeUrl={versionCheck.storeUrl}
        description={versionCheck.description}
        revocationReason={versionCheck.revocationReason}
        isRequired={versionCheck.isUpdateRequired}
        onSkip={versionCheck.isUpdateRequired ? undefined : handleSkipUpdate}
      />
    );
  }

  // Auth durumuna göre screens
  const isAuthenticated = !!user;
  const isAdminUser = hasRole('Admin') || hasRole('Administrator');

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
            {!isAdminUser && (
              <>
                <Stack.Screen name="DeviceSharing" component={DeviceSharingScreen} />
                <Stack.Screen
                  name="DeviceSharingDetail"
                  component={DeviceSharingDetailScreen}
                />
              </>
            )}
            <Stack.Screen name="CompanyInfo" component={CompanyInfoScreen} />
            <Stack.Screen name="Home" component={HomeScreen} />
            <Stack.Screen name="JoinOrCreate" component={JoinOrCreateScreen} />
            <Stack.Screen name="HomeSetup" component={HomeSetupScreen} />
            <Stack.Screen name="DeviceSetup" component={DeviceSetupScreen} />
            <Stack.Screen name="DeviceAdd" component={DeviceAddScreen} />
            <Stack.Screen
              name="PowerMetricsDetail"
              component={PowerMetricsDetailScreen}
              options={{ animation: 'slide_from_right' }}
            />
            <Stack.Screen
              name="GridPerformanceDetail"
              component={GridPerformanceDetailScreen}
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
            <Stack.Screen
              name="InverterModes"
              component={InverterModesScreen}
              options={{ animation: 'slide_from_right' }}
            />
            <Stack.Screen
              name="ModeDetail"
              component={ModeDetailScreen}
              options={{ animation: 'slide_from_right' }}
            />
            <Stack.Screen
              name="CustomMode"
              component={CustomModeScreen}
              options={{ animation: 'slide_from_right' }}
            />
            <Stack.Screen
              name="TimeOfUse"
              component={TimeOfUseScreen}
              options={{ animation: 'slide_from_right' }}
            />
            <Stack.Screen
              name="MaintenanceSettings"
              component={MaintenanceSettingsScreen}
              options={{ animation: 'slide_from_right' }}
            />
            <Stack.Screen
              name="AppSettings"
              component={SettingsScreen}
              options={{ animation: 'slide_from_right' }}
            />
            <Stack.Screen
              name="GeneralSettings"
              component={GeneralSettingsScreen}
              options={{ animation: 'slide_from_right' }}
            />
            <Stack.Screen
              name="PricingSettings"
              component={PricingSettingsScreen}
              options={{ animation: 'slide_from_right' }}
            />
            <Stack.Screen
              name="DeviceSettings"
              component={DeviceSettingsScreen}
              options={{ animation: 'slide_from_right' }}
            />
            <Stack.Screen
              name="ResetSettings"
              component={ResetSettingsScreen}
              options={{ animation: 'slide_from_right' }}
            />
            <Stack.Screen
              name="Procedures"
              component={ProceduresScreen}
              options={{ animation: 'slide_from_right' }}
            />
            <Stack.Screen
              name="BatteryRecovery"
              component={BatteryRecoveryScreen}
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
