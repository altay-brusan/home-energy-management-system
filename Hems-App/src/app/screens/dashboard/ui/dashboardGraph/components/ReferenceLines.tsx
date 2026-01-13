import React from 'react';
import { Path, Rect, Text as SvgText } from 'react-native-svg';
import { useTranslation } from 'react-i18next';


type Props = {
  avg: number;
  peak: number;
  padding: { top: number; right: number; bottom: number; left: number };
  innerW: number;
  yScale: (v: number) => number;
  COLORS: Record<string, string>;
  accentColor: string;
};

export default function ReferenceLines({
  
  avg,
  peak,
  padding,
  innerW,
  yScale,
  COLORS,
  accentColor,
}: Props) {
  const { t } = useTranslation(); 
  const yAvg = padding.top + yScale(avg);
  const yPeak = padding.top + yScale(peak);
  const labelAvg = `Avg ${avg}`;
  const labelPeak = `${t('dashboardGraph.peak')} ${peak}`;
  const labelX = padding.left + innerW - 6;
  const bwAvg = Math.max(34, labelAvg.length * 6 + 10);
  const bwPeak = Math.max(40, labelPeak.length * 6 + 10);
  const bxAvg = labelX - bwAvg;
  const bxPeak = labelX - bwPeak;
  const byAvg = Math.max(yAvg - 16, 2);
  const byPeak = Math.max(yPeak - 16, 2);

  return (
    <>
      {/* <Path
        d={`M ${padding.left} ${yAvg} H ${padding.left + innerW}`}
        stroke={COLORS.slate400}
        strokeDasharray="4 4"
        strokeWidth={1}
        opacity={0.9}
      />
      <Rect
        x={bxAvg}
        y={byAvg}
        width={bwAvg}
        height={16}
        rx={6}
        fill={COLORS.cardBg}
        stroke={COLORS.border}
      />
      <SvgText
        x={bxAvg + bwAvg / 2}
        y={byAvg + 12}
        fontSize={10}
        fill={COLORS.slate400}
        textAnchor="middle"
      >
        {labelAvg}
      </SvgText> */}

      <Path
        d={`M ${padding.left} ${yPeak} H ${padding.left + innerW}`}
        stroke={accentColor}
        strokeDasharray="6 4"
        strokeWidth={1}
        opacity={0.95}
      />
      <Rect
        x={bxPeak}
        y={byPeak}
        width={bwPeak}
        height={16}
        rx={6}
        fill={COLORS.cardBg}
        stroke={COLORS.border}
      />
      <SvgText
        x={bxPeak + bwPeak / 2}
        y={byPeak + 12}
        fontSize={10}
        fill={accentColor}
        textAnchor="middle"
        fontWeight="700"
      >
        {labelPeak}
      </SvgText>
    </>
  );
}

