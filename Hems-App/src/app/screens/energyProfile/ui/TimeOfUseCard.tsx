import React, { useState, useEffect } from "react";
import { View, Text, TouchableOpacity, Switch, LayoutAnimation } from "react-native";
import FontAwesome from "@react-native-vector-icons/fontawesome";
import { useI18n } from "../../../../hooks/useI18n";
import { setupDeviceParameterValuesService } from "../../../../api/services/setupDeviceParameterValuesService";

interface TimeSlotData {
  start: string;
  end: string;
  gridCharge: boolean;
  genCharge: boolean;
  maxDischarge: string;
  minBatterySOC: string;
  minBatteryVoltage: string;
}

interface TimeSlotParams {
  startParam: { id: string; valueId: string; modelNumber: string } | null;
  endParam: { id: string; valueId: string; modelNumber: string } | null;
  gridChargeParam: { id: string; valueId: string; modelNumber: string } | null;
  genChargeParam: { id: string; valueId: string; modelNumber: string } | null;
  maxDischargeParam: { id: string; valueId: string; modelNumber: string } | null;
  minBatterySOCParam: { id: string; valueId: string; modelNumber: string } | null;
  minBatteryVoltageParam: { id: string; valueId: string; modelNumber: string } | null;
}

interface TimeSlotHas {
  hasStart: boolean;
  hasEnd: boolean;
  hasGridCharge: boolean;
  hasGenCharge: boolean;
  hasMaxDischarge: boolean;
  hasMinBatterySOC: boolean;
  hasMinBatteryVoltage: boolean;
}

const DAYS = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"];
const DAY_LABELS: Record<string, string> = {
  Monday: "Mon",
  Tuesday: "Tue",
  Wednesday: "Wed",
  Thursday: "Thu",
  Friday: "Fri",
  Saturday: "Sat",
  Sunday: "Sun",
};

