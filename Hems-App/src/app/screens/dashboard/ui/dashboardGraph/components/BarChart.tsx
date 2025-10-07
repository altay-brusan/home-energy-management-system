import React from 'react';
import Svg, { Defs, LinearGradient, Stop, Rect, Path } from 'react-native-svg';
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
};

export default function BarChart({
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
}: Props) {
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
        const x = padding.left + i * stepX + stepX / 2 - barW / 2;
        const y = padding.top + yScale(d.y);
        const h = innerH - yScale(d.y);
        return (
          <Rect
            key={i}
            x={x}
            y={y}
            width={barW}
            height={h}
            rx={6}
            fill="url(#barGrad)"
            onPress={() => setSelectedIndex(i)}
          />
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

      {selectedIndex !== null && (() => {
        const i = selectedIndex as number;
        const d = data[i];
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
