import AsyncStorage from "@react-native-async-storage/async-storage";

const MODE_KEY = "currentMode";

export const saveMode = async (mode: string) => {
  try {
    await AsyncStorage.setItem(MODE_KEY, mode);
  } catch (err) {}
};

export const getMode = async (): Promise<string | null> => {
  try {
    const mode = await AsyncStorage.getItem(MODE_KEY);
    return mode;
  } catch (err) {return null;
  }
};

export const clearMode = async () => {
  try {
    await AsyncStorage.removeItem(MODE_KEY);
  } catch (err) {}
};
