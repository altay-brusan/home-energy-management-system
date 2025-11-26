"use client"
import EnergyComponent from "./EnergyComponent"
import AnimatedFlowLine from "./AnimatedFlowLine"

const COMPONENT_SIZE = 100
const CENTER_SIZE = 60
const OFFSET = 140

interface EnergyComponentData {
  power: number
  active: boolean
  flow: "input" | "output" | "both"
}

interface EnergyFlowDiagramProps {
  data: {
    solar: EnergyComponentData
    battery: EnergyComponentData
    grid: EnergyComponentData
    house: EnergyComponentData
  }
}

export default function EnergyFlowDiagram({ data }: EnergyFlowDiagramProps) {
  const centerX = 400
  const centerY = 300

  return (
    <div className="relative" style={{ width: "800px", height: "600px" }}>
      {/* SVG for all flow lines */}
      <svg className="absolute inset-0" width="800" height="600" style={{ pointerEvents: "none" }}>
        <defs>
          <linearGradient id="flowGradient" x1="0%" y1="0%" x2="100%" y2="0%">
            <stop offset="0%" stopColor="#3b82f6" stopOpacity="0.3" />
            <stop offset="50%" stopColor="#3b82f6" stopOpacity="1" />
            <stop offset="100%" stopColor="#3b82f6" stopOpacity="0.3" />
          </linearGradient>
        </defs>

        {/* Solar to Center */}
        {data.solar.active && data.solar.power > 0 && (
          <AnimatedFlowLine
            startX={centerX}
            startY={centerY - OFFSET + COMPONENT_SIZE / 2}
            endX={centerX}
            endY={centerY - CENTER_SIZE / 2}
            direction="forward"
            active={true}
          />
        )}

        {/* Center to Battery (bidirectional) */}
        {data.battery.active && data.battery.power > 0 && (
          <AnimatedFlowLine
            startX={centerX - OFFSET + COMPONENT_SIZE / 2}
            startY={centerY}
            endX={centerX - CENTER_SIZE / 2}
            endY={centerY}
            direction={data.battery.flow === "input" ? "backward" : "forward"}
            active={true}
          />
        )}

        {/* Center to Grid */}
        {data.grid.active && data.grid.power > 0 && (
          <AnimatedFlowLine
            startX={centerX + CENTER_SIZE / 2}
            startY={centerY}
            endX={centerX + OFFSET - COMPONENT_SIZE / 2}
            endY={centerY}
            direction={data.grid.flow === "input" ? "backward" : "forward"}
            active={true}
          />
        )}

        {/* Center to House */}
        {data.house.active && data.house.power > 0 && (
          <AnimatedFlowLine
            startX={centerX}
            startY={centerY + CENTER_SIZE / 2}
            endX={centerX}
            endY={centerY + OFFSET - COMPONENT_SIZE / 2}
            direction="forward"
            active={true}
          />
        )}
      </svg>

      {/* Center Hub */}
      <div
        className="absolute rounded-full bg-gray-300 border-[3px] border-gray-400"
        style={{
          left: centerX - CENTER_SIZE / 2,
          top: centerY - CENTER_SIZE / 2,
          width: CENTER_SIZE,
          height: CENTER_SIZE,
        }}
      />

      {/* Solar Panel - Top */}
      <EnergyComponent
        type="solar"
        power={data.solar.power}
        active={data.solar.active}
        style={{
          left: centerX - COMPONENT_SIZE / 2,
          top: centerY - OFFSET - COMPONENT_SIZE / 2,
        }}
      />

      {/* Battery - Left */}
      <EnergyComponent
        type="battery"
        power={data.battery.power}
        active={data.battery.active}
        style={{
          left: centerX - OFFSET - COMPONENT_SIZE / 2,
          top: centerY - COMPONENT_SIZE / 2,
        }}
      />

      {/* Grid - Right */}
      <EnergyComponent
        type="grid"
        power={data.grid.power}
        active={data.grid.active}
        style={{
          left: centerX + OFFSET - COMPONENT_SIZE / 2,
          top: centerY - COMPONENT_SIZE / 2,
        }}
      />

      {/* House - Bottom */}
      <EnergyComponent
        type="house"
        power={data.house.power}
        active={data.house.active}
        style={{
          left: centerX - COMPONENT_SIZE / 2,
          top: centerY + OFFSET - COMPONENT_SIZE / 2,
        }}
      />
    </div>
  )
}
