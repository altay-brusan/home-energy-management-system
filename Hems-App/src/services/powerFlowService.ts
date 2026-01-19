import { HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel, IStreamResult, HttpTransportType } from '@microsoft/signalr';
import { apiClient } from '../api/client/apiClient';
import { PowerFlowDto, SignalRConnectionState } from '../api/types/signalR.types';
import 'react-native-url-polyfill/auto';

// React Native URL polyfill - Same as signalRService
if (typeof URL !== 'undefined' && URL.prototype) {
    const urlPropertiesToFix = ['pathname', 'search', 'hash', 'host', 'hostname', 'port', 'protocol', 'origin'];

    urlPropertiesToFix.forEach(prop => {
        const originalGetter = Object.getOwnPropertyDescriptor(URL.prototype, prop);
        const alreadySet = (URL.prototype as any)[`__${prop}Set`];

        if (originalGetter && !originalGetter.set && !alreadySet) {
            Object.defineProperty(URL.prototype, prop, {
                get: originalGetter.get,
                set: function() {
                    try {
                        Object.defineProperty(this, `_${prop}`, {
                            value: arguments[0],
                            writable: true,
                            enumerable: false,
                            configurable: true
                        });
                    } catch (e) {
                        // Silent error handling
                    }
                },
                configurable: true
            });
            (URL.prototype as any)[`__${prop}Set`] = true;
        }
    });
}

interface ActiveStream {
    deviceId: string;
    subscription: any; // IStreamResult subscription
    intervalSeconds: number;
}

class PowerFlowService {
    private connection: HubConnection | null = null;
    private activeStreams: Map<string, ActiveStream> = new Map();
    private connectionListeners: Set<(state: SignalRConnectionState) => void> = new Set();
    private isConnecting = false;

    private readonly HUB_URL = "https://hems-api-dev.oklidis.com.tr/powerFlowHub";

    /**
     * Lazy connection - Only connects when first stream is requested
     */
    private async ensureConnected(): Promise<void> {
        if (this.connection?.state === HubConnectionState.Connected) {
            return;
        }

        if (this.isConnecting) {
            // Wait for ongoing connection
            return new Promise((resolve, reject) => {
                const checkInterval = setInterval(() => {
                    if (!this.isConnecting) {
                        clearInterval(checkInterval);
                        if (this.connection?.state === HubConnectionState.Connected) {
                            resolve();
                        } else {
                            reject(new Error('Connection failed'));
                        }
                    }
                }, 100);
            });
        }

        try {
            this.isConnecting = true;
            this.notifyConnectionState('Connecting');

            // Clean up any existing stale connection before creating a new one
            if (this.connection) {
                try {
                    await this.connection.stop();
                } catch (e) {
                    // Ignore cleanup errors
                }
                this.connection = null;
            }

            this.connection = new HubConnectionBuilder()
                .withUrl(this.HUB_URL, {
                    accessTokenFactory: async () => {
                        const freshToken = await this.getAccessToken();
                        return freshToken || '';
                    },
                    transport: HttpTransportType.WebSockets,
                    skipNegotiation: true,
                })
                .withServerTimeout(300000) // 5 minutes
                .withKeepAliveInterval(15000) // 15 seconds
                .configureLogging(LogLevel.Information) // Enable logging to see errors
                .withAutomaticReconnect({
                    nextRetryDelayInMilliseconds: (retryContext) => {
                        console.log(`🔄 [PowerFlowService] Reconnecting... Attempt ${retryContext.previousRetryCount + 1}`);
                        const delay = Math.min(
                            1000 * Math.pow(2, retryContext.previousRetryCount),
                            30000
                        );
                        return delay;
                    }
                })
                .build();

            this.setupConnectionEvents();

            await this.connection.start();

            this.notifyConnectionState('Connected');
            this.isConnecting = false;

        } catch (error: any) {
            this.isConnecting = false;
            // Clean up failed connection to ensure fresh state on retry
            this.connection = null;
            this.notifyConnectionState('Failed');
            console.error('❌ [PowerFlowService] Connection failed:', {
                error: error?.message || error,
                stack: error?.stack,
            });
            throw error;
        }
    }

