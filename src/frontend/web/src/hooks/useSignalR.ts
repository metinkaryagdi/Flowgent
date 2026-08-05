import { useEffect, useRef, useState, useCallback } from 'react';
import * as signalR from '@microsoft/signalr';

const HUB_URL = (import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000') + '/hubs/notifications';

type SignalRStatus = 'disconnected' | 'connecting' | 'connected';

/** Payload of the NotificationRead hub method. Mirrors NotificationReadEventHandler. */
export interface NotificationReadPayload {
    notificationId: string;
    readAt: string;
}

/**
 * Hub method names. These must match the constants on NotificationsHub exactly: sending to
 * a group with no matching client handler is not an error on the server, so a typo here
 * makes notifications vanish while the service still reports them as delivered. That is
 * not hypothetical -- it happened, and went unnoticed for months.
 */
const RECEIVE_NOTIFICATION = 'ReceiveNotification';
const NOTIFICATION_READ = 'NotificationRead';

interface SignalRHandlers {
    /** A new notification arrived for this user. */
    onNotification?: (data: unknown) => void;
    /** One of this user's notifications was marked read, possibly in another tab. */
    onNotificationRead?: (data: NotificationReadPayload) => void;
}

/**
 * Both events used to be wired to the same callback, which meant a caller could not tell
 * "you have a new notification" from "one of your notifications was read somewhere else"
 * -- two events with different payloads and opposite effects on an unread badge.
 */
export function useSignalR(handlers: SignalRHandlers = {}) {
    // Starts as 'connecting' because the mount effect opens a connection right away.
    // Setting it from inside the effect instead would be a synchronous setState in an
    // effect body, which triggers an extra render pass on every mount.
    const [status, setStatus] = useState<SignalRStatus>('connecting');
    const connectionRef = useRef<signalR.HubConnection | null>(null);

    // Keeps the latest callbacks without making them effect dependencies -- otherwise a
    // caller passing inline functions would tear down and reopen the socket on
    // every render.
    const handlersRef = useRef(handlers);
    useEffect(() => {
        handlersRef.current = handlers;
    }, [handlers]);

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

        connection.on(RECEIVE_NOTIFICATION, (data: unknown) => {
            handlersRef.current.onNotification?.(data);
        });

        connection.on(NOTIFICATION_READ, (data: NotificationReadPayload) => {
            handlersRef.current.onNotificationRead?.(data);
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
