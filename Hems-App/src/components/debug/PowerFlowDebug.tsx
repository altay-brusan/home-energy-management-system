import React, { useState } from 'react';
import { View, Text, TextInput, TouchableOpacity, ScrollView, StyleSheet } from 'react-native';
import { usePowerFlowStream } from '../../hooks/usePowerFlowStream';

export const PowerFlowDebug = () => {
  const [deviceId, setDeviceId] = useState('');
  const [isEnabled, setIsEnabled] = useState(false);

  const {
    powerFlow,
    isStreaming,
    streamState,
    connectionState,
    error,
    startStream,
    stopStream
  } = usePowerFlowStream({
    deviceId: deviceId || null,
    intervalSeconds: 5,
    enabled: isEnabled,
  });

  const handleStart = () => {
    if (deviceId.trim()) {
      setIsEnabled(true);
    } else {
      alert('Please enter a device ID');
    }
  };

  const handleStop = () => {
    setIsEnabled(false);
    stopStream();
  };

  return (
    <ScrollView style={styles.container}>
      <Text style={styles.title}>🔬 PowerFlow Stream Test</Text>

      {/* Input */}
      <View style={styles.inputGroup}>
        <Text style={styles.label}>Device ID (GUID):</Text>
        <TextInput
          style={styles.input}
          value={deviceId}
          onChangeText={setDeviceId}
          placeholder="Enter device GUID..."
          autoCapitalize="none"
          autoCorrect={false}
        />
      </View>

      {/* Controls */}
      <View style={styles.buttonRow}>
        <TouchableOpacity
          style={[styles.button, styles.startButton]}
          onPress={handleStart}
          disabled={isStreaming}
        >
          <Text style={styles.buttonText}>
            {isStreaming ? '▶️ Streaming...' : '▶️ Start Stream'}
          </Text>
        </TouchableOpacity>

        <TouchableOpacity
          style={[styles.button, styles.stopButton]}
          onPress={handleStop}
          disabled={!isStreaming}
        >
          <Text style={styles.buttonText}>⏹️ Stop</Text>
        </TouchableOpacity>
      </View>

      {/* Status */}
      <View style={styles.statusGroup}>
        <Text style={styles.statusTitle}>📊 Status</Text>
        <Text style={styles.statusText}>
          Connection: <Text style={connectionState === 'Connected' ? styles.success : styles.error}>
            {connectionState}
          </Text>
        </Text>
        <Text style={styles.statusText}>
          Stream: <Text style={isStreaming ? styles.success : styles.muted}>
            {streamState}
          </Text>
        </Text>
        <Text style={styles.statusText}>
          Live: {isStreaming ? '🔴' : '⚫'}
        </Text>
      </View>

      {/* Error */}
      {error && (
        <View style={styles.errorBox}>
          <Text style={styles.errorTitle}>❌ Error</Text>
          <Text style={styles.errorText}>{error.message}</Text>
        </View>
      )}

      {/* Data */}
      {powerFlow && (
        <View style={styles.dataBox}>
          <Text style={styles.dataTitle}>⚡ Live PowerFlow Data</Text>
          <Text style={styles.dataText}>Device ID: {powerFlow.deviceId}</Text>
          <Text style={styles.dataText}>Timestamp: {new Date(powerFlow.timestamp).toLocaleTimeString()}</Text>
          <View style={styles.separator} />
          <Text style={styles.dataText}>🔌 Grid Power: {powerFlow.gridPower.toFixed(2)} W</Text>
          <Text style={styles.dataText}>☀️ Solar Power: {powerFlow.solarPower.toFixed(2)} W</Text>
          <Text style={styles.dataText}>🔋 Battery Power: {powerFlow.batteryPower.toFixed(2)} W</Text>
          <Text style={styles.dataText}>💡 Load Power: {powerFlow.loadPower.toFixed(2)} W</Text>
          <View style={styles.separator} />
          <Text style={styles.dataText}>🔋 Battery SOC: {powerFlow.batterySOC.toFixed(1)}%</Text>
          {powerFlow.batteryStatus && (
            <Text style={styles.dataText}>Battery Status: {powerFlow.batteryStatus}</Text>
          )}
          {powerFlow.gridStatus && (
            <Text style={styles.dataText}>Grid Status: {powerFlow.gridStatus}</Text>
          )}
        </View>
      )}

      {/* Console Logs */}
      <View style={styles.infoBox}>
        <Text style={styles.infoText}>
          💡 Tip: Check console logs for detailed stream events
        </Text>
      </View>
    </ScrollView>
  );
};

const styles = StyleSheet.create({
  container: {
    padding: 16,
    backgroundColor: '#f5f5f5',
  },
  title: {
    fontSize: 20,
    fontWeight: 'bold',
    marginBottom: 16,
    textAlign: 'center',
  },
  inputGroup: {
    marginBottom: 16,
  },
  label: {
    fontSize: 14,
    fontWeight: '600',
    marginBottom: 8,
    color: '#333',
  },
  input: {
    backgroundColor: 'white',
    borderWidth: 1,
    borderColor: '#ddd',
    borderRadius: 8,
    padding: 12,
    fontSize: 14,
  },
  buttonRow: {
    flexDirection: 'row',
    gap: 12,
    marginBottom: 16,
  },
  button: {
    flex: 1,
    padding: 12,
    borderRadius: 8,
    alignItems: 'center',
  },
  startButton: {
    backgroundColor: '#4CAF50',
  },
  stopButton: {
    backgroundColor: '#f44336',
  },
  buttonText: {
    color: 'white',
    fontWeight: 'bold',
    fontSize: 14,
  },
  statusGroup: {
    backgroundColor: 'white',
    padding: 16,
    borderRadius: 8,
    marginBottom: 16,
  },
  statusTitle: {
    fontSize: 16,
    fontWeight: 'bold',
    marginBottom: 8,
  },
  statusText: {
    fontSize: 14,
    marginBottom: 4,
    color: '#333',
  },
  success: {
    color: '#4CAF50',
    fontWeight: 'bold',
  },
  error: {
    color: '#f44336',
    fontWeight: 'bold',
  },
  muted: {
    color: '#999',
  },
  errorBox: {
    backgroundColor: '#ffebee',
    padding: 16,
    borderRadius: 8,
    marginBottom: 16,
    borderLeftWidth: 4,
    borderLeftColor: '#f44336',
  },
  errorTitle: {
    fontSize: 16,
    fontWeight: 'bold',
    color: '#f44336',
    marginBottom: 8,
  },
  errorText: {
    fontSize: 14,
    color: '#c62828',
  },
  dataBox: {
    backgroundColor: '#e8f5e9',
    padding: 16,
    borderRadius: 8,
    marginBottom: 16,
    borderLeftWidth: 4,
    borderLeftColor: '#4CAF50',
  },
  dataTitle: {
    fontSize: 16,
    fontWeight: 'bold',
    color: '#2e7d32',
    marginBottom: 12,
  },
  dataText: {
    fontSize: 14,
    color: '#1b5e20',
    marginBottom: 6,
    fontFamily: 'monospace',
  },
  separator: {
    height: 1,
    backgroundColor: '#a5d6a7',
    marginVertical: 8,
  },
  infoBox: {
    backgroundColor: '#e3f2fd',
    padding: 12,
    borderRadius: 8,
    marginBottom: 16,
  },
  infoText: {
    fontSize: 12,
    color: '#1565c0',
    textAlign: 'center',
  },
});
