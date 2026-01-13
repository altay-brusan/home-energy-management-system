import type { Dispatch, SetStateAction } from 'react';

export type Range = 'Day' | 'Week' | 'Month' | 'Year';

export interface DashboardGraphProps {
  COLORS: Record<string, string>;
  isDark: boolean;
  selectedRange: Range;
  setSelectedRange: Dispatch<SetStateAction<Range>>;
}

