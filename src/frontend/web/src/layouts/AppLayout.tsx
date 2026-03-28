import { useState, useEffect, useCallback, useRef } from 'react';
import { NavLink, Outlet, useNavigate, useLocation } from 'react-router-dom';
import { useAuthStore } from '../store/authStore';
import { useThemeStore } from '../store/themeStore';
import { notificationsApi } from '../api/notifications';
import type { NotificationDto } from '../types';
import styles from './AppLayout.module.css';

export default function AppLayout() {
    const { user, roles, flags, logout } = useAuthStore();
    const { theme, toggleTheme } = useThemeStore();
    const navigate = useNavigate();
    const location = useLocation();
    const [unreadCount, setUnreadCount] = useState(0);
    const [showNotifications, setShowNotifications] = useState(false);
    const [sidebarOpen, setSidebarOpen] = useState(false);
    const [notifications, setNotifications] = useState<NotificationDto[]>([]);
    const [notificationsLoading, setNotificationsLoading] = useState(false);
    const [notificationsError, setNotificationsError] = useState('');
    const notificationsRef = useRef<HTMLDivElement>(null);

    const fetchUnreadCount = useCallback(async () => {
        try {
            const count = await notificationsApi.getUnreadCount();
            setUnreadCount(count);
        } catch { /* ignore */ }
    }, []);

    const fetchNotifications = useCallback(async () => {
        setNotificationsLoading(true);
        setNotificationsError('');
        try {
            const data = await notificationsApi.getAll();
            setNotifications(data);
        } catch {
            setNotificationsError('Bildirimler yÃ¼klenemedi.');
        } finally {
            setNotificationsLoading(false);
        }
    }, []);

    useEffect(() => {
        fetchUnreadCount();
        const interval = setInterval(fetchUnreadCount, 30000);
        return () => clearInterval(interval);
    }, [fetchUnreadCount]);

    useEffect(() => {
        if (!showNotifications) return;
        void fetchNotifications();
    }, [showNotifications, fetchNotifications]);

    useEffect(() => {
        const handleClick = (event: MouseEvent) => {
            if (!notificationsRef.current) return;
            if (!notificationsRef.current.contains(event.target as Node)) {
                setShowNotifications(false);
            }
        };
        if (showNotifications) {
            document.addEventListener('mousedown', handleClick);
        }
        return () => document.removeEventListener('mousedown', handleClick);
    }, [showNotifications]);

    // Sayfa değişince sidebar'ı kapat (mobile)
    useEffect(() => {
        setSidebarOpen(false);
    }, [location.pathname]);

    const getBreadcrumb = () => {
        const path = location.pathname;
        if (path === '/projects') return 'Projeler';
        if (path === '/notifications') return 'Bildirimler';
        if (path === '/admin') return 'YÃ¶netim Paneli';
        if (path.includes('/board')) return 'Projeler â€º Board';
        if (path.includes('/sprints')) return 'Projeler â€º Sprint';
        return 'Kontrol Paneli';
    };

    const handleLogout = () => {
        logout();
        navigate('/login');
    };

    const formatDate = (dateStr: string) =>
        new Date(dateStr).toLocaleDateString('tr-TR', { day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit' });

    const handleMarkAllRead = async () => {
        try {
            await notificationsApi.markAllAsRead();
            setNotifications((prev) => prev.map((n) => ({ ...n, isRead: true, readAt: new Date().toISOString() })));
            setUnreadCount(0);
        } catch { /* ignore */ }
    };

    const handleNotificationClick = async (notification: NotificationDto) => {
        try {
            if (!notification.isRead) {
                await notificationsApi.markAsRead(notification.id);
                setNotifications((prev) =>
                    prev.map((n) => (n.id === notification.id ? { ...n, isRead: true, readAt: new Date().toISOString() } : n))
                );
                fetchUnreadCount();
            }
        } catch { /* ignore */ }
        setShowNotifications(false);
        navigate('/notifications');
    };

    const initials = user?.userName
        ? user.userName.slice(0, 2).toUpperCase()
        : '??';

    const primaryRole = roles.length > 0 ? roles[0] : 'Member';

    return (
        <div className={styles.appLayout}>
            {/* ── Mobile overlay ──────── */}
            {sidebarOpen && (
                <div className={styles.sidebarOverlay} onClick={() => setSidebarOpen(false)} aria-hidden=”true” />
            )}

            {/* ── Sidebar ─────────────── */}
            <aside className={`${styles.sidebar} ${sidebarOpen ? styles.sidebarOpen : ''}`} aria-label=”Yan menü”>
                <div className={styles.sidebar__header}>
                    <div className={styles.sidebar__logoIcon}>âš¡</div>
                    <span className={styles.sidebar__logoText}>BitirmeProject</span>
                </div>

                <nav className={styles.sidebar__nav}>
                    <span className={styles.sidebar__sectionLabel}>Ana MenÃ¼</span>

                    <NavLink
                        to="/projects"
                        className={({ isActive }) =>
                            `${styles.sidebar__link} ${isActive ? styles.sidebar__linkActive : ''}`
                        }
                        data-testid="nav-projects"
                    >
                        <span className={styles.sidebar__linkIcon}>ğŸ“</span>
                        Projeler
                    </NavLink>

                    <NavLink
                        to="/notifications"
                        className={({ isActive }) =>
                            `${styles.sidebar__link} ${isActive ? styles.sidebar__linkActive : ''}`
                        }
                        data-testid="nav-notifications"
                    >
                        <span className={styles.sidebar__linkIcon}>ğŸ””</span>
                        Bildirimler
                        {unreadCount > 0 && <span className={styles.sidebar__badge}>{unreadCount > 9 ? '9+' : unreadCount}</span>}
                    </NavLink>

                    {flags?.canViewAdmin && (
                        <>
                            <span className={styles.sidebar__sectionLabel}>YÃ¶netim</span>
                            <NavLink
                                to="/admin"
                                className={({ isActive }) =>
                                    `${styles.sidebar__link} ${isActive ? styles.sidebar__linkActive : ''}`
                                }
                            >
                                <span className={styles.sidebar__linkIcon}>âš™ï¸</span>
                                Admin Panel
                            </NavLink>
                        </>
                    )}
                </nav>

                <div className={styles.sidebar__footer}>
                    <div className={styles.sidebar__user}>
                        <div className={styles.sidebar__avatar}>{initials}</div>
                        <div className={styles.sidebar__userInfo}>
                            <div className={styles.sidebar__userName}>{user?.userName}</div>
                            <div className={styles.sidebar__userRole}>{primaryRole}</div>
                        </div>
                    </div>
                </div>
            </aside>

            {/* â”€â”€ Main â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€ */}
            <div className={styles.main}>
                <header className={styles.topbar}>
                    <div className={styles.topbar__left}>
                        <button
                            className={styles.hamburgerBtn}
                            onClick={() => setSidebarOpen((v) => !v)}
                            aria-label={sidebarOpen ? 'Menüyü kapat' : 'Menüyü aç'}
                            aria-expanded={sidebarOpen}
                        >
                            {sidebarOpen ? '✕' : '☰'}
                        </button>
                        <span className={styles.topbar__breadcrumb}>{getBreadcrumb()}</span>
                    </div>
                    <div className={styles.topbar__right}>
                        <button
                            className={styles.topbar__iconBtn}
                            title={theme === 'light' ? 'Karanlık mod' : 'Aydınlık mod'}
                            onClick={toggleTheme}
                            aria-label="Tema değiştir"
                        >
                            {theme === 'light' ? '🌙' : '☀️'}
                        </button>
                        <div className={styles.topbar__notifications} ref={notificationsRef}>
                            <button
                                className={styles.topbar__iconBtn}
                                title="Bildirimler"
                                onClick={() => setShowNotifications((prev) => !prev)}
                                data-testid="topbar-notifications"
                            >
                                🔔
                                {unreadCount > 0 && (
                                    <span className={styles.topbar__badge}>{unreadCount > 9 ? '9+' : unreadCount}</span>
                                )}
                            </button>
                            {showNotifications && (
                                <div className={styles.notificationsDropdown}>
                                    <div className={styles.notificationsHeader}>
                                        <span>Bildirimler</span>
                                        {notifications.length > 0 && (
                                            <button className={styles.notificationsAction} onClick={handleMarkAllRead}>
                                                Tümünü okundu yap
                                            </button>
                                        )}
                                    </div>
                                    {notificationsLoading && (
                                        <div className={styles.notificationsEmpty}>Yükleniyor...</div>
                                    )}
                                    {notificationsError && !notificationsLoading && (
                                        <div className={styles.notificationsEmpty}>{notificationsError}</div>
                                    )}
                                    {!notificationsLoading && !notificationsError && notifications.length === 0 && (
                                        <div className={styles.notificationsEmpty}>Henüz bildirim yok.</div>
                                    )}
                                    {!notificationsLoading && !notificationsError && notifications.length > 0 && (
                                        <div className={styles.notificationsList}>
                                            {notifications.slice(0, 5).map((notification) => (
                                                <button
                                                    key={notification.id}
                                                    className={`${styles.notificationItem} ${!notification.isRead ? styles.notificationUnread : ''}`}
                                                    onClick={() => handleNotificationClick(notification)}
                                                >
                                                    <div className={styles.notificationTitle}>{notification.title}</div>
                                                    <div className={styles.notificationMessage}>{notification.message}</div>
                                                    <div className={styles.notificationDate}>{formatDate(notification.createdAt)}</div>
                                                </button>
                                            ))}
                                        </div>
                                    )}
                                    <button
                                        className={styles.notificationsFooter}
                                        onClick={() => {
                                            setShowNotifications(false);
                                            navigate('/notifications');
                                        }}
                                    >
                                        Tüm bildirimleri gör
                                    </button>
                                </div>
                            )}
                        </div>
                        <button className={styles.topbar__logoutBtn} onClick={handleLogout} aria-label="Çıkış yap">
                            Ã‡Ä±kÄ±ÅŸ Yap
                        </button>
                    </div>
                </header>

                <main id="main-content" className={styles.content}>
                    <Outlet />
                </main>
            </div>
        </div>
    );
}
