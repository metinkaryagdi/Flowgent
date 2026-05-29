import { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { notificationsApi } from '../../api/notifications';
import { issuesApi } from '../../api/issues';
import { authApi } from '../../api/auth';
import { organizationsApi } from '../../api/organizations';
import { useToastStore } from '../../store/toastStore';
import { useAuthStore } from '../../store/authStore';
import { useSignalR } from '../../hooks/useSignalR';
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

const PAGE_SIZE = 20;

const isOrganizationInvite = (notif: NotificationDto) =>
    notif.entityType === 'Organization' &&
    Boolean(notif.entityId) &&
    notif.title.toLocaleLowerCase('tr-TR').includes('davet');

export default function NotificationsPage() {
    const [notifications, setNotifications] = useState<NotificationDto[]>([]);
    const [loading, setLoading] = useState(true);
    const [page, setPage] = useState(1);
    const [selectedInvite, setSelectedInvite] = useState<NotificationDto | null>(null);
    const [inviteActionLoading, setInviteActionLoading] = useState<'accept' | 'decline' | null>(null);
    const { addToast: showToast } = useToastStore();
    const { setActiveOrg, setFlags } = useAuthStore();
    const navigate = useNavigate();

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
        const previous = notifications;
        setNotifications((prev) =>
            prev.map((n) => (n.id === id ? { ...n, isRead: true, readAt: new Date().toISOString() } : n))
        );
        try {
            await notificationsApi.markAsRead(id);
        } catch (err: unknown) {
            setNotifications(previous);
            const status = (err as { response?: { status?: number } })?.response?.status;
            showToast(`Bildirim okundu olarak işaretlenemedi${status ? ` (HTTP ${status})` : ''}.`, 'error');
        }
    };

    const handleNotificationClick = useCallback(async (notif: NotificationDto) => {
        if (!notif.isRead) {
            handleMarkAsRead(notif.id);
        }
        if (isOrganizationInvite(notif)) {
            setSelectedInvite(notif);
            return;
        }
        if (notif.entityType === 'Issue' && notif.entityId) {
            try {
                const issue = await issuesApi.getById(notif.entityId);
                navigate(`/projects/${issue.projectId}/board`, {
                    state: { openIssueId: notif.entityId },
                });
            } catch {
                showToast('Issue bulunamadı.', 'error');
            }
        }
    }, [navigate, showToast]);

    const removeNotificationFromList = (id: string) => {
        setNotifications((prev) => prev.filter((item) => item.id !== id));
    };

    const handleAcceptInvite = async () => {
        if (!selectedInvite?.entityId) return;

        setInviteActionLoading('accept');
        try {
            await organizationsApi.acceptOrganizationInvite(selectedInvite.entityId);
            const switchResult = await organizationsApi.switchOrg(selectedInvite.entityId);
            setActiveOrg({
                id: switchResult.orgId,
                name: switchResult.orgName,
                role: switchResult.orgRole,
            });
            try {
                const flags = await authApi.getFlags();
                setFlags(flags);
            } catch { /* flags refresh is non-fatal */ }
            await notificationsApi.markAsRead(selectedInvite.id);
            removeNotificationFromList(selectedInvite.id);
            setSelectedInvite(null);
            showToast('Organizasyona katildiniz.');
            navigate('/projects');
        } catch (err: unknown) {
            if (err && typeof err === 'object' && 'response' in err) {
                const e = err as { response?: { data?: { message?: string } } };
                showToast(e.response?.data?.message || 'Davet kabul edilemedi.', 'error');
            } else {
                showToast('Sunucuya baglanilamadi.', 'error');
            }
        } finally {
            setInviteActionLoading(null);
        }
    };

    const handleDeclineInvite = async () => {
        if (!selectedInvite?.entityId) return;

        setInviteActionLoading('decline');
        try {
            await organizationsApi.declineOrganizationInvite(selectedInvite.entityId);
            await notificationsApi.markAsRead(selectedInvite.id);
            removeNotificationFromList(selectedInvite.id);
            setSelectedInvite(null);
            showToast('Davet reddedildi.');
        } catch (err: unknown) {
            if (err && typeof err === 'object' && 'response' in err) {
                const e = err as { response?: { data?: { message?: string } } };
                showToast(e.response?.data?.message || 'Davet reddedilemedi.', 'error');
            } else {
                showToast('Sunucuya baglanilamadi.', 'error');
            }
        } finally {
            setInviteActionLoading(null);
        }
    };

    const handleMarkAllAsRead = async () => {
        try {
            await notificationsApi.markAllAsRead();
            setNotifications((prev) =>
                prev.map((n) => ({ ...n, isRead: true, readAt: new Date().toISOString() }))
            );
            showToast('Tümü okundu olarak işaretlendi.');
        } catch { /* ignore */ }
    };

    const unreadCount = notifications.filter((n) => !n.isRead).length;
    const totalPages = Math.max(1, Math.ceil(notifications.length / PAGE_SIZE));
    const pagedNotifications = notifications.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE);

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
                <>
                    <div className={styles.list} data-testid="notifications-list">
                        {pagedNotifications.map((notif) => {
                            const isUnread = !notif.isRead;
                            const canOpen = (notif.entityType === 'Issue' && notif.entityId) || isOrganizationInvite(notif);
                            return (
                                <div
                                    key={notif.id}
                                    className={`${styles.item} ${isUnread ? styles.itemUnread : ''} ${canOpen ? styles.itemClickable : ''}`}
                                    data-testid="notification-item"
                                    onClick={() => handleNotificationClick(notif)}
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

                    {totalPages > 1 && (
                        <div className={styles.pagination}>
                            <button
                                className={styles.pageBtn}
                                onClick={() => setPage((p) => Math.max(1, p - 1))}
                                disabled={page === 1}
                                aria-label="Önceki sayfa"
                            >
                                ← Önceki
                            </button>
                            <span className={styles.pageInfo}>
                                {page} / {totalPages} <span style={{ color: 'var(--text-tertiary)', fontSize: 'var(--font-size-xs)' }}>({notifications.length} bildirim)</span>
                            </span>
                            <button
                                className={styles.pageBtn}
                                onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                                disabled={page === totalPages}
                                aria-label="Sonraki sayfa"
                            >
                                Sonraki →
                            </button>
                        </div>
                    )}
                </>
            )}

            {selectedInvite && (
                <div className={styles.modalOverlay} onClick={() => setSelectedInvite(null)}>
                    <div className={styles.modal} onClick={(event) => event.stopPropagation()}>
                        <h2 className={styles.modalTitle}>{selectedInvite.title}</h2>
                        <p className={styles.modalText}>{selectedInvite.message}</p>
                        <div className={styles.modalFooter}>
                            <button
                                className={styles.btnSecondary}
                                disabled={inviteActionLoading !== null}
                                onClick={handleDeclineInvite}
                            >
                                {inviteActionLoading === 'decline' ? 'Isleniyor...' : 'Katilma'}
                            </button>
                            <button
                                className={styles.btnPrimary}
                                disabled={inviteActionLoading !== null}
                                onClick={handleAcceptInvite}
                            >
                                {inviteActionLoading === 'accept' ? 'Katiliniyor...' : 'Katil'}
                            </button>
                        </div>
                    </div>
                </div>
            )}

        </div>
    );
}
