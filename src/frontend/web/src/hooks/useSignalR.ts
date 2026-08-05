import { useEffect, useRef, useState, useCallback } from 'react';
import * as signalR from '@microsoft/signalr';

const HUB_URL = (import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000') + '/hubs/notifications';

type SignalRStatus = 'disconnected' | 'connecting' | 'connected';

export function useSignalR(onNotification?: (data: unknown) => void) {
    // Starts as 'connecting' because the mount effect opens a connection right away.
    // Setting it from inside the effect instead would be a synchronous setState in an
    // effect body, which triggers an extra render pass on every mount.
    const [status, setStatus] = useState<SignalRStatus>('connecting');
    const connectionRef = useRef<signalR.HubConnection | null>(null);

    // Keeps the latest callback without making it an effect dependency -- otherwise a
    // caller passing an inline function would tear down and reopen the socket on
    // every render.
    const onNotificationRef = useRef(onNotification);
    useEffect(() => {
        onNotificationRef.current = onNotification;
    }, [onNotification]);

    // Builds and starts a connection. Deliberately free of synchronous setState: every
    // status update here happens in an async callback.
    const openConnection = useCallback(() => {
        const connection = new signalR.HubConnectionBuilder()
            .withUrl(HUB_URL, {
                withCredentials: true,
            })
            .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
            .configureLogging(signalR.LogLevel.Warning)
            .build();

        connection.onreconnecting(() => setStatus('connecting'));
        connection.onreconnected(() => setStatus('connected'));
        connection.onclose(() => setStatus('disconnected'));

        // Listen for notification events
        connection.on('ReceiveNotification', (data: unknown) => {
            onNotificationRef.current?.(data);
        });

        connection.on('NotificationRead', (data: unknown) => {
            onNotificationRef.current?.(data);
        });

        connection
            .start()
            .then(() => setStatus('connected'))
            .catch(() => setStatus('disconnected'));

        connectionRef.current = connection;
        return connection;
    }, []);

    useEffect(() => {
        const connection = openConnection();
        return () => {
            void connection.stop();
            if (connectionRef.current === connection) {
                connectionRef.current = null;
            }
        };
    }, [openConnection]);

    // Called from event handlers, where a synchronous setState is fine.
    const reconnect = useCallback(() => {
        void connectionRef.current?.stop();
        setStatus('connecting');
        openConnection();
    }, [openConnection]);

    return { status, reconnect };
}