    /**
     * Start streaming power flow data for a specific device
     * @param deviceId Device GUID
     * @param intervalSeconds Update interval (2-60 seconds, default: 5)
     * @param onData Callback for each data update
     * @param onError Callback for errors
     * @param onComplete Callback when stream completes
     * @returns Cleanup function to stop the stream
     */
    async startStream(
        deviceId: string,
        intervalSeconds: number = 5,
        onData: (data: PowerFlowDto) => void,
        onError?: (error: Error) => void,
        onComplete?: () => void
    ): Promise<() => void> {
        // Stop existing stream for this device
        if (this.activeStreams.has(deviceId)) {
            this.stopStream(deviceId);
        }

        // Ensure connection is established
        await this.ensureConnected();

        if (!this.connection || this.connection.state !== HubConnectionState.Connected) {
            throw new Error('PowerFlow hub connection is not established');
        }

        try {
            // Start the stream
            const stream: IStreamResult<PowerFlowDto> = this.connection.stream(
                'StreamPowerFlow',
                deviceId,
                intervalSeconds
            );

            const subscription = stream.subscribe({
                next: (data: PowerFlowDto) => {
                    console.log('✅ [PowerFlowService] Data received:', {
                        deviceId: data.deviceId,
                        solar: data.solarPowerKw,
                        battery: data.batteryPowerKw,
                        grid: data.gridPowerKw,
                        load: data.loadPowerKw,
                    });
                    try {
                        onData(data);
                    } catch (error) {
                        console.error('❌ [PowerFlowService] Error in onData callback:', error);
                    }
                },
                error: (error: any) => {
                    console.error('❌ [PowerFlowService] Stream error:', {
                        deviceId,
                        error: error?.message || error,
                    });
                    this.activeStreams.delete(deviceId);
                    if (onError) {
                        onError(error instanceof Error ? error : new Error(String(error)));
                    }
                },
                complete: () => {
                    this.activeStreams.delete(deviceId);
                    if (onComplete) {
                        onComplete();
                    }
                }
            });

            // Store active stream
            this.activeStreams.set(deviceId, {
                deviceId,
                subscription,
                intervalSeconds
            });

            // Return cleanup function
            console.log('✅ [PowerFlowService] Stream started successfully for device:', deviceId);
            return () => this.stopStream(deviceId);

        } catch (error: any) {
            console.error('❌ [PowerFlowService] Failed to start stream:', {
                deviceId,
                error: error?.message || error,
            });
            throw error;
        }
    }

    /**
     * Stop streaming for a specific device
     */
    stopStream(deviceId: string): void {
        const activeStream = this.activeStreams.get(deviceId);
        if (activeStream) {
            try {
                activeStream.subscription.dispose();
            } catch (error) {
                // Silent error handling
            }
            this.activeStreams.delete(deviceId);
        }

        // If no active streams, disconnect
        if (this.activeStreams.size === 0) {
            this.disconnect();
        }
    }

    /**
     * Stop all active streams
     */
    stopAllStreams(): void {
        this.activeStreams.forEach((stream) => {
            try {
                stream.subscription.dispose();
            } catch (error) {
                // Silent error handling
            }
        });
        this.activeStreams.clear();
        this.disconnect();
    }

    /**
     * Disconnect from hub (only if no active streams)
     */
    private async disconnect(): Promise<void> {
        if (this.activeStreams.size > 0) {
            return; // Don't disconnect if streams are active
        }

        if (this.connection) {
            try {
                await this.connection.stop();
            } catch (error) {
                // Silent error handling
            }
            this.connection = null;
        }

        this.notifyConnectionState('Disconnected');
    }

    private setupConnectionEvents(): void {
        if (!this.connection) return;

        this.connection.onclose(async (error) => {
            console.warn('⚠️ [PowerFlowService] Connection closed:', error?.message || 'No error');
            this.notifyConnectionState('Disconnected');
            // Clear all active streams on connection close
            this.activeStreams.clear();
        });

        this.connection.onreconnecting((error) => {
            console.log('🔄 [PowerFlowService] Reconnecting...', error?.message || '');
            this.notifyConnectionState('Reconnecting');
        });

        this.connection.onreconnected(async (connectionId) => {
            console.log('✅ [PowerFlowService] Reconnected with ID:', connectionId);
            this.notifyConnectionState('Connected');

            // Restart all active streams after reconnection
            const streamsToRestart = Array.from(this.activeStreams.values());
            this.activeStreams.clear();

            console.log(`🔄 [PowerFlowService] Need to restart ${streamsToRestart.length} streams`);
            for (const stream of streamsToRestart) {
                // Note: The original callbacks are lost, streams need to be restarted by hooks
            }
        });
    }

    private async getAccessToken(): Promise<string> {
        const token = await apiClient.getAccessToken();
        if (!token) {
            throw new Error('No access token found');
        }
        return token;
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

    get activeStreamCount(): number {
        return this.activeStreams.size;
    }

    isStreamActive(deviceId: string): boolean {
        return this.activeStreams.has(deviceId);
    }
}

export const powerFlowService = new PowerFlowService();
