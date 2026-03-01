import { useState, useEffect, useCallback } from 'react';
import { notificationsApi } from '../../api/notifications';
import { useSignalR } from '../../hooks/useSignalR';
import { NotificationStatus } from '../../types';
import type { NotificationDto } from '../../types';
import styles from './Notifications.module.css';

const formatDate = (d: string) => {
    const date = new Date(d);
    const now = new Date();
    const diff = now.getTime() - date.getTime();
    const minutes = Math.floor(diff / 60000);
    const hours = Math.floor(diff / 3600000);
    const days = Math.floor(diff / 86400000);

    if (minutes < 1) return 'Az önce';
    if (minutes < 60) return `${minutes} dk önce`;
    if (hours < 24) return `${hours} saat önce`;
    if (days < 7) return `${days} gün önce`;
    return date.toLocaleDateString('tr-TR', { day: 'numeric', month: 'short' });
};

export default function NotificationsPage() {
    const [notifications, setNotifications] = useState<NotificationDto[]>([]);
    const [loading, setLoading] = useState(true);
    const [toast, setToast] = useState<string | null>(null);

    const showToast = (msg: string) => {
        setToast(msg);
        setTimeout(() => setToast(null), 3000);
    };

    const loadNotifications = useCallback(async () => {
        try {
            const data = await notificationsApi.getAll();
            setNotifications(data);
        } catch {
            // silently fail
        } finally {
            setLoading(false);
        }
    }, []);

    useEffect(() => {
        loadNotifications();
    }, [loadNotifications]);

    // ── SignalR real-time ─────────
    const handleSignalREvent = useCallback(() => {
        // Refresh notifications when a real-time event comes in
        loadNotifications();
    }, [loadNotifications]);

    const { status: signalRStatus } = useSignalR(handleSignalREvent);

    // ── Mark as read ──────────────
    const handleMarkAsRead = async (id: string) => {
        try {
            await notificationsApi.markAsRead(id);
            setNotifications((prev) =>
                prev.map((n) => (n.id === id ? { ...n, status: NotificationStatus.Read } : n))
            );
        } catch { /* ignore */ }
    };

    const handleMarkAllAsRead = async () => {
        try {
            await notificationsApi.markAllAsRead();
            setNotifications((prev) =>
                prev.map((n) => ({ ...n, status: NotificationStatus.Read }))
            );
            showToast('Tümü okundu olarak işaretlendi.');
        } catch { /* ignore */ }
    };

    const unreadCount = notifications.filter((n) => n.status === NotificationStatus.Unread).length;

    if (loading) {
        return (
            <div className={styles.notifPage}>
                <div className={styles.header}>
                    <h1 className={styles.title}>Bildirimler</h1>
                </div>
                <div className={styles.list} data-testid="notifications-list">
                    {[1, 2, 3, 4].map((i) => <div key={i} className={styles.skeleton} />)}
                </div>
            </div>
        );
    }

    return (
        <div className={styles.notifPage}>
            {/* ── Header ─────────────── */}
            <div className={styles.header}>
                <h1 className={styles.title}>
                    Bildirimler
                    {unreadCount > 0 && <span style={{ color: 'var(--color-primary)', fontSize: 'var(--font-size-base)', marginLeft: 8 }}>({unreadCount})</span>}
                </h1>
                <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
                    <div className={styles.signalrStatus}>
                        <span className={`${styles.signalrDot} ${signalRStatus === 'connected' ? styles.signalrConnected : styles.signalrDisconnected}`} />
                        {signalRStatus === 'connected' ? 'Canlı' : signalRStatus === 'connecting' ? 'Bağlanıyor...' : 'Çevrimdışı'}
                    </div>
                    {unreadCount > 0 && (
                        <button className={styles.markAllBtn} onClick={handleMarkAllAsRead}>
                            Tümünü Okundu İşaretle
                        </button>
                    )}
                </div>
            </div>

            {/* ── List ───────────────── */}
            {notifications.length === 0 ? (
                <div className={styles.empty}>
                    <div className={styles.emptyIcon}>🔔</div>
                    <p className={styles.emptyText}>Henüz bildirim yok.</p>
                </div>
            ) : (
                <div className={styles.list} data-testid="notifications-list">
                    {notifications.map((notif) => {
                        const isUnread = notif.status === NotificationStatus.Unread;
                        return (
                            <div
                                key={notif.id}
                                className={`${styles.item} ${isUnread ? styles.itemUnread : ''}`}
                                data-testid="notification-item"
                                onClick={() => isUnread && handleMarkAsRead(notif.id)}
                            >
                                <span className={`${styles.dot} ${!isUnread ? styles.dotRead : ''}`} />
                                <div className={styles.itemContent}>
                                    <div className={styles.itemTitle}>{notif.title}</div>
                                    <div className={styles.itemMessage}>{notif.message}</div>
                                </div>
                                <span className={styles.itemDate}>{formatDate(notif.createdAt)}</span>
                            </div>
                        );
                    })}
                </div>
            )}

            {/* ── Toast ─────────────── */}
            {toast && <div className={styles.toast}>{toast}</div>}
        </div>
    );
}
