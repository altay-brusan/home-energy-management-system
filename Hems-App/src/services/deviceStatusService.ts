import { HttpTransportType, HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';
import { apiClient } from '../api/client/apiClient';
import { SignalRConnectionState, DeviceStatusChangedDto } from '../api/types/signalR.types';
import 'react-native-url-polyfill/auto';

class DeviceStatusService {
    private connection: HubConnection | null = null;
    private isManualDisconnect = false;
    private currentDeviceId: string | null = null;
    private connectionListeners: Set<(state: SignalRConnectionState) => void> = new Set();
    private statusListeners: Set<(data: DeviceStatusChangedDto) => void> = new Set();

    private readonly HUB_URL = "https://hems-api-dev.oklidis.com.tr/deviceStatusHub";

    async connect(): Promise<void> {
        // Zaten bağlıysa tekrar bağlanma
        if (this.connection?.state === HubConnectionState.Connected) {
            console.log('[DeviceStatusService] Already connected, skipping. Connection ID:', this.connection.connectionId);
            return;
        }

        console.log('[DeviceStatusService] Current connection state:', this.connection?.state ?? 'null');

        try {
            this.isManualDisconnect = false;

            console.log('[DeviceStatusService] Connecting to:', this.HUB_URL);

            this.connection = new HubConnectionBuilder()
                .withUrl(this.HUB_URL, {
                    accessTokenFactory: async () => {
                        const freshToken = await this.getAccessToken();
                        return freshToken || '';
                    },
                    transport: HttpTransportType.WebSockets,
                    skipNegotiation: true,
                })
                .withServerTimeout(120000)
                .withKeepAliveInterval(15000)
                .configureLogging(LogLevel.Information)
                .withAutomaticReconnect({
                    nextRetryDelayInMilliseconds: (retryContext) => {
                        const delay = Math.min(
                            2000 * Math.pow(2, retryContext.previousRetryCount),
                            30000
                        );
                        return delay;
                    }
                })
                .build();

            this.setupConnectionEvents();
            this.setupMessageHandlers();

            await this.connection.start();
            console.log('[DeviceStatusService] ✅ Connected successfully to:', this.HUB_URL);
            console.log('[DeviceStatusService] Connection ID:', this.connection.connectionId);

            this.notifyConnectionState('Connected');

        } catch (error) {
            console.log('[DeviceStatusService] Connection failed:', error);
            this.notifyConnectionState('Failed');
            throw error;
        }
    }

    async subscribeToDevice(deviceId: string): Promise<void> {
        if (!this.connection || this.connection.state !== HubConnectionState.Connected) {
            console.log('[DeviceStatusService] Cannot subscribe - not connected');
            return;
        }

        // Aynı device'a zaten subscribe olmuşsak, tekrar yapma
        if (this.currentDeviceId === deviceId) {
            console.log('[DeviceStatusService] Already subscribed to device:', deviceId);
            return;
        }

        // Farklı bir device'a subscribe olacaksak, önce mevcut aboneliği iptal et
        if (this.currentDeviceId && this.currentDeviceId !== deviceId) {
            await this.unsubscribeFromDevice();
        }

        try {
            console.log('[DeviceStatusService] Subscribing to device:', deviceId);
            console.log('[DeviceStatusService] Connection ID:', this.connection.connectionId);
            console.log('[DeviceStatusService] Connection state:', this.connection.state);
            console.log('[DeviceStatusService] Hub URL:', this.HUB_URL);
            const result = await this.connection.invoke('SubscribeToDevice', deviceId);
            console.log('[DeviceStatusService] SubscribeToDevice invoke result:', result);
            this.currentDeviceId = deviceId;
            console.log('[DeviceStatusService] ✅ Subscribed to device:', deviceId);
            console.log('[DeviceStatusService] Now waiting for DeviceStatusChanged events...');
        } catch (error) {
            console.log('[DeviceStatusService] ❌ Subscribe failed:', error);
            throw error;
        }
    }

    async unsubscribeFromDevice(): Promise<void> {
        if (!this.connection || this.connection.state !== HubConnectionState.Connected) {
            return;
        }

        if (!this.currentDeviceId) {
            return;
        }

        try {
            console.log('[DeviceStatusService] Unsubscribing from device:', this.currentDeviceId);
            await this.connection.invoke('UnsubscribeFromDevice', this.currentDeviceId);
            this.currentDeviceId = null;
            console.log('[DeviceStatusService] Unsubscribed successfully');
        } catch (error) {
            console.log('[DeviceStatusService] Unsubscribe failed:', error);
        }
    }

    async disconnect(): Promise<void> {
        this.isManualDisconnect = true;

        // Önce device subscription'ı kaldır
        await this.unsubscribeFromDevice();

        if (this.connection) {
            try {
                await this.connection.stop();
            } catch (error) {
                // Silent error handling
            }
            this.connection = null;
        }

        this.currentDeviceId = null;
        this.notifyConnectionState('Disconnected');
    }

    private setupConnectionEvents(): void {
        if (!this.connection) return;

        this.connection.onclose(async (_error) => {
            this.notifyConnectionState('Disconnected');
        });

        this.connection.onreconnecting((_error) => {
            this.notifyConnectionState('Reconnecting');
        });

        this.connection.onreconnected(async (_connectionId) => {
            console.log('[DeviceStatusService] Reconnected');
            this.notifyConnectionState('Connected');

            // Reconnect sonrası mevcut device'a tekrar subscribe ol
            if (this.currentDeviceId) {
                const deviceId = this.currentDeviceId;
                this.currentDeviceId = null; // Subscribe'ın çalışması için null yap
                try {
                    await this.subscribeToDevice(deviceId);
                } catch (error) {
                    console.log('[DeviceStatusService] Re-subscribe failed after reconnect:', error);
                }
            }
        });
    }

    private setupMessageHandlers(): void {
        if (!this.connection) return;

        console.log('[DeviceStatusService] Setting up message handlers for hub:', this.HUB_URL);

        // DeviceStatusChanged event listener
        this.connection.on('DeviceStatusChanged', (data: DeviceStatusChangedDto) => {
            console.log('[DeviceStatusService] ✅ Received DeviceStatusChanged:', JSON.stringify(data, null, 2));
            this.notifyStatusChange(data);
        });

        console.log('[DeviceStatusService] ✅ Message handler registered for DeviceStatusChanged');
    }

    addStatusListener(listener: (data: DeviceStatusChangedDto) => void): () => void {
        this.statusListeners.add(listener);
        return () => {
            this.statusListeners.delete(listener);
        };
    }

    private notifyStatusChange(data: DeviceStatusChangedDto): void {
        this.statusListeners.forEach(listener => {
            try {
                listener(data);
            } catch (error) {
                // Silent error handling
            }
        });
    }

    addConnectionStateListener(listener: (state: SignalRConnectionState) => void): () => void {
        this.connectionListeners.add(listener);
        return () => {
            this.connectionListeners.delete(listener);
        };
    }

    private notifyConnectionState(state: SignalRConnectionState): void {
        this.connectionListeners.forEach(listener => {
            try {
                listener(state);
            } catch (error) {
                // Silent error handling
            }
        });
    }

    async getAccessToken(): Promise<string> {
        const token = await apiClient.getAccessToken();
        if (!token) {
            throw new Error('No access token found');
        }
        return token;
    }

    get isConnected(): boolean {
        return this.connection?.state === HubConnectionState.Connected;
    }

    get connectionState(): SignalRConnectionState {
        if (!this.connection) return 'Disconnected';

        switch (this.connection.state) {
            case HubConnectionState.Connected:
                return 'Connected';
            case HubConnectionState.Connecting:
                return 'Connecting';
            case HubConnectionState.Reconnecting:
                return 'Reconnecting';
            case HubConnectionState.Disconnected:
                return 'Disconnected';
            default:
                return 'Failed';
        }
    }

    get currentDevice(): string | null {
        return this.currentDeviceId;
    }
}

export const deviceStatusService = new DeviceStatusService();
