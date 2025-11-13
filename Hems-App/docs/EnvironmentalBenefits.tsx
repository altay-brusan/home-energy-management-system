"use client"

import type React from "react"

interface BenefitCardProps {
  value: string
  unit: string
  label: string
  icon: string
}

const BenefitCard: React.FC<BenefitCardProps> = ({ value, unit, label, icon }) => {
  return (
    <div className="bg-gray-50 rounded-lg p-4 flex flex-col">
      <div className="w-12 h-12 rounded-full bg-green-50 flex items-center justify-center mb-3">
        <span className="text-2xl">{icon}</span>
      </div>
      <div className="flex items-baseline gap-1 mb-1">
        <span className="text-2xl font-bold text-gray-900">{value}</span>
        <span className="text-sm text-gray-600 font-medium">{unit}</span>
      </div>
      <p className="text-sm text-gray-500 leading-relaxed">{label}</p>
    </div>
  )
}

export default function EnvironmentalBenefits() {
  const benefits = [
    {
      value: "0.00",
      unit: "CO₂",
      label: "Tasarrufu",
      icon: "☁️",
    },
    {
      value: "0.0",
      unit: "Ağaçlar",
      label: "Eşdeğer Güç ekimi",
      icon: "🌳",
    },
  ]

  return (
    <div className="bg-white rounded-xl p-5 shadow-sm max-w-md mx-auto">
      <div className="flex items-center justify-between mb-4">
        <h2 className="text-base font-semibold text-gray-800">Çevresel faydalar</h2>
        <button className="text-gray-400 hover:text-gray-600">
          <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
            />
          </svg>
        </button>
      </div>

      <div className="grid grid-cols-2 gap-3">
        {benefits.map((benefit, index) => (
          <BenefitCard
            key={index}
            value={benefit.value}
            unit={benefit.unit}
            label={benefit.label}
            icon={benefit.icon}
          />
        ))}
      </div>
    </div>
  )
}
