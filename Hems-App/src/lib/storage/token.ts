import AsyncStorage from '@react-native-async-storage/async-storage';

export interface UserData {
  id: string;
  email: string;
  name?: string;
  surname?: string;
  roles: string[];
}

const STORAGE_KEYS = {
  ACCESS_TOKEN: '@auth_access_token',
  REFRESH_TOKEN: '@auth_refresh_token',
  TOKEN_EXPIRY: '@auth_token_expiry',
  USER_DATA: '@auth_user_data',
} as const;

export async function saveTokens(accessToken: string, refreshToken: string, expiresIn: number): Promise<void> {
  try {
    const expiryTime = Date.now() + expiresIn * 1000;
    await AsyncStorage.multiSet([
      [STORAGE_KEYS.ACCESS_TOKEN, accessToken],
      [STORAGE_KEYS.REFRESH_TOKEN, refreshToken],
      [STORAGE_KEYS.TOKEN_EXPIRY, expiryTime.toString()],
    ]);
  } catch (error) {}
}

export async function getAccessToken(): Promise<string | null> {
  try {
    return await AsyncStorage.getItem(STORAGE_KEYS.ACCESS_TOKEN);
  } catch (error) {return null;
  }
}

export async function getRefreshToken(): Promise<string | null> {
  try {
    return await AsyncStorage.getItem(STORAGE_KEYS.REFRESH_TOKEN);
  } catch (error) {return null;
  }
}

export async function removeTokens(): Promise<void> {
  try {
    await AsyncStorage.multiRemove([
      STORAGE_KEYS.ACCESS_TOKEN,
      STORAGE_KEYS.REFRESH_TOKEN,
      STORAGE_KEYS.TOKEN_EXPIRY,
    ]);
  } catch (error) {}
}

export async function isTokenExpired(): Promise<boolean> {
  try {
    const expiryTime = await AsyncStorage.getItem(STORAGE_KEYS.TOKEN_EXPIRY);
    if (!expiryTime) return true;
    return Date.now() >= parseInt(expiryTime, 10) - 300000;
  } catch (error) {return true;
  }
}

export async function saveUserData(userData: UserData): Promise<void> {
  try {
    await AsyncStorage.setItem(STORAGE_KEYS.USER_DATA, JSON.stringify(userData));
  } catch (error) {}
}

export async function getUserData(): Promise<UserData | null> {
  try {
    const data = await AsyncStorage.getItem(STORAGE_KEYS.USER_DATA);
    const userData = data ? JSON.parse(data) : null;
    return userData;
  } catch (error) {return null;
  }
}

export async function removeUserData(): Promise<void> {
  try {
    await AsyncStorage.removeItem(STORAGE_KEYS.USER_DATA);
  } catch (error) {}
}

const TOKEN_KEY = '@auth_token';
export async function saveToken(token: string): Promise<void> {
  try {
    await AsyncStorage.setItem(TOKEN_KEY, token);
  } catch (error) {}
}

export async function getToken(): Promise<string | null> {
  try {
    return await AsyncStorage.getItem(TOKEN_KEY);
  } catch (error) {return null;
  }
}

export async function removeToken(): Promise<void> {
  try {
    await AsyncStorage.removeItem(TOKEN_KEY);
  } catch (error) {}
}
