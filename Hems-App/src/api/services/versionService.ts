import axios from 'axios';
import { Platform } from 'react-native';
import { API_CONFIG, ENDPOINTS } from '../client/config';
import {
  AppVersionCheckResponse,
  MobilePlatform,
} from '../types/version.types';

// App version from package.json - this should be updated during build
// In production, consider using react-native-device-info for accurate version
const APP_VERSION = '1.0.0';

/**
 * Get the current platform as MobilePlatform enum
 */
export function getCurrentPlatform(): MobilePlatform {
  return Platform.OS === 'ios' ? MobilePlatform.iOS : MobilePlatform.Android;
}

/**
 * Get the current app version
 */
export function getAppVersion(): string {
  return APP_VERSION;
}

export const versionService = {
  /**
   * Check if the current app version requires or has an update available.
   * This endpoint is public (no authentication required).
   */
  async checkVersion(
    platform?: MobilePlatform,
    version?: string
  ): Promise<AppVersionCheckResponse> {
    const currentPlatform = platform ?? getCurrentPlatform();
    const currentVersion = version ?? getAppVersion();

    // Use axios directly without auth interceptors since this is a public endpoint
    const response = await axios.get<AppVersionCheckResponse>(
      `${API_CONFIG.BASE_URL}${ENDPOINTS.MOBILE_APP_VERSIONS.CHECK}`,
      {
        params: {
          platform: currentPlatform,
          version: currentVersion,
        },
        timeout: 10000, // 10 second timeout for version check
      }
    );

    return response.data;
  },
};
