import { useEffect, useState } from 'react';
import NetInfo, { NetInfoState } from '@react-native-community/netinfo';

export function useNetInfo() {
  const [netInfo, setNetInfo] = useState<NetInfoState | null>(null);

  useEffect(() => {
    const unsub = NetInfo.addEventListener(state => setNetInfo(state));
    return () => unsub();
  }, []);

  return {
    isConnected: netInfo?.isConnected ?? null,
    type: netInfo?.type ?? 'unknown',
    details: netInfo,
  };
}
