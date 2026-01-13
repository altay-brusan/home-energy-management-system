import React from 'react';
import Svg, { Defs, LinearGradient, Stop, Rect, Path, Text as SvgText } from 'react-native-svg';
import ReferenceLines from './ReferenceLines';
import Tooltip from './Tooltip';

type Datum = { x: string | number; y: number };

type Props = {
  width: number;
  height: number;
  padding: { top: number; right: number; bottom: number; left: number };
  innerW: number;
  innerH: number;
  stepX: number;
  yScale: (v: number) => number;
  data: ReadonlyArray<Datum>;
  COLORS: Record<string, string>;
  accentColor: string;
  avg: number;
  peak: number;
  selectedIndex: number | null;
  setSelectedIndex: (i: number | null) => void;
  selectedRange?: string;
};

export default function BarChart ({
  width,
  height,
  padding,
  innerW,
  innerH,
  stepX,
  yScale,
  data,
  COLORS,
  accentColor,
  avg,
  peak,
  selectedIndex,
  setSelectedIndex,
  selectedRange,
}: Props) {
  // Calculate optimal label interval based on data length (max 31 points)
  const getLabelInterval = (dataLength: number, timeRange?: string) => {
    // Haftalık için özel durum - gün adları kısa olmalı
    if (timeRange === 'Week' && dataLength <= 7) return 1;
    
    if (dataLength <= 7) return 1;   // Show all if 7 or fewer points
    if (dataLength <= 14) return 2;  // Every 2nd if 8-14 points
    if (dataLength <= 21) return 3;  // Every 3rd if 15-21 points
    if (dataLength <= 31) return 4;  // Every 4th if 22-31 points
    return Math.ceil(dataLength / 8); // Fallback for larger datasets
  };

  const labelInterval = getLabelInterval(data.length, selectedRange);

  const barW = Math.max(stepX * 0.5, 6);

  return (
    <Svg width={width} height={height}>
      <Defs>
        <LinearGradient id="barGrad" x1="0" y1="0" x2="0" y2="1">
          <Stop offset="0%" stopColor={accentColor} stopOpacity={0.95} />
          <Stop offset="100%" stopColor={accentColor} stopOpacity={0.45} />
        </LinearGradient>
      </Defs>

      {Array.from({ length: 3 }).map((_, i) => {
        const y = padding.top + ((i + 1) * innerH) / 4;
        return (
          <Rect
            key={i}
            x={padding.left}
            y={y}
            width={innerW}
            height={1}
            fill={COLORS.slate200}
            opacity={0.5}
          />
        );
      })}

      <Rect x={0} y={0} width={width} height={height} fill="transparent" onPress={() => setSelectedIndex(null)} />

      {data.map((d, i) => {
        if (!d || typeof d.y !== 'number' || isNaN(d.y)) return null;
        const x = padding.left + i * stepX + stepX / 2 - barW / 2;
        const yValue = Math.max(0, d.y);
        const y = padding.top + yScale(yValue);
        const h = innerH - yScale(yValue);
        return (
          <Rect
            key={i}
            x={x}
            y={y}
            width={barW}
            height={Math.max(0, h)}
            rx={6}
            fill="url(#barGrad)"
            onPress={() => setSelectedIndex(i)}
          />
        );
      })}

      {data.map((d, i) => {
        // For Day view, show specific hours: 0, 4, 8, 12, 16, 20, 23
        if (selectedRange === 'Day') {
          const showHours = [0, 4, 8, 12, 16, 20, 23];
          if (!showHours.includes(i)) return null;
        } else if (selectedRange === 'Week') {
          // Week için tüm günleri göster (kısa adlarla) - 7 günün tamamı
          if (i >= 7) return null;
        } else {
          // For other periods, use dynamic interval
          if (i % labelInterval !== 0) return null;
        }
        
        const x = padding.left + i * stepX + stepX / 2;
        
        // Week için çok kısa gün adı kullan
        let labelText = d.x;
        if (selectedRange === 'Week') {
          const dayNames = ['P', 'S', 'Ç', 'P', 'C', 'C', 'P'];
          labelText = i < 7 ? dayNames[i] : d.x;
        }
        
        return (
          <SvgText
            key={`label-${i}`}
            x={x}
            y={selectedRange === 'Week' ? padding.top + innerH + 35 : padding.top + innerH + 35}
            fontSize={selectedRange === 'Week' ? "10" : "12"}
            fill={COLORS.textSoft}
            textAnchor="middle"
            fontWeight="bold"
            transform={selectedRange === 'Week' ? `rotate(-60 ${x} ${padding.top + innerH + 35})` : `rotate(-90 ${x} ${padding.top + innerH + 35})`}
          >
            {labelText}
          </SvgText>
        );
      })}

      <ReferenceLines
        avg={avg}
        peak={peak}
        padding={padding}
        innerW={innerW}
        yScale={yScale}
        COLORS={COLORS}
        accentColor={accentColor}
      />

      {selectedIndex !== null && selectedIndex >= 0 && selectedIndex < data.length && (() => {
        const i = selectedIndex as number;
        const d = data[i];
        if (!d || typeof d.y !== 'number' || isNaN(d.y)) return null;
        const cx = padding.left + i * stepX + stepX / 2;
        const cy = padding.top + yScale(d.y);
        return (
          <Tooltip
            cx={cx}
            cy={cy}
            label={d.y}
            padding={padding}
            innerW={innerW}
            COLORS={COLORS}
          />
        );
      })()}
    </Svg>
  );
}