export default function TimeOfUseCard({ styles, COLORS, deviceParams }: any) {
  const [showTimeOfUse, setShowTimeOfUse] = useState(false);
  const [showActiveDays, setShowActiveDays] = useState(false);
  const [expandedSlot, setExpandedSlot] = useState<number | null>(null);
  const { t } = useI18n();

  // TimeOfUseEnabled
  const [touEnabled, setTouEnabled] = useState(false);
  const [touEnabledParam, setTouEnabledParam] = useState<{ id: string; valueId: string; modelNumber: string } | null>(null);
  const [hasTouEnabled, setHasTouEnabled] = useState(false);

  // Active Days
  const [activeDays, setActiveDays] = useState<Record<string, boolean>>({
    Monday: false,
    Tuesday: false,
    Wednesday: false,
    Thursday: false,
    Friday: false,
    Saturday: false,
    Sunday: false,
  });
  const [dayParams, setDayParams] = useState<Record<string, { id: string; valueId: string; modelNumber: string } | null>>({});
  const [hasDays, setHasDays] = useState<Record<string, boolean>>({});

  // Time Slots (6 slots)
  const [timeSlots, setTimeSlots] = useState<TimeSlotData[]>(
    Array(6).fill(null).map(() => ({
      start: "00:00",
      end: "00:00",
      gridCharge: false,
      genCharge: false,
      maxDischarge: "0",
      minBatterySOC: "0",
      minBatteryVoltage: "0",
    }))
  );
  const [slotParams, setSlotParams] = useState<TimeSlotParams[]>(
    Array(6).fill(null).map(() => ({
      startParam: null,
      endParam: null,
      gridChargeParam: null,
      genChargeParam: null,
      maxDischargeParam: null,
      minBatterySOCParam: null,
      minBatteryVoltageParam: null,
    }))
  );
  const [slotHas, setSlotHas] = useState<TimeSlotHas[]>(
    Array(6).fill(null).map(() => ({
      hasStart: false,
      hasEnd: false,
      hasGridCharge: false,
      hasGenCharge: false,
      hasMaxDischarge: false,
      hasMinBatterySOC: false,
      hasMinBatteryVoltage: false,
    }))
  );

  useEffect(() => {
    if (!deviceParams?.data || !deviceParams?.findParameterByName) return;

    // TimeOfUseEnabled
    const touEnabledFound = deviceParams.findParameterByName("TimeOfUseEnabled");
    if (touEnabledFound?.setupDeviceParameterValue) {
      setTouEnabledParam({
        id: touEnabledFound.id,
        valueId: touEnabledFound.setupDeviceParameterValue.id,
        modelNumber: touEnabledFound.setupDeviceParameterValue.modelNumber,
      });
      setTouEnabled(touEnabledFound.setupDeviceParameterValue.value === "true");
      setHasTouEnabled(true);
    }

    // Active Days
    const newDayParams: Record<string, { id: string; valueId: string; modelNumber: string } | null> = {};
    const newHasDays: Record<string, boolean> = {};
    const newActiveDays: Record<string, boolean> = {
      Monday: false,
      Tuesday: false,
      Wednesday: false,
      Thursday: false,
      Friday: false,
      Saturday: false,
      Sunday: false,
    };

    DAYS.forEach((day) => {
      const dayFound = deviceParams.findParameterByName(day);
      if (dayFound?.setupDeviceParameterValue) {
        newDayParams[day] = {
          id: dayFound.id,
          valueId: dayFound.setupDeviceParameterValue.id,
          modelNumber: dayFound.setupDeviceParameterValue.modelNumber,
        };
        newActiveDays[day] = dayFound.setupDeviceParameterValue.value === "true";
        newHasDays[day] = true;
      } else {
        newHasDays[day] = false;
      }
    });

    setDayParams(newDayParams);
    setHasDays(newHasDays);
    setActiveDays(newActiveDays);

    // Time Slots - create fresh arrays
    const newTimeSlots: TimeSlotData[] = Array(6).fill(null).map(() => ({
      start: "00:00",
      end: "00:00",
      gridCharge: false,
      genCharge: false,
      maxDischarge: "0",
      minBatterySOC: "0",
      minBatteryVoltage: "0",
    }));
    const newSlotParams: TimeSlotParams[] = Array(6).fill(null).map(() => ({
      startParam: null,
      endParam: null,
      gridChargeParam: null,
      genChargeParam: null,
      maxDischargeParam: null,
      minBatterySOCParam: null,
      minBatteryVoltageParam: null,
    }));
    const newSlotHas: TimeSlotHas[] = Array(6).fill(null).map(() => ({
      hasStart: false,
      hasEnd: false,
      hasGridCharge: false,
      hasGenCharge: false,
      hasMaxDischarge: false,
      hasMinBatterySOC: false,
      hasMinBatteryVoltage: false,
    }));

    for (let i = 1; i <= 6; i++) {
      const idx = i - 1;
      const prefix = `TimeSlot${i}`;

      // Start
      const startFound = deviceParams.findParameterByName(`${prefix}Start`);
      if (startFound?.setupDeviceParameterValue) {
        newSlotParams[idx].startParam = {
          id: startFound.id,
          valueId: startFound.setupDeviceParameterValue.id,
          modelNumber: startFound.setupDeviceParameterValue.modelNumber,
        };
        newTimeSlots[idx].start = startFound.setupDeviceParameterValue.value || "00:00";
        newSlotHas[idx].hasStart = true;
      }

      // End
      const endFound = deviceParams.findParameterByName(`${prefix}End`);
      if (endFound?.setupDeviceParameterValue) {
        newSlotParams[idx].endParam = {
          id: endFound.id,
          valueId: endFound.setupDeviceParameterValue.id,
          modelNumber: endFound.setupDeviceParameterValue.modelNumber,
        };
        newTimeSlots[idx].end = endFound.setupDeviceParameterValue.value || "00:00";
        newSlotHas[idx].hasEnd = true;
      }

      // GridCharge
      const gridChargeFound = deviceParams.findParameterByName(`${prefix}GridCharge`);
      if (gridChargeFound?.setupDeviceParameterValue) {
        newSlotParams[idx].gridChargeParam = {
          id: gridChargeFound.id,
          valueId: gridChargeFound.setupDeviceParameterValue.id,
          modelNumber: gridChargeFound.setupDeviceParameterValue.modelNumber,
        };
        newTimeSlots[idx].gridCharge = gridChargeFound.setupDeviceParameterValue.value === "true";
        newSlotHas[idx].hasGridCharge = true;
      }

      // GenCharge
      const genChargeFound = deviceParams.findParameterByName(`${prefix}GenCharge`);
      if (genChargeFound?.setupDeviceParameterValue) {
        newSlotParams[idx].genChargeParam = {
          id: genChargeFound.id,
          valueId: genChargeFound.setupDeviceParameterValue.id,
          modelNumber: genChargeFound.setupDeviceParameterValue.modelNumber,
        };
        newTimeSlots[idx].genCharge = genChargeFound.setupDeviceParameterValue.value === "true";
        newSlotHas[idx].hasGenCharge = true;
      }

      // MaxDischarge
      const maxDischargeFound = deviceParams.findParameterByName(`${prefix}MaxDischarge`);
      if (maxDischargeFound?.setupDeviceParameterValue) {
        newSlotParams[idx].maxDischargeParam = {
          id: maxDischargeFound.id,
          valueId: maxDischargeFound.setupDeviceParameterValue.id,
          modelNumber: maxDischargeFound.setupDeviceParameterValue.modelNumber,
        };
        newTimeSlots[idx].maxDischarge = maxDischargeFound.setupDeviceParameterValue.value || "0";
        newSlotHas[idx].hasMaxDischarge = true;
      }

      // MinBatterySOC
      const minBatterySOCFound = deviceParams.findParameterByName(`${prefix}MinBatterySOC`);
      if (minBatterySOCFound?.setupDeviceParameterValue) {
        newSlotParams[idx].minBatterySOCParam = {
          id: minBatterySOCFound.id,
          valueId: minBatterySOCFound.setupDeviceParameterValue.id,
          modelNumber: minBatterySOCFound.setupDeviceParameterValue.modelNumber,
        };
        newTimeSlots[idx].minBatterySOC = minBatterySOCFound.setupDeviceParameterValue.value || "0";
        newSlotHas[idx].hasMinBatterySOC = true;
      }

      // MinBatteryVoltage
      const minBatteryVoltageFound = deviceParams.findParameterByName(`${prefix}MinBatteryVoltage`);
      if (minBatteryVoltageFound?.setupDeviceParameterValue) {
        newSlotParams[idx].minBatteryVoltageParam = {
          id: minBatteryVoltageFound.id,
          valueId: minBatteryVoltageFound.setupDeviceParameterValue.id,
          modelNumber: minBatteryVoltageFound.setupDeviceParameterValue.modelNumber,
        };
        newTimeSlots[idx].minBatteryVoltage = minBatteryVoltageFound.setupDeviceParameterValue.value || "0";
        newSlotHas[idx].hasMinBatteryVoltage = true;
      }
    }

    setTimeSlots(newTimeSlots);
    setSlotParams(newSlotParams);
    setSlotHas(newSlotHas);
  }, [deviceParams?.data, deviceParams?.findParameterByName]);

  const handleTouEnabledChange = async (value: boolean) => {
    setTouEnabled(value);
    if (touEnabledParam) {
      try {
        await setupDeviceParameterValuesService.update(touEnabledParam.valueId, {
          id: touEnabledParam.valueId,
          value: value ? "true" : "false",
          sortOrder: 0,
          modelNumber: touEnabledParam.modelNumber,
        });
      } catch (error) {
        console.error("Failed to update TimeOfUseEnabled:", error);
      }
    }
  };

  const handleDayChange = async (day: string, value: boolean) => {
    setActiveDays((prev) => ({ ...prev, [day]: value }));
    const param = dayParams[day];
    if (param) {
      try {
        await setupDeviceParameterValuesService.update(param.valueId, {
          id: param.valueId,
          value: value ? "true" : "false",
          sortOrder: 0,
          modelNumber: param.modelNumber,
        });
      } catch (error) {
        console.error(`Failed to update ${day}:`, error);
      }
    }
  };

  const handleSlotGridChargeChange = async (slotIndex: number, value: boolean) => {
    const newTimeSlots = [...timeSlots];
    newTimeSlots[slotIndex].gridCharge = value;
    setTimeSlots(newTimeSlots);

    const param = slotParams[slotIndex].gridChargeParam;
    if (param) {
      try {
        await setupDeviceParameterValuesService.update(param.valueId, {
          id: param.valueId,
          value: value ? "true" : "false",
          sortOrder: 0,
          modelNumber: param.modelNumber,
        });
      } catch (error) {
        console.error(`Failed to update TimeSlot${slotIndex + 1}GridCharge:`, error);
      }
    }
  };

  const handleSlotGenChargeChange = async (slotIndex: number, value: boolean) => {
    const newTimeSlots = [...timeSlots];
    newTimeSlots[slotIndex].genCharge = value;
    setTimeSlots(newTimeSlots);

    const param = slotParams[slotIndex].genChargeParam;
    if (param) {
      try {
        await setupDeviceParameterValuesService.update(param.valueId, {
          id: param.valueId,
          value: value ? "true" : "false",
          sortOrder: 0,
          modelNumber: param.modelNumber,
        });
      } catch (error) {
        console.error(`Failed to update TimeSlot${slotIndex + 1}GenCharge:`, error);
      }
    }
  };

  // Check if any time slot has data
  const hasAnyTimeSlot = slotHas.some(
    (s) =>
      s.hasStart ||
      s.hasEnd ||
      s.hasGridCharge ||
      s.hasGenCharge ||
      s.hasMaxDischarge ||
      s.hasMinBatterySOC ||
      s.hasMinBatteryVoltage
  );

  // Check if any day has data
  const hasAnyDay = Object.values(hasDays).some((v) => v);

  // Check if we have any TOU data
  const hasAnyTouData = hasTouEnabled || hasAnyDay || hasAnyTimeSlot;

  if (!hasAnyTouData) {
    return null;
  }

  const renderTimeSlot = (index: number) => {
    const slot = timeSlots[index];
    const has = slotHas[index];
    const hasAnySlotData =
      has.hasStart ||
      has.hasEnd ||
      has.hasGridCharge ||
      has.hasGenCharge ||
      has.hasMaxDischarge ||
      has.hasMinBatterySOC ||
      has.hasMinBatteryVoltage;

    if (!hasAnySlotData) return null;

    const isExpanded = expandedSlot === index;

    return (
      <View key={`slot-${index}`} style={{ marginBottom: 8 }}>
        <TouchableOpacity
          activeOpacity={0.8}
          onPress={() => {
            LayoutAnimation.configureNext(LayoutAnimation.Presets.easeInEaseOut);
            setExpandedSlot(isExpanded ? null : index);
          }}
        >
          <View
            style={{
              flexDirection: "row",
              justifyContent: "space-between",
              alignItems: "center",
              paddingVertical: 10,
              paddingHorizontal: 12,
              backgroundColor: COLORS.surface,
              borderRadius: 8,
              borderWidth: 1,
              borderColor: COLORS.border,
            }}
          >
            <View style={{ flexDirection: "row", alignItems: "center", gap: 8 }}>
              <Text style={{ color: COLORS.text, fontWeight: "600" }}>
                Time Slot {index + 1}
              </Text>
              {(has.hasStart || has.hasEnd) && (
                <Text style={{ color: COLORS.textSoft, fontSize: 12 }}>
                  ({slot.start} - {slot.end})
                </Text>
              )}
            </View>
            <FontAwesome
              name={isExpanded ? "chevron-up" : "chevron-down"}
              size={14}
              color={COLORS.textSoft}
            />
          </View>
        </TouchableOpacity>

        {isExpanded && (
          <View
            style={{
              marginTop: 8,
              padding: 12,
              backgroundColor: COLORS.surface,
              borderRadius: 8,
              borderWidth: 1,
              borderColor: COLORS.border,
            }}
          >
            {(has.hasStart || has.hasEnd) && (
              <View style={{ marginBottom: 12 }}>
                <Text style={{ color: COLORS.textSoft, fontSize: 13, marginBottom: 4 }}>
                  Time Range
                </Text>
                <Text style={{ color: COLORS.text, fontWeight: "600" }}>
                  {slot.start} - {slot.end}
                </Text>
              </View>
            )}

            {has.hasGridCharge && (
              <View style={styles.switchRow}>
                <Text style={styles.label}>Grid Charge</Text>
                <Switch
                  value={slot.gridCharge}
                  onValueChange={(v) => handleSlotGridChargeChange(index, v)}
                  thumbColor={slot.gridCharge ? COLORS.accent : "#ccc"}
                  trackColor={{ false: "#ccc", true: COLORS.accent + "66" }}
                />
              </View>
            )}

            {has.hasGenCharge && (
              <View style={styles.switchRow}>
                <Text style={styles.label}>Generator Charge</Text>
                <Switch
                  value={slot.genCharge}
                  onValueChange={(v) => handleSlotGenChargeChange(index, v)}
                  thumbColor={slot.genCharge ? COLORS.accent : "#ccc"}
                  trackColor={{ false: "#ccc", true: COLORS.accent + "66" }}
                />
              </View>
            )}

            {has.hasMaxDischarge && (
              <Text style={styles.valueText}>
                Max Discharge: <Text style={styles.valueAccent}>{slot.maxDischarge} W</Text>
              </Text>
            )}

            {has.hasMinBatterySOC && (
              <Text style={styles.valueText}>
                Min Battery SOC: <Text style={styles.valueAccent}>{slot.minBatterySOC}%</Text>
              </Text>
            )}

            {has.hasMinBatteryVoltage && (
              <Text style={styles.valueText}>
                Min Battery Voltage: <Text style={styles.valueAccent}>{slot.minBatteryVoltage} V</Text>
              </Text>
            )}
          </View>
        )}
      </View>
    );
  };

  return (
    <View style={styles.mainCard}>
      <Text style={styles.cardTitle}>{t("tou.title")}</Text>

      {/* TOU Enabled Toggle */}
      {hasTouEnabled && (
        <View style={styles.dropdownRow}>
          <Text style={styles.subCardTitle}>Time-of-Use Enabled</Text>
          <Switch
            value={touEnabled}
            onValueChange={handleTouEnabledChange}
            thumbColor={touEnabled ? COLORS.accent : "#ccc"}
            trackColor={{ false: "#ccc", true: COLORS.accent + "66" }}
          />
        </View>
      )}

      {/* Active Days Section */}
      {hasAnyDay && (
        <>
          <TouchableOpacity
            activeOpacity={0.8}
            onPress={() => {
              LayoutAnimation.configureNext(LayoutAnimation.Presets.easeInEaseOut);
              setShowActiveDays(!showActiveDays);
            }}
          >
            <View style={styles.dropdownRow}>
              <Text style={styles.subCardTitle}>Active Days</Text>
              <FontAwesome
                name={showActiveDays ? "chevron-up" : "chevron-down"}
                size={16}
                color={COLORS.textSoft}
              />
            </View>
          </TouchableOpacity>

          {showActiveDays && (
            <View style={styles.subCard}>
              <View
                style={{
                  flexDirection: "row",
                  flexWrap: "wrap",
                  gap: 8,
                }}
              >
                {DAYS.map((day) => {
                  if (!hasDays[day]) return null;
                  return (
                    <TouchableOpacity
                      key={day}
                      onPress={() => handleDayChange(day, !activeDays[day])}
                      style={{
                        paddingHorizontal: 12,
                        paddingVertical: 8,
                        borderRadius: 8,
                        backgroundColor: activeDays[day] ? COLORS.accent : COLORS.surface,
                        borderWidth: 1,
                        borderColor: activeDays[day] ? COLORS.accent : COLORS.border,
                      }}
                    >
                      <Text
                        style={{
                          color: activeDays[day] ? "#fff" : COLORS.text,
                          fontWeight: "600",
                          fontSize: 13,
                        }}
                      >
                        {DAY_LABELS[day]}
                      </Text>
                    </TouchableOpacity>
                  );
                })}
              </View>
            </View>
          )}
        </>
      )}

      {/* Time Slots Section */}
      {hasAnyTimeSlot && (
        <>
          <TouchableOpacity
            activeOpacity={0.8}
            onPress={() => {
              LayoutAnimation.configureNext(LayoutAnimation.Presets.easeInEaseOut);
              setShowTimeOfUse(!showTimeOfUse);
            }}
          >
            <View style={styles.dropdownRow}>
              <Text style={styles.subCardTitle}>Time Slots</Text>
              <FontAwesome
                name={showTimeOfUse ? "chevron-up" : "chevron-down"}
                size={16}
                color={COLORS.textSoft}
              />
            </View>
          </TouchableOpacity>

          {showTimeOfUse && (
            <View style={styles.subCard}>
              {Array.from({ length: 6 }).map((_, idx) => renderTimeSlot(idx))}
            </View>
          )}
        </>
      )}
    </View>
  );
}
