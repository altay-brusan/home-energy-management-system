"use client"

interface AnimatedFlowLineProps {
  startX: number
  startY: number
  endX: number
  endY: number
  direction: "forward" | "backward"
  active: boolean
}

export default function AnimatedFlowLine({ startX, startY, endX, endY, direction, active }: AnimatedFlowLineProps) {
  return (
    <g>
      {/* Static line */}
      <line x1={startX} y1={startY} x2={endX} y2={endY} stroke="#cbd5e1" strokeWidth="3" />

      {/* Animated flow line */}
      {active && (
        <line
          x1={startX}
          y1={startY}
          x2={endX}
          y2={endY}
          stroke="url(#flowGradient)"
          strokeWidth="3"
          strokeDasharray="10, 10"
          className={direction === "forward" ? "animate-dash-forward" : "animate-dash-backward"}
        />
      )}
    </g>
  )
}
