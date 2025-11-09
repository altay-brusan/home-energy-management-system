"use client"

import type { CSSProperties } from "react"

interface EnergyComponentProps {
  type: "solar" | "battery" | "grid" | "house"
  power: number
  active: boolean
  style?: CSSProperties
}

// SVG Icons as components
const SolarIcon = ({ active }: { active: boolean }) => (
  <svg width="48" height="48" viewBox="0 0 24 24" fill="none">
    <path
      d="M12 2v2m0 16v2M4.93 4.93l1.41 1.41m11.32 11.32l1.41 1.41M2 12h2m16 0h2M6.34 17.66l-1.41 1.41M19.07 4.93l-1.41 1.41"
      stroke={active ? "#1f2937" : "#9ca3af"}
      strokeWidth="2"
      strokeLinecap="round"
    />
    <circle cx="12" cy="12" r="4" stroke={active ? "#1f2937" : "#9ca3af"} strokeWidth="2" fill="none" />
  </svg>
)

const BatteryIcon = ({ active }: { active: boolean }) => (
  <svg width="48" height="48" viewBox="0 0 24 24" fill="none">
    <rect
      x="3"
      y="7"
      width="15"
      height="10"
      rx="2"
      stroke={active ? "#1f2937" : "#9ca3af"}
      strokeWidth="2"
      fill="none"
    />
    <rect x="5" y="9" width="6" height="6" fill={active ? "#1f2937" : "#9ca3af"} />
    <path d="M18 11h3v2h-3" stroke={active ? "#1f2937" : "#9ca3af"} strokeWidth="2" />
  </svg>
)

const GridIcon = ({ active }: { active: boolean }) => (
  <svg width="48" height="48" viewBox="0 0 24 24" fill="none">
    <path
      d="M13 2L3 14h8l-1 8 10-12h-8l1-8z"
      stroke={active ? "#1f2937" : "#9ca3af"}
      strokeWidth="2"
      strokeLinejoin="round"
      fill="none"
    />
  </svg>
)

const HouseIcon = ({ active }: { active: boolean }) => (
  <svg width="48" height="48" viewBox="0 0 24 24" fill="none">
    <path
      d="M3 9l9-7 9 7v11a2 2 0 01-2 2H5a2 2 0 01-2-2V9z"
      stroke={active ? "#1f2937" : "#9ca3af"}
      strokeWidth="2"
      fill="none"
    />
    <path d="M9 22V12h6v10" stroke={active ? "#1f2937" : "#9ca3af"} strokeWidth="2" />
  </svg>
)

const iconMap = {
  solar: SolarIcon,
  battery: BatteryIcon,
  grid: GridIcon,
  house: HouseIcon,
}

export default function EnergyComponent({ type, power, active, style }: EnergyComponentProps) {
  const Icon = iconMap[type]

  return (
    <div
      className={`absolute flex flex-col items-center justify-center bg-white rounded-xl border-2 border-gray-200 shadow-md transition-opacity duration-300 ${
        !active ? "opacity-40 bg-gray-50" : ""
      }`}
      style={{
        ...style,
        width: 100,
        height: 100,
      }}
    >
      <div className="mb-2">
        <Icon active={active} />
      </div>
      <p className={`text-base font-bold ${active ? "text-gray-800" : "text-gray-400"}`}>{power} W</p>
    </div>
  )
}
