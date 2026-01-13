import { useState, useCallback } from 'react';
export type Region = 'Europe' | 'US' | 'Asia';

export function useRegion(initial: Region = 'Europe') {
  const [region, setRegion] = useState<Region>(initial);
  const toggleRegion = useCallback(
    () => setRegion(r => (r === 'Europe' ? 'US' : r === 'US' ? 'Asia' : 'Europe')),
    []
  );
  return { region, toggleRegion };
}
