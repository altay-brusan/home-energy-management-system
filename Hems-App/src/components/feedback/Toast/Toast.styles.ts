// Toast styles are now inline in Toast.tsx
// This file is kept for backward compatibility

import { StyleSheet } from 'react-native';

export const createToastStyles = (COLORS: Record<string, string>, isDark: boolean) =>
  StyleSheet.create({});

export type ToastStyles = ReturnType<typeof createToastStyles>;
