import React from 'react';
import Svg, { Rect, Path, Circle } from 'react-native-svg';
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

export default function LineChart({
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
  const points = data.map((d, i) => ({
    x: padding.left + i * stepX + stepX / 2,
    y: padding.top + yScale(d.y),
  }));
  const pathD = points.map((p, i) => (i === 0 ? `M ${p.x} ${p.y}` : `L ${p.x} ${p.y}`)).join(' ');

  return (
    <Svg width={width} height={height}>
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

      <Path d={pathD} stroke={accentColor} strokeWidth={2} fill="none" />
      {points.map((p, i) => (
        <Circle key={i} cx={p.x} cy={p.y} r={3} fill={accentColor} onPress={() => setSelectedIndex(i)} />
      ))}

      {data.map((_, i) => {
        const hx = padding.left + i * stepX;
        return (
          <Rect
            key={`hitl-${i}`}
            x={hx}
            y={padding.top}
            width={stepX}
            height={innerH}
            fill="transparent"
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
