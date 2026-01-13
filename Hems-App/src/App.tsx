import React, { useEffect } from 'react';
import { StatusBar, Linking } from 'react-native';
import 'react-native-gesture-handler';
import { GestureHandlerRootView } from 'react-native-gesture-handler';
import { SafeAreaProvider } from 'react-native-safe-area-context';

import { RootNavigator } from './app/routes';
import GlobalAlert from './components/feedback/GlobalAlert';
import GlobalError from './components/feedback/GlobalError';
import Toast from './components/feedback/Toast/Toast';
import NetworkWrapper from './components/NetworkWrapper';
import MaintenanceScreen from './screens/MaintenanceScreen';
import { SignalRProvider } from './providers/SignalRProvider';
import './lib/i18n';
import { initializeI18nStore } from './store/i18nStore';
import { useToastStore } from './store/toastStore';
import { useErrorStore } from './store/errorStore';
import { useInvitationStore, loadPendingInvitationFromStorage } from './store/invitationStore';

export default function App () {
  const { visible, message, type, title, duration, hide } = useToastStore();
  const { setPendingInvitationCode } = useInvitationStore();
  const criticalError = useErrorStore(s => s.criticalError);

  // Deep link handler
  useEffect(() => {
    const handleDeepLink = (event: { url: string }) => {
      try {
        // Handle custom scheme: n2nenergymobile://invite?code=2321312
        if (event.url.startsWith('n2nenergymobile://invite')) {
          const urlObj: any = new URL(event.url);
          const code = urlObj && urlObj.searchParams.get('code');
          if (code) {
            setPendingInvitationCode(code);
          }
        }
        if (event.url.includes('inviteCode=')) {
          const urlObj: any = new URL(event.url);
          const code = urlObj && urlObj.searchParams.get('inviteCode');

          if (code) {
            setPendingInvitationCode(code);
          }
        }
      } catch (error) {}
    };

    // Check initial URL (if app was opened from deep link)
    Linking.getInitialURL().then((url) => {
      if (url) {
        handleDeepLink({ url });
      }
    });

    // Listen for deep links when app is running
    const subscription = Linking.addEventListener('url', handleDeepLink);

    return () => {
      subscription.remove();
    };
  }, [setPendingInvitationCode]);

  useEffect(() => {
    const initializeApp = async () => {
      try {
        await initializeI18nStore();
        // Load pending invitation from storage
        const pendingCode = await loadPendingInvitationFromStorage();
        if (pendingCode) {}
      } catch (error) {}
    };
    initializeApp();
  }, []);

  // Kritik hata varsa (sunucu/ağ hatası) bakım sayfası göster
  if (criticalError) {
    return (
      <GestureHandlerRootView style={{ flex: 1 }}>
        <SafeAreaProvider>
          <StatusBar barStyle="light-content" backgroundColor="#1b222a" />
          <MaintenanceScreen error={criticalError} />
        </SafeAreaProvider>
      </GestureHandlerRootView>
    );
  }

  return (
    <GestureHandlerRootView style={{ flex: 1 }}>
      <SafeAreaProvider>
        <StatusBar barStyle="light-content" backgroundColor="#1b222a" />
        <SignalRProvider>
          <NetworkWrapper>
            <RootNavigator />
          </NetworkWrapper>
          <GlobalError />
          <GlobalAlert />
          <Toast
            visible={visible}
            message={message}
            type={type}
            title={title}
            duration={duration}
            onHide={hide}
          />
        </SignalRProvider>
      </SafeAreaProvider>
    </GestureHandlerRootView>
  );
}
