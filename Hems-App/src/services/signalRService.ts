import { HttpTransportType, HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';
import { apiClient } from '../api/client/apiClient';
import { SignalRConnectionState } from '../api/types/signalR.types';
import 'react-native-url-polyfill/auto';

// React Native URL polyfill - Fixes SignalR negotiation issue with immutable URL properties
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
                    }
                },
                configurable: true
            });
            (URL.prototype as any)[`__${prop}Set`] = true;
        }
    });
}

class SignalRService {
    private connection: HubConnection | null = null;
    private isManualDisconnect = false;
    private currentSiteId: string | null = null;
    private connectionListeners: Set<(state: SignalRConnectionState) => void> = new Set();
    
    private readonly HUB_URL = "https://hems-api.pentaunity.com/deviceNotificationHub";
    
    async connect(siteId?: string): Promise<void> {
        if (this.connection?.state === HubConnectionState.Connected) {
            return;
        }

        try {
            this.isManualDisconnect = false;

            const token = await this.getAccessToken();
            
            this.connection = new HubConnectionBuilder()
                .withUrl(this.HUB_URL, {
                    accessTokenFactory: async () => {
                        const freshToken = await this.getAccessToken();
                        return freshToken || '';
                    },
                    transport: HttpTransportType.LongPolling,
                })
                .withServerTimeout(120000)
                .withKeepAliveInterval(15000)
                .configureLogging(LogLevel.None)
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
            
            await this.connection.start();

            if (siteId) {
                await this.joinSiteGroup(siteId);
            }

            this.notifyConnectionState('Connected');

        } catch (error) {
            this.notifyConnectionState('Failed');
            throw error;
        }
    }

    async disconnect(): Promise<void> {
        this.isManualDisconnect = true;

        if (this.connection) {
            try {
                await this.connection.stop();
            } catch (error) {
                // Silent error handling
            }
            this.connection = null;
        }

        this.currentSiteId = null;
        this.notifyConnectionState('Disconnected');
    }

    async joinSiteGroup(siteId: string): Promise<void> {
        if (!this.connection || this.connection.state !== HubConnectionState.Connected) {
            return;
        }

        try {
            await this.connection.invoke('JoinSiteGroup', siteId);
            this.currentSiteId = siteId;
        } catch (error) {
            throw error;
        }
    }

    async leaveSiteGroup(): Promise<void> {
        if (!this.connection || this.connection.state !== HubConnectionState.Connected) {
            return;
        }

        try {
            await this.connection.invoke('LeaveSiteGroup');
            this.currentSiteId = null;
        } catch (error) {
            throw error;
        }
    }

    private setupConnectionEvents(): void {
        if (!this.connection) return;

        this.connection.onclose(async (error) => {
            this.notifyConnectionState('Disconnected');
        });

        this.connection.onreconnecting((error) => {
            this.notifyConnectionState('Reconnecting');
        });

        this.connection.onreconnected((connectionId) => {
            this.notifyConnectionState('Connected');

            if (this.currentSiteId) {
                this.joinSiteGroup(this.currentSiteId).catch(() => {});
            }
        });
    }

    on(methodName: string, callback: (...args: any[]) => void): void {
        if (!this.connection) return;

        this.connection.on(methodName, callback);
    }

    off(methodName: string, callback?: (...args: any[]) => void): void {
        if (!this.connection) return;

        if (callback) {
            this.connection.off(methodName, callback);
        } else {
            this.connection.off(methodName);
        }
    }

    async invoke<T = any>(methodName: string, ...args: any[]): Promise<T> {
        if (!this.connection || this.connection.state !== HubConnectionState.Connected) {
            throw new Error('SignalR connection is not established.');
        }
        return this.connection.invoke(methodName, ...args);
    }

    async getAccessToken(): Promise<string> {
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

    get currentSite(): string | null {
        return this.currentSiteId;
    }
}

export const signalRService = new SignalRService();
