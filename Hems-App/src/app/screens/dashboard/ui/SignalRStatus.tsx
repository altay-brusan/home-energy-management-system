import React, { useEffect, useState } from 'react';
import { View, Text, TouchableOpacity } from 'react-native';
import MaterialIcons from '@react-native-vector-icons/material-icons';
import { signalRService } from '../../../../services/signalRService';
import { SignalRConnectionState } from '../../../../api/types/signalR.types';
import { useSelectedSiteUtils } from '../../../../hooks/useSelectedSite';

interface SignalRStatusProps {
  COLORS: any;
}

export default function SignalRStatus({ COLORS }: SignalRStatusProps) {
  const [connectionState, setConnectionState] = useState<SignalRConnectionState>(signalRService.connectionState);
  const [error, setError] = useState<string | null>(null);
  const [isConnected, setIsConnected] = useState(signalRService.isConnected);
  const { selectedSite } = useSelectedSiteUtils();

  useEffect(() => {
    // İlk state'i al
    updateState();

    // Connection state listener ekle
    const unsubscribe = signalRService.addConnectionStateListener((state) => {
      setConnectionState(state);
      setIsConnected(signalRService.isConnected);
      setError(null);
    });

    // Periyodik kontrol (her 5 saniyede bir)
    const interval = setInterval(() => {
      updateState();
    }, 5000);

    return () => {
      unsubscribe();
      clearInterval(interval);
    };
  }, []);

  const updateState = () => {
    const state = signalRService.connectionState;
    setConnectionState(state);
    setIsConnected(signalRService.isConnected);
  };

  const getStatusIcon = () => {
    switch (connectionState) {
      case 'Connected':
        return 'check-circle';
      case 'Connecting':
      case 'Reconnecting':
        return 'sync';
      case 'Disconnected':
        return 'cancel';
      case 'Failed':
        return 'error';
      default:
        return 'help';
    }
  };

  const getStatusColor = () => {
    switch (connectionState) {
      case 'Connected':
        return COLORS.success || '#4CAF50';
      case 'Connecting':
      case 'Reconnecting':
        return COLORS.warning || '#FF9800';
      case 'Disconnected':
        return COLORS.disabled || '#757575';
      case 'Failed':
        return COLORS.error || '#F44336';
      default:
        return COLORS.textSoft || '#9E9E9E';
    }
  };

  const getStatusText = () => {
    switch (connectionState) {
      case 'Connected':
        return 'Connected';
      case 'Connecting':
        return 'Connecting...';
      case 'Reconnecting':
        return 'Reconnecting...';
      case 'Disconnected':
        return 'Disconnected';
      case 'Failed':
        return 'Connection Failed';
      default:
        return 'Unknown';
    }
  };

  const handleReconnect = async () => {
    setError(null);
    
    // Mevcut site ID'yi al
    const currentSiteId = selectedSite?.id || signalRService.currentSite;
    
    try {
      // Önce bağlantıyı kes
      await signalRService.disconnect();
      
      // Kısa bir bekleme
      await new Promise((resolve:any) => setTimeout(resolve, 1000));
      
      // Yeniden bağlan (site ID ile birlikte)
      await signalRService.connect(currentSiteId || undefined);
    } catch (err: any) {
      setError(err?.message || 'Reconnection failed');
    }
  };

  return (
    <View
      style={{
        padding: 16,
        margin: 16,
        backgroundColor: COLORS.surface,
        borderRadius: 12,
        borderWidth: 1,
        borderColor: COLORS.border,
      }}
    >
      <View
        style={{
          flexDirection: 'row',
          alignItems: 'center',
          justifyContent: 'space-between',
          marginBottom: 12,
        }}
      >
        <View style={{ flexDirection: 'row', alignItems: 'center', gap: 8 }}>
          <MaterialIcons name="wifi" size={24} color={COLORS.text} />
          <Text style={{ fontSize: 16, fontWeight: '600', color: COLORS.text }}>
            SignalR Status
          </Text>
        </View>
        <View
          style={{
            flexDirection: 'row',
            alignItems: 'center',
            gap: 8,
            paddingHorizontal: 12,
            paddingVertical: 6,
            borderRadius: 20,
            backgroundColor: COLORS.dark,
          }}
        >
          <MaterialIcons
            name={getStatusIcon()}
            size={20}
            color={getStatusColor()}
          />
          <Text
            style={{
              fontSize: 12,
              fontWeight: '600',
              color: getStatusColor(),
            }}
          >
            {getStatusText()}
          </Text>
        </View>
      </View>

      <View style={{ gap: 8 }}>
        <View style={{ flexDirection: 'row', justifyContent: 'space-between' }}>
          <Text style={{ fontSize: 12, color: COLORS.textSoft }}>Connection State:</Text>
          <Text
            style={{
              fontSize: 12,
              fontWeight: '600',
              color: getStatusColor(),
            }}
          >
            {connectionState}
          </Text>
        </View>

        <View style={{ flexDirection: 'row', justifyContent: 'space-between' }}>
          <Text style={{ fontSize: 12, color: COLORS.textSoft }}>Is Connected:</Text>
          <Text
            style={{
              fontSize: 12,
              fontWeight: '600',
              color: isConnected ? COLORS.success : COLORS.error,
            }}
          >
            {isConnected ? 'Yes' : 'No'}
          </Text>
        </View>

        {signalRService.currentSite && (
          <View style={{ flexDirection: 'row', justifyContent: 'space-between' }}>
            <Text style={{ fontSize: 12, color: COLORS.textSoft }}>Current Site:</Text>
            <Text style={{ fontSize: 12, fontWeight: '600', color: COLORS.text }}>
              {signalRService.currentSite}
            </Text>
          </View>
        )}

        {error && (
          <View
            style={{
              padding: 8,
              backgroundColor: COLORS.error + '20',
              borderRadius: 8,
              marginTop: 8,
            }}
          >
            <Text style={{ fontSize: 12, color: COLORS.error }}>Error: {error}</Text>
          </View>
        )}

        {connectionState !== 'Connected' && (
          <TouchableOpacity
            onPress={handleReconnect}
            style={{
              marginTop: 8,
              padding: 12,
              backgroundColor: COLORS.accent,
              borderRadius: 8,
              alignItems: 'center',
            }}
          >
            <Text style={{ color: '#fff', fontWeight: '600' }}>Reconnect</Text>
          </TouchableOpacity>
        )}
      </View>
    </View>
  );
}

