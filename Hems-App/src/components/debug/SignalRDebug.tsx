import React from 'react';
import { View, Text, TouchableOpacity, StyleSheet } from 'react-native';
import { useSignalRContext } from '../../providers/SignalRProvider';
import { useAlertStore } from '../../store/alertStore';
import { signalRService } from '../../services/signalRService';

export const SignalRDebug = () => {
    const { isConnected, connectionState } = useSignalRContext();
    const showAlert = useAlertStore(s => s.show);

    const testNotification = () => {
        console.log('[SignalRDebug] Testing notification...');
        showAlert({
            title: 'E001 - Battery Low Warning',
            message: 'Battery level has dropped below 20%. Please check your energy settings.',
            level: 'warning',
            deviceName: 'Inverter-001',
        });
    };

    return (
        <View style={styles.container}>
            <Text style={styles.title}>SignalR Debug</Text>
            <View style={styles.row}>
                <Text style={styles.label}>Connection:</Text>
                <Text style={[styles.value, { color: isConnected ? '#4CAF50' : '#f44336' }]}>
                    {connectionState}
                </Text>
            </View>
            <View style={styles.row}>
                <Text style={styles.label}>Current Site:</Text>
                <Text style={styles.value}>
                    {signalRService.currentSite || 'None'}
                </Text>
            </View>
            <TouchableOpacity style={styles.button} onPress={testNotification}>
                <Text style={styles.buttonText}>Test Notification</Text>
            </TouchableOpacity>
        </View>
    );
};

const styles = StyleSheet.create({
    container: {
        backgroundColor: '#1a1a2e',
        padding: 12,
        margin: 8,
        borderRadius: 8,
        borderWidth: 1,
        borderColor: '#333',
    },
    title: {
        color: '#fff',
        fontSize: 14,
        fontWeight: 'bold',
        marginBottom: 8,
    },
    row: {
        flexDirection: 'row',
        justifyContent: 'space-between',
        marginBottom: 4,
    },
    label: {
        color: '#888',
        fontSize: 12,
    },
    value: {
        color: '#fff',
        fontSize: 12,
        fontWeight: '600',
    },
    button: {
        backgroundColor: '#3498db',
        padding: 10,
        borderRadius: 6,
        marginTop: 8,
        alignItems: 'center',
    },
    buttonText: {
        color: '#fff',
        fontWeight: 'bold',
        fontSize: 12,
    },
});
