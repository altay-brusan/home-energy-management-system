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
  if (label === null || label === undefined || (typeof label === 'number' && isNaN(label))) {
    return null;
  }
  
  const s = String(label);
  if (!s || s.length === 0) {
    return null;
  }
  
  const lines = s.split('\n');
  if (lines.length === 0) {
    return null;
  }
  
  const maxLineLength = Math.max(...lines.map(line => line.length || 0));
  const bw = Math.max(60, maxLineLength * 7 + 14);
  const lineHeight = 14;
  const tooltipHeight = lines.length > 1 ? lineHeight * lines.length + 8 : 20;
  const bx = Math.min(Math.max(cx - bw / 2, padding.left + 2), padding.left + innerW - bw - 2);
  const by = Math.max(cy - tooltipHeight - 10, 6);
  return (
    <>
      <Path d={`M ${cx} ${padding.top} V ${padding.top + (innerW ? 0 : 0)}`} stroke="transparent" />
      <Rect x={bx} y={by} width={bw} height={tooltipHeight} rx={6} fill={COLORS.cardBg} stroke={COLORS.border} strokeWidth={1} />
      <Path d={`M ${cx - 5} ${by + tooltipHeight} L ${cx} ${by + tooltipHeight + 4} L ${cx + 5} ${by + tooltipHeight} Z`} fill={COLORS.cardBg} stroke={COLORS.border} strokeWidth={1} />
      {lines.map((line, index) => (
        <SvgText
          key={index}
          x={bx + bw / 2}
          y={by + 12 + (index * lineHeight)}
          fontSize={11}
          fill={COLORS.textSoft}
          textAnchor="middle"
          fontWeight={index === 0 ? "600" : "700"}
        >
          {line}
        </SvgText>
      ))}
    </>
  );
}

