"use client"

import { useState } from "react"
import { Home, Smartphone, Bell, User, Shield } from "lucide-react"

type TabType = "home" | "device" | "notifications" | "profile" | "admin"

export default function MobileApp() {
  const [activeTab, setActiveTab] = useState<TabType>("home")

  const tabs = [
    { id: "home" as TabType, icon: Home, label: "Home", color: "bg-red-100 text-red-600" },
    { id: "device" as TabType, icon: Smartphone, label: "Device", color: "bg-purple-100 text-purple-600" },
    { id: "notifications" as TabType, icon: Bell, label: "Notifications", color: "bg-cyan-100 text-cyan-600" },
    { id: "profile" as TabType, icon: User, label: "Profile", color: "bg-orange-100 text-orange-600" },
    { id: "admin" as TabType, icon: Shield, label: "Admin", color: "bg-pink-100 text-pink-600" },
  ]

  const renderContent = () => {
    switch (activeTab) {
      case "home":
        return (
          <div className="text-center">
            <Home className="mx-auto mb-4 h-16 w-16 text-blue-500" />
            <h2 className="text-2xl font-bold text-gray-800">Home</h2>
            <p className="mt-2 text-gray-600">Welcome to your dashboard</p>
          </div>
        )
      case "device":
        return (
          <div className="text-center">
            <Smartphone className="mx-auto mb-4 h-16 w-16 text-purple-500" />
            <h2 className="text-2xl font-bold text-gray-800">Device</h2>
            <p className="mt-2 text-gray-600">Manage your devices</p>
          </div>
        )
      case "notifications":
        return (
          <div className="text-center">
            <Bell className="mx-auto mb-4 h-16 w-16 text-orange-500" />
            <h2 className="text-2xl font-bold text-gray-800">Notifications</h2>
            <p className="mt-2 text-gray-600">View your notifications</p>
          </div>
        )
      case "profile":
        return (
          <div className="text-center">
            <User className="mx-auto mb-4 h-16 w-16 text-green-500" />
            <h2 className="text-2xl font-bold text-gray-800">Profile</h2>
            <p className="mt-2 text-gray-600">Your account settings</p>
          </div>
        )
      case "admin":
        return (
          <div className="text-center">
            <Shield className="mx-auto mb-4 h-16 w-16 text-red-500" />
            <h2 className="text-2xl font-bold text-gray-800">Admin</h2>
            <p className="mt-2 text-gray-600">Administrative panel</p>
          </div>
        )
    }
  }

  return (
    <div className="flex h-screen flex-col bg-gradient-to-br from-indigo-300 via-purple-200 to-blue-300">
      {/* Main Content Area */}
      <div className="flex-1 overflow-auto p-6">
        <div className="flex h-full items-center justify-center">{renderContent()}</div>
      </div>

      <nav className="p-6">
        <div className="mx-auto max-w-2xl rounded-full bg-white px-6 py-4 shadow-xl">
          <div className="flex items-center justify-between gap-2">
            {tabs.map((tab) => {
              const Icon = tab.icon
              const isActive = activeTab === tab.id

              return (
                <button
                  key={tab.id}
                  onClick={() => setActiveTab(tab.id)}
                  className={`flex items-center gap-2 rounded-full px-4 py-2 transition-all duration-300 ${
                    isActive ? tab.color : "hover:bg-gray-50"
                  }`}
                >
                  <Icon className={`h-6 w-6 ${isActive ? "" : "text-gray-600"}`} strokeWidth={2} />
                  {isActive && <span className="font-medium">{tab.label}</span>}
                </button>
              )
            })}
          </div>
        </div>
      </nav>
    </div>
  )
}
