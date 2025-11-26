"use client"

import { useState, useEffect } from "react"
import EnergyFlowDiagram from "@/components/EnergyFlowDiagram"

// Mock data - you can replace this with real data from your API
const mockEnergyData = {
  solar: { power: 238, active: true, flow: "output" as const },
  battery: { power: 238, active: true, flow: "input" as const },
  grid: { power: 238, active: true, flow: "output" as const },
  house: { power: 238, active: true, flow: "input" as const },
}

export default function Page() {
  const [energyData, setEnergyData] = useState(mockEnergyData)

  // Simulate dynamic energy changes
  useEffect(() => {
    const interval = setInterval(() => {
      setEnergyData({
        solar: {
          power: Math.random() > 0.3 ? Math.floor(Math.random() * 300) : 0,
          active: Math.random() > 0.3,
          flow: "output" as const,
        },
        battery: {
          power: Math.floor(Math.random() * 300),
          active: true,
          flow: (Math.random() > 0.5 ? "input" : "output") as const,
        },
        grid: {
          power: Math.floor(Math.random() * 300),
          active: Math.random() > 0.2,
          flow: (Math.random() > 0.5 ? "input" : "output") as const,
        },
        house: {
          power: Math.floor(Math.random() * 300),
          active: true,
          flow: "input" as const,
        },
      })
    }, 3000)

    return () => clearInterval(interval)
  }, [])

  return (
    <div className="flex min-h-screen items-center justify-center bg-gray-100">
      <EnergyFlowDiagram data={energyData} />
    </div>
  )
}
