import { useEffect, useRef, useState, useCallback } from 'react';
import * as signalR from '@microsoft/signalr';

const HUB_URL = (import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000') + '/hubs/notifications';

type SignalRStatus = 'disconnected' | 'connecting' | 'connected';

export function useSignalR(onNotification?: (data: unknown) => void) {
    const [status, setStatus] = useState<SignalRStatus>('disconnected');
    const connectionRef = useRef<signalR.HubConnection | null>(null);

    const connect = useCallback(() => {
        const token = localStorage.getItem('accessToken');
        if (!token) return;

        const connection = new signalR.HubConnectionBuilder()
            .withUrl(HUB_URL, {
                accessTokenFactory: () => token,
            })
            .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
            .configureLogging(signalR.LogLevel.Warning)
            .build();

        connection.onreconnecting(() => setStatus('connecting'));
        connection.onreconnected(() => setStatus('connected'));
        connection.onclose(() => setStatus('disconnected'));

        // Listen for notification events
        connection.on('ReceiveNotification', (data: unknown) => {
            onNotification?.(data);
        });

        connection.on('NotificationRead', (data: unknown) => {
            onNotification?.(data);
        });

        setStatus('connecting');
        connection
            .start()
            .then(() => setStatus('connected'))
            .catch(() => setStatus('disconnected'));

        connectionRef.current = connection;
    }, [onNotification]);

    const disconnect = useCallback(() => {
        connectionRef.current?.stop();
        connectionRef.current = null;
        setStatus('disconnected');
    }, []);

    useEffect(() => {
        connect();
        return () => {
            disconnect();
        };
    }, [connect, disconnect]);

    return { status, reconnect: connect };
}
