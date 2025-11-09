"use client"

import { useState } from "react"
import { Home, Smartphone, Bell, User, Shield } from "lucide-react"

type TabType = "home" | "device" | "notifications" | "profile" | "admin"

export default function MobileApp() {
  const [activeTab, setActiveTab] = useState<TabType>("home")

  const tabs = [
    { id: "home" as TabType, icon: Home, label: "Home" },
    { id: "device" as TabType, icon: Smartphone, label: "Device" },
    { id: "notifications" as TabType, icon: Bell, label: "Notifications" },
    { id: "profile" as TabType, icon: User, label: "Profile" },
    { id: "admin" as TabType, icon: Shield, label: "Admin" },
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
    <div className="flex h-screen flex-col bg-gray-50">
      {/* Main Content Area */}
      <div className="flex-1 overflow-auto p-6">
        <div className="flex h-full items-center justify-center">{renderContent()}</div>
      </div>

      {/* Bottom Navigation */}
      <nav className="border-t border-gray-200 bg-white shadow-lg">
        <div className="mx-auto flex max-w-md items-center justify-around px-4 py-2">
          {tabs.map((tab) => {
            const Icon = tab.icon
            const isActive = activeTab === tab.id

            return (
              <button
                key={tab.id}
                onClick={() => setActiveTab(tab.id)}
                className="group relative flex flex-col items-center gap-1 px-3 py-2 transition-all duration-300"
              >
                {/* Active indicator */}
                {isActive && (
                  <div className="absolute -top-2 left-1/2 h-1 w-12 -translate-x-1/2 rounded-full bg-gradient-to-r from-blue-500 to-purple-500" />
                )}

                {/* Icon */}
                <div
                  className={`rounded-xl p-2 transition-all duration-300 ${
                    isActive
                      ? "bg-gradient-to-br from-blue-500 to-purple-500 shadow-lg"
                      : "bg-gray-100 group-hover:bg-gray-200"
                  }`}
                >
                  <Icon
                    className={`h-5 w-5 transition-colors duration-300 ${
                      isActive ? "text-white" : "text-gray-600 group-hover:text-gray-800"
                    }`}
                  />
                </div>

                {/* Label */}
                <span
                  className={`text-xs font-medium transition-colors duration-300 ${
                    isActive
                      ? "bg-gradient-to-r from-blue-600 to-purple-600 bg-clip-text text-transparent"
                      : "text-gray-600 group-hover:text-gray-800"
                  }`}
                >
                  {tab.label}
                </span>
              </button>
            )
          })}
        </div>
      </nav>
    </div>
  )
}
