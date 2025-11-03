import React, { useEffect, useState } from "react";
import { LayoutAnimation, Text, TouchableOpacity, View } from "react-native";
import { setupDeviceParameterValuesService } from "../../../../api/services/setupDeviceParameterValuesService";
import { useI18n } from "../../../../hooks/useI18n";
import ModeSelectionPanel from "./ModeSelectionPanel";

export default function EnergyPriorityCard ({
  styles,
  COLORS,
  selectedMode,
  setSelectedMode,
  selectedGrid,
  setSelectedGrid,
  handleSave,
  deviceParams,
}: any) {
  const [showOperationPanel, setShowOperationPanel] = useState(false);
  const [showGridPanel, setShowGridPanel] = useState(false);
  const { t } = useI18n();
  const [operationModeParams, setOperationModeParams] = useState<{ [key: string]: { id: string, valueId: string } }>({});
  const [gridInteractionParams, setGridInteractionParams] = useState<{ [key: string]: { id: string, valueId: string } }>({});
  const [isSaving, setIsSaving] = useState(false);
  const [hasOperationMode, setHasOperationMode] = useState(false);
  const [hasGridInteraction, setHasGridInteraction] = useState(false);

  const getTranslatedMode = (mode: string) => {
    switch (mode) {
      case 'Battery Priority':
        return t('modeSelection.operation.batteryPriority.title');
      case 'Load Priority':
        return t('modeSelection.operation.loadPriority.title');
      case 'Custom':
        return t('modeSelection.operation.custom.title');
      default:
        return mode;
    }
  };

  useEffect(() => {
    if (!deviceParams?.data || !deviceParams?.findParameterByName) return;

    const batteryPriorityParam = deviceParams.findParameterByName("Battery Priorty,") || deviceParams.findParameterByName("Battery Priority");
    const loadPriorityParam = deviceParams.findParameterByName("Load Priority");
    const customParam = deviceParams.findParameterByName("Custom");

    const opModeParams: typeof operationModeParams = {};
    if (batteryPriorityParam?.setupDeviceParameterValue) {
      opModeParams["Battery Priority"] = {
        id: batteryPriorityParam.id,
        valueId: batteryPriorityParam.setupDeviceParameterValue.id,
      };
      if (batteryPriorityParam.setupDeviceParameterValue.value === "true") {
        setSelectedMode("Battery Priority");
      }
    }
    if (loadPriorityParam?.setupDeviceParameterValue) {
      opModeParams["Load Priority"] = {
        id: loadPriorityParam.id,
        valueId: loadPriorityParam.setupDeviceParameterValue.id,
      };
      if (loadPriorityParam.setupDeviceParameterValue.value === "true") {
        setSelectedMode("Load Priority");
      }
    }
    if (customParam?.setupDeviceParameterValue) {
      opModeParams["Custom"] = {
        id: customParam.id,
        valueId: customParam.setupDeviceParameterValue.id,
      };
      if (customParam.setupDeviceParameterValue.value === "true") {
        setSelectedMode("Custom");
      }
    }
    setOperationModeParams(opModeParams);
    setHasOperationMode(Object.keys(opModeParams).length > 0);

    const ieeeParam = deviceParams.findParameterByName("IEEE-1547");
    const vdeParam = deviceParams.findParameterByName("VDE-AR-N-4105");
    const as4777Param = deviceParams.findParameterByName("AS4777");

    const gridParams: typeof gridInteractionParams = {};
    if (ieeeParam?.setupDeviceParameterValue) {
      gridParams["IEEE-1547"] = {
        id: ieeeParam.id,
        valueId: ieeeParam.setupDeviceParameterValue.id,
      };
      if (ieeeParam.setupDeviceParameterValue.value === "true") {
        setSelectedGrid("IEEE-1547");
      }
    }
    if (vdeParam?.setupDeviceParameterValue) {
      gridParams["VDE-AR-N-4105"] = {
        id: vdeParam.id,
        valueId: vdeParam.setupDeviceParameterValue.id,
      };
      if (vdeParam.setupDeviceParameterValue.value === "true") {
        setSelectedGrid("VDE-AR-N-4105");
      }
    }
    if (as4777Param?.setupDeviceParameterValue) {
      gridParams["AS4777"] = {
        id: as4777Param.id,
        valueId: as4777Param.setupDeviceParameterValue.id,
      };
      if (as4777Param.setupDeviceParameterValue.value === "true") {
        setSelectedGrid("AS4777");
      }
    }
    setGridInteractionParams(gridParams);
    setHasGridInteraction(Object.keys(gridParams).length > 0);
  }, [deviceParams?.data]);

  const handleModeSelect = (mode: string) => {
    setSelectedMode(mode);
  };

  const handleGridSelect = (grid: string) => {
    setSelectedGrid(grid);
  };

  const handleOperationModeSave = async () => {
    if (isSaving) return;
    setIsSaving(true);

    try {
      if (deviceParams?.setupDeviceId) {
        let paramsToUse = operationModeParams;
        if (Object.keys(operationModeParams).length === 0 && deviceParams?.findParameterByName) {
          const batteryPriorityParam = deviceParams.findParameterByName("Battery Priorty,") || deviceParams.findParameterByName("Battery Priority");
          const loadPriorityParam = deviceParams.findParameterByName("Load Priority");
          const customParam = deviceParams.findParameterByName("Custom");

          const tempParams: typeof operationModeParams = {};
          if (batteryPriorityParam?.setupDeviceParameterValue) {
            tempParams["Battery Priority"] = {
              id: batteryPriorityParam.id,
              valueId: batteryPriorityParam.setupDeviceParameterValue.id,
            };
          }
          if (loadPriorityParam?.setupDeviceParameterValue) {
            tempParams["Load Priority"] = {
              id: loadPriorityParam.id,
              valueId: loadPriorityParam.setupDeviceParameterValue.id,
            };
          }
          if (customParam?.setupDeviceParameterValue) {
            tempParams["Custom"] = {
              id: customParam.id,
              valueId: customParam.setupDeviceParameterValue.id,
            };
          }
          if (Object.keys(tempParams).length > 0) {
            setOperationModeParams(tempParams);
            paramsToUse = tempParams;
          }
        }

        if (Object.keys(paramsToUse).length > 0) {
          const modeParam = paramsToUse[selectedMode];
          if (modeParam) {
            await setupDeviceParameterValuesService.update(modeParam.valueId, {
              id: modeParam.valueId,
              value: "true",
              sortOrder: 1,
              modelNumber: "NX-200",
            });

            for (const [key, param] of Object.entries(paramsToUse)) {
              if (key !== selectedMode) {
                await setupDeviceParameterValuesService.update(param.valueId, {
                  id: param.valueId,
                  value: "false",
                  sortOrder: 1,
                  modelNumber: "NX-200",
                });
              }
            }
          }
        }
      }

      handleSave(() => {
        setShowOperationPanel(false);
      });
    } catch (error) {
    } finally {
      setIsSaving(false);
    }
  };

  const handleGridInteractionSave = async () => {
    if (isSaving) return;
    setIsSaving(true);

    try {
      if (deviceParams?.setupDeviceId && Object.keys(gridInteractionParams).length > 0) {
        const gridParam = gridInteractionParams[selectedGrid];
        if (gridParam) {
          await setupDeviceParameterValuesService.update(gridParam.valueId, {
            id: gridParam.valueId,
            value: "true",
            sortOrder: 1,
            modelNumber: "NX-200",
          });

          for (const [key, param] of Object.entries(gridInteractionParams)) {
            if (key !== selectedGrid) {
              await setupDeviceParameterValuesService.update(param.valueId, {
                id: param.valueId,
                value: "false",
                sortOrder: 1,
                modelNumber: "NX-200",
              });
            }
          }
        }
      }

      handleSave(() => {
        setShowGridPanel(false);
      });
    } catch (error) {
    } finally {
      setIsSaving(false);
    }
  };

  if (!hasOperationMode && !hasGridInteraction) {
    return null;
  }

  return (
    <View style={styles.mainCard}>
      <Text style={styles.cardTitle}>{t('energyPriority.title')}</Text>

      {hasOperationMode && (
        <>
          <TouchableOpacity
            activeOpacity={0.8}
            onPress={() => {
              LayoutAnimation.configureNext(LayoutAnimation.Presets.easeInEaseOut);
              setShowOperationPanel(!showOperationPanel);
              if (showGridPanel) setShowGridPanel(false);
            }}
          >
            <View style={styles.dropdownRow}>
              <Text style={styles.subCardTitle}>{t('energyPriority.operationMode')}</Text>
              <Text style={styles.valueAccent}>{getTranslatedMode(selectedMode)}</Text>
            </View>
          </TouchableOpacity>

          {showOperationPanel && (
            <ModeSelectionPanel
              type="operation"
              selectedMode={selectedMode}
              onSelectMode={handleModeSelect}
              onSave={handleOperationModeSave}
              visible={showOperationPanel}
            />
          )}
        </>
      )}

      {hasGridInteraction && (
        <>
          <TouchableOpacity
            activeOpacity={0.8}
            onPress={() => {
              LayoutAnimation.configureNext(LayoutAnimation.Presets.easeInEaseOut);
              setShowGridPanel(!showGridPanel);
              if (showOperationPanel) setShowOperationPanel(false);
            }}
          >
            <View style={styles.dropdownRow}>
              <Text style={styles.subCardTitle}>{t('energyPriority.gridInteraction')}</Text>
              <Text style={styles.valueAccent}>{selectedGrid}</Text>
            </View>
          </TouchableOpacity>

          {showGridPanel && (
            <ModeSelectionPanel
              type="grid"
              selectedGrid={selectedGrid}
              onSelectGrid={handleGridSelect}
              onSave={handleGridInteractionSave}
              visible={showGridPanel}
            />
          )}
        </>
      )}
    </View>
  );
}
