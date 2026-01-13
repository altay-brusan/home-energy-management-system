import { Platform } from 'react-native';

export function usePlatform() {
  const isIOS = Platform.OS === 'ios';
  const isAndroid = Platform.OS === 'android';
  const isWeb = Platform.OS === 'web';

  return {
    isIOS,
    isAndroid,
    isWeb,
    os: Platform.OS,
    version: Platform.Version,
    select: Platform.select,
  };
}
