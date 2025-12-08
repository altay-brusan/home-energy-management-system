import React, { useState } from 'react';
import { View, Text, TextInput, TouchableOpacity, StyleSheet, ScrollView } from 'react-native';
import { useDeviceStatus } from '../../../../hooks/useDeviceStatus';
import { useThemeStore } from '../../../../store/themeStore';
import { getThemeColors } from '../../../../theme/constants';

export const DeviceStatusDebug: React.FC = () => {
  const mode = useThemeStore(s => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);

  const [manualDeviceId, setManualDeviceId] = useState('');
  const [activeDeviceId, setActiveDeviceId] = useState<string | undefined>(undefined);

  const {
    isConnected,
    connectionState,
    deviceStatus,
    missedHeartbeats,
    lastUpdate,
    isInitialStatus,
  } = useDeviceStatus(activeDeviceId);

  const handleConnect = () => {
    if (manualDeviceId.trim()) {
      setActiveDeviceId(manualDeviceId.trim());
    }
  };

  const handleDisconnect = () => {
    setActiveDeviceId(undefined);
  };

  return (
    <ScrollView style={[styles.container, { backgroundColor: COLORS.cardBg }]}>
      <Text style={[styles.title, { color: COLORS.text }]}>🔧 Device Status Debug</Text>

      {/* Input Section */}
      <View style={styles.inputSection}>
        <Text style={[styles.label, { color: COLORS.textSoft }]}>Device ID:</Text>
        <TextInput
          style={[styles.input, { backgroundColor: COLORS.bg, color: COLORS.text, borderColor: COLORS.border }]}
          value={manualDeviceId}
          onChangeText={setManualDeviceId}
          placeholder="Enter device ID (GUID)"
          placeholderTextColor={COLORS.textSoft}
          autoCapitalize="none"
          autoCorrect={false}
        />
        <View style={styles.buttonRow}>
          <TouchableOpacity
            style={[styles.button, { backgroundColor: COLORS.accent }]}
            onPress={handleConnect}
          >
            <Text style={styles.buttonText}>Subscribe</Text>
          </TouchableOpacity>
          <TouchableOpacity
            style={[styles.button, { backgroundColor: COLORS.error || '#FF4444' }]}
            onPress={handleDisconnect}
          >
            <Text style={styles.buttonText}>Unsubscribe</Text>
          </TouchableOpacity>
        </View>
      </View>

      {/* Status Section */}
      <View style={styles.statusSection}>
        <Text style={[styles.sectionTitle, { color: COLORS.text }]}>📡 Connection Status</Text>

        <View style={styles.row}>
          <Text style={[styles.key, { color: COLORS.textSoft }]}>Active Device ID:</Text>
          <Text style={[styles.value, { color: COLORS.text }]}>{activeDeviceId || 'None'}</Text>
        </View>

        <View style={styles.row}>
          <Text style={[styles.key, { color: COLORS.textSoft }]}>Connection State:</Text>
          <Text style={[styles.value, { color: connectionState === 'Connected' ? COLORS.success : COLORS.warning }]}>
            {connectionState}
          </Text>
        </View>

        <View style={styles.row}>
          <Text style={[styles.key, { color: COLORS.textSoft }]}>Is Connected:</Text>
          <Text style={[styles.value, { color: isConnected ? COLORS.success : COLORS.error }]}>
            {isConnected ? '✅ Yes' : '❌ No'}
          </Text>
        </View>
      </View>

      {/* Device Status Section */}
      <View style={styles.statusSection}>
        <Text style={[styles.sectionTitle, { color: COLORS.text }]}>📊 Device Status (from SignalR)</Text>

        <View style={styles.row}>
          <Text style={[styles.key, { color: COLORS.textSoft }]}>Status:</Text>
          <Text style={[styles.value, { color: deviceStatus ? COLORS.success : COLORS.textSoft }]}>
            {deviceStatus || 'Waiting for data...'}
          </Text>
        </View>

        <View style={styles.row}>
          <Text style={[styles.key, { color: COLORS.textSoft }]}>Missed Heartbeats:</Text>
          <Text style={[styles.value, { color: COLORS.text }]}>{missedHeartbeats}</Text>
        </View>

        <View style={styles.row}>
          <Text style={[styles.key, { color: COLORS.textSoft }]}>Last Update:</Text>
          <Text style={[styles.value, { color: COLORS.text }]}>{lastUpdate || 'Never'}</Text>
        </View>

        <View style={styles.row}>
          <Text style={[styles.key, { color: COLORS.textSoft }]}>Is Initial Status:</Text>
          <Text style={[styles.value, { color: COLORS.text }]}>{isInitialStatus ? 'Yes' : 'No'}</Text>
        </View>
      </View>

      {/* Raw Data Section */}
      <View style={styles.statusSection}>
        <Text style={[styles.sectionTitle, { color: COLORS.text }]}>📋 Raw Data</Text>
        <View style={[styles.codeBlock, { backgroundColor: COLORS.bg }]}>
          <Text style={[styles.code, { color: COLORS.text }]}>
            {JSON.stringify({
              activeDeviceId,
              connectionState,
              isConnected,
              deviceStatus,
              missedHeartbeats,
              lastUpdate,
              isInitialStatus,
            }, null, 2)}
          </Text>
        </View>
      </View>

      <Text style={[styles.hint, { color: COLORS.textSoft }]}>
        💡 Check console for detailed SignalR logs
      </Text>
    </ScrollView>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    padding: 16,
    borderRadius: 12,
    margin: 8,
  },
  title: {
    fontSize: 18,
    fontWeight: '700',
    marginBottom: 16,
  },
  inputSection: {
    marginBottom: 20,
  },
  label: {
    fontSize: 14,
    marginBottom: 8,
  },
  input: {
    borderWidth: 1,
    borderRadius: 8,
    padding: 12,
    fontSize: 14,
    marginBottom: 12,
  },
  buttonRow: {
    flexDirection: 'row',
    gap: 12,
  },
  button: {
    flex: 1,
    padding: 12,
    borderRadius: 8,
    alignItems: 'center',
  },
  buttonText: {
    color: '#FFFFFF',
    fontWeight: '600',
  },
  statusSection: {
    marginBottom: 20,
    padding: 12,
    borderRadius: 8,
    backgroundColor: 'rgba(0,0,0,0.05)',
  },
  sectionTitle: {
    fontSize: 16,
    fontWeight: '600',
    marginBottom: 12,
  },
  row: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    marginBottom: 8,
  },
  key: {
    fontSize: 14,
  },
  value: {
    fontSize: 14,
    fontWeight: '600',
  },
  codeBlock: {
    padding: 12,
    borderRadius: 8,
  },
  code: {
    fontFamily: 'monospace',
    fontSize: 12,
  },
  hint: {
    fontSize: 12,
    textAlign: 'center',
    marginTop: 8,
    marginBottom: 20,
  },
});

export default DeviceStatusDebug;
