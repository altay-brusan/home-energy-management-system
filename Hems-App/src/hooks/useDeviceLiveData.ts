import { useState } from "react";
import { mobileDashboardService } from "../api/services/mobileDashboardService";

export function useDeviceLiveData(deviceId: string | null) {
  const [isFetching, setIsFetching] = useState(false);
  const [data, setData] = useState<any>(null);

  const fetchLiveData = async (component: any) => {
    if (!deviceId) return null;
    setIsFetching(true);
    try {
      const devWithData = await mobileDashboardService.getDeviceWithData(deviceId);
      const target = devWithData?.components?.find(
        (c: any) => c.componentId === component.id
      );
      if (!target) throw new Error("Bileşen bulunamadı");

      const liveData = await mobileDashboardService.getComponentLiveData(
        deviceId,
        target.componentId
      );
      const d = liveData.data || {};
      let parsed: any = {};

      if (component.name.toLowerCase().includes("inverter")) {
        parsed = {
          type: "inverter",
          "Active Power": d["Active Power"] ?? 0,
          "Reactive Power": d["Reactive Power"] ?? 0,
          "Power Factor": d["Power Factor"] ?? 0,
          "Insulation Resistance": d["Insulation Resistance"] ?? 0,
          "Last Inverter Startup Time": d["Last Inverter Startup Time"] ?? "-",
          "Last Inverter Shutdown Time": d["Last Inverter Shutdown Time"] ?? "-",
          "Device Serial Number": d["Device Serial Number"] ?? "-",
          "Firmware Version": d["Firmware Version"] ?? "-",
          Phases: d.Phases ?? [],
        };
      } else if (component.name.toLowerCase().includes("bms")) {
  parsed = {
    type: "bms",
    SOC: component.componentData ?? 0,
    VoltagePack: d["Voltage Pack"] ?? 0,
    CurrentPack: d["Current Pack"] ?? 0,
    MaxVoltageOfBatteryCluster: d["Max Voltage Of Battery Cluster"] ?? 0,
    MinVoltageOfBatteryCluster: d["Min Voltage Of Battery Cluster"] ?? 0,
    TotalChargeCapacity: d["Total Charge Capacity"] ?? 0,
    TotalDischargeCapacity: d["Total Discharge Capacity"] ?? 0,
    DateTime: d["DateTime"] ?? "-",
    DeviceSerialNumber: d["Device Serial Number"] ?? "-",
    FirmwareVersion: d["Firmware Version"] ?? "-",
  };
} else {
        // 🔹 Battery Parsing (Verdiğin JSON’a göre)
        const cells = d.SOC || [];
        const avgVoltage =
          cells.reduce((a: number, c: any) => a + (c.CellVoltage || 0), 0) /
            (cells.length || 1) || 0;
        const avgTemp =
          cells.reduce((a: number, c: any) => a + (c.CellTemperature || 0), 0) /
            (cells.length || 1) || 0;

        parsed = {
          type: "battery",
         soc: component.componentData ?? 0,
          soh: d.SOH ?? "-",
          voltage: d.BatteryPackVoltage ?? 0,
          current: d.BatteryCurrent ?? 0,
          maxCellVoltage: d.MaxCellVoltage ?? 0,
          minCellVoltage: d.MinCellVoltage ?? 0,
          maxCellTemp: d.MaxCellTemperature ?? 0,
          minCellTemp: d.MinCellTemperature ?? 0,
          avgCellVoltage: avgVoltage,
          avgTemp: avgTemp,
          totalCharge: d.TotalChargeCapacity ?? "-",
          totalDischarge: d.TotalDischargeAmount ?? 0,
          model: d.DeviceModel ?? "-",
          sn: d.DeviceSerialNumber ?? "-",
          version: d.FirmwareVersion ?? "-",
          dateTime: d.DateTime ?? "-",
        };
      }

      setData(parsed);
      return parsed;
    } catch (err) {setData(null);
      return null;
    } finally {
      setIsFetching(false);
    }
  };

  return { data, isFetching, fetchLiveData };
}
