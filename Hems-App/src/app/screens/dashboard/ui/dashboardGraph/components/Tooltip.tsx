import React from 'react';
import { Path, Rect, Text as SvgText } from 'react-native-svg';

type Props = {
  cx: number;
  cy: number;
  label: string | number;
  padding: { top: number; right: number; bottom: number; left: number };
  innerW: number;
  COLORS: Record<string, string>;
};

export default function Tooltip({ cx, cy, label, padding, innerW, COLORS }: Props) {
  const s = String(label);
  const bw = Math.max(40, s.length * 7 + 14);
  const bx = Math.min(Math.max(cx - bw / 2, padding.left + 2), padding.left + innerW - bw - 2);
  const by = Math.max(cy - 32, 6);
  return (
    <>
      <Path d={`M ${cx} ${padding.top} V ${padding.top + (innerW ? 0 : 0)}`} stroke="transparent" />
      <Rect x={bx} y={by} width={bw} height={20} rx={6} fill={COLORS.cardBg} stroke={COLORS.border} />
      <Path d={`M ${cx - 5} ${by + 20} L ${cx} ${by + 24} L ${cx + 5} ${by + 20} Z`} fill={COLORS.cardBg} stroke={COLORS.border} />
      <SvgText x={bx + bw / 2} y={by + 14} fontSize={11} fill={COLORS.textSoft} textAnchor="middle" fontWeight="700">
        {s}
      </SvgText>
    </>
  );
}

