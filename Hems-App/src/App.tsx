import React, { useEffect } from 'react';
import { StatusBar } from 'react-native';
import 'react-native-gesture-handler';
import { GestureHandlerRootView } from 'react-native-gesture-handler';
import { SafeAreaProvider } from 'react-native-safe-area-context';

import { RootNavigator } from './app/routes';
import GlobalAlert from './components/feedback/GlobalAlert';
import GlobalError from './components/feedback/GlobalError';
import NetworkWrapper from './components/NetworkWrapper';
import { initializeI18nStore } from './store/i18nStore';
import './lib/i18n';

export default function App () {

  useEffect(() => {
    const initializeApp = async () => {
      try {
        await initializeI18nStore();
      } catch (error) {
        console.error('Failed to initialize i18n store:', error);
      }
    };
    initializeApp();
  }, []);

  return (
    <GestureHandlerRootView style={{ flex: 1 }}>
      <SafeAreaProvider>
        <StatusBar barStyle="light-content" backgroundColor="#1b222a" />
        <NetworkWrapper>
          <RootNavigator />
        </NetworkWrapper>
        <GlobalError />
        <GlobalAlert />
      </SafeAreaProvider>
    </GestureHandlerRootView>
  );
}
