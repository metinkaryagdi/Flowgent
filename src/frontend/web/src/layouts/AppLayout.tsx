import { useState, useEffect, useCallback, useRef } from 'react';
import { NavLink, Outlet, useNavigate, useLocation } from 'react-router-dom';
import { useAuthStore } from '../store/authStore';
import { useThemeStore } from '../store/themeStore';
import { authApi } from '../api/auth';
import { notificationsApi } from '../api/notifications';
import { organizationsApi } from '../api/organizations';
import type { NotificationDto, OrganizationDto } from '../types';
import styles from './AppLayout.module.css';

export default function AppLayout() {
    const { user, roles, flags, logout, activeOrg, setActiveOrg, setFlags } = useAuthStore();
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

    // Org switcher state
    const [allOrgs, setAllOrgs] = useState<OrganizationDto[]>([]);
    const [showOrgDropdown, setShowOrgDropdown] = useState(false);
    const [orgSwitching, setOrgSwitching] = useState(false);
    const orgDropdownRef = useRef<HTMLDivElement>(null);
    // Prevents child pages from making API calls with a stale JWT before switchOrg completes
    const [orgReady, setOrgReady] = useState(false);

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
            setNotificationsError('Bildirimler yüklenemedi.');
        } finally {
            setNotificationsLoading(false);
        }
    }, []);

    // Load all orgs + always refresh JWT by calling switchOrg on mount
    const loadOrgs = useCallback(async () => {
        try {
            const orgs = await organizationsApi.getAll();
            setAllOrgs(orgs);

            if (orgs.length === 0) return;

            // Determine which org to activate: prefer the currently stored one, else first
            const storedId = activeOrg?.id;
            const targetOrg = (storedId ? orgs.find((o) => o.id === storedId) : null) ?? orgs[0];

            // Always call switchOrg to issue a fresh JWT with correct org_id/org_role claims
            try {
                const result = await organizationsApi.switchOrg(targetOrg.id);
                setActiveOrg({ id: result.orgId, name: result.orgName, role: result.orgRole });

                // Re-fetch UI flags after JWT refresh so canManageProjects reflects the new org_role
                try {
                    const updatedFlags = await authApi.getFlags();
                    setFlags(updatedFlags);
                } catch { /* flags fetch failure is non-fatal */ }
            } catch {
                // Fallback: keep existing org info if switch fails
                if (!activeOrg) {
                    setActiveOrg({ id: targetOrg.id, name: targetOrg.name, role: '' });
                }
            } finally {
                setOrgReady(true);
            }
        } catch { /* ignore */ } finally {
            // No orgs — still mark ready so pages can render their empty state
            setOrgReady(true);
        }
    }, []); // eslint-disable-line react-hooks/exhaustive-deps

    useEffect(() => {
        void loadOrgs();
    }, [loadOrgs]);

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

    useEffect(() => {
        const handleClick = (event: MouseEvent) => {
            if (!orgDropdownRef.current) return;
            if (!orgDropdownRef.current.contains(event.target as Node)) {
                setShowOrgDropdown(false);
            }
        };
        if (showOrgDropdown) {
            document.addEventListener('mousedown', handleClick);
        }
        return () => document.removeEventListener('mousedown', handleClick);
    }, [showOrgDropdown]);

    // Sayfa değişince sidebar'ı kapat (mobile)
    useEffect(() => {
        setSidebarOpen(false);
    }, [location.pathname]);

    const getBreadcrumb = () => {
        const path = location.pathname;
        if (path === '/projects') return 'Projeler';
        if (path === '/notifications') return 'Bildirimler';
        if (path === '/ai-assistant') return 'AI Asistan';
        if (path === '/admin') return 'Yönetim Paneli';
        if (path === '/settings/organization') {
            return roles.includes('Admin') ? 'Organizasyon Yönetimi' : 'Organizasyon Ayarları';
        }
        if (path.includes('/board')) return 'Projeler › Board';
        if (path.includes('/sprints')) return 'Projeler › Sprint';
        return 'Kontrol Paneli';
    };

    const handleLogout = async () => {
        try {
            await authApi.logout();
        } catch { /* sunucu erişilemese bile yerel oturum temizlenir */ }
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

    const handleSwitchOrg = async (org: OrganizationDto) => {
        if (orgSwitching || activeOrg?.id === org.id) {
            setShowOrgDropdown(false);
            return;
        }
        setOrgSwitching(true);
        setShowOrgDropdown(false);
        try {
            const result = await organizationsApi.switchOrg(org.id);
            setActiveOrg({ id: result.orgId, name: result.orgName, role: result.orgRole });
            // Reload to refresh JWT-dependent data
            window.location.reload();
        } catch {
            // ignore, user stays on current org
        } finally {
            setOrgSwitching(false);
        }
    };

    const handleNewOrg = () => {
        setShowOrgDropdown(false);
        navigate('/onboarding');
    };

    const initials = user?.userName
        ? user.userName.slice(0, 2).toUpperCase()
        : '??';

    const roleLabel = activeOrg?.role || (roles.length > 0 ? roles.join(', ') : 'Member');

    const activeOrgName = activeOrg?.name ?? (allOrgs.length > 0 ? allOrgs[0].name : null);
    const activeOrgId = activeOrg?.id ?? (allOrgs.length > 0 ? allOrgs[0].id : null);

    return (
        <div className={styles.appLayout}>
            {/* ── Mobile overlay ──────── */}
            {sidebarOpen && (
                <div className={styles.sidebarOverlay} onClick={() => setSidebarOpen(false)} aria-hidden="true" />
            )}

            {/* ── Sidebar ─────────────── */}
            <aside className={`${styles.sidebar} ${sidebarOpen ? styles.sidebarOpen : ''}`} aria-label="Yan menü">
                <div className={styles.sidebar__header}>
                    <div className={styles.sidebar__logoIcon}>⚡</div>
                    <span className={styles.sidebar__logoText}>BitirmeProject</span>
                </div>

                {/* ── Org Switcher ── */}
                {(allOrgs.length > 0 || activeOrgName) && (
                    <div className={styles.orgSwitcher} ref={orgDropdownRef}>
                        <button
                            className={styles.orgSwitcherBtn}
                            onClick={() => setShowOrgDropdown((v) => !v)}
                            disabled={orgSwitching}
                        >
                            <span className={styles.orgSwitcherIcon}>🏢</span>
                            <span className={styles.orgSwitcherName}>
                                {orgSwitching ? 'Geçiş yapılıyor...' : (activeOrgName ?? 'Organizasyon')}
                            </span>
                            <span className={styles.orgSwitcherChevron}>▼</span>
                        </button>

                        {showOrgDropdown && (
                            <div className={styles.orgDropdown}>
                                <div className={styles.orgDropdownHeader}>Organizasyonlar</div>
                                {allOrgs.map((org) => (
                                    <button
                                        key={org.id}
                                        className={`${styles.orgDropdownItem} ${org.id === activeOrgId ? styles.orgDropdownItemActive : ''}`}
                                        onClick={() => handleSwitchOrg(org)}
                                    >
                                        <span className={styles.orgDropdownItemName}>{org.name}</span>
                                        {org.id === activeOrgId && (
                                            <span className={styles.orgDropdownItemCheck}>✓</span>
                                        )}
                                    </button>
                                ))}
                                <hr className={styles.orgDropdownDivider} />
                                <button className={styles.orgDropdownNewBtn} onClick={handleNewOrg}>
                                    + Yeni Organizasyon
                                </button>
                            </div>
                        )}
                    </div>
                )}

                <nav className={styles.sidebar__nav}>
                    <span className={styles.sidebar__sectionLabel}>Ana Menü</span>

                    <NavLink
                        to="/projects"
                        className={({ isActive }) =>
                            `${styles.sidebar__link} ${isActive ? styles.sidebar__linkActive : ''}`
                        }
                        data-testid="nav-projects"
                    >
                        <span className={styles.sidebar__linkIcon}>📁</span>
                        Projeler
                    </NavLink>

                    <NavLink
                        to="/notifications"
                        className={({ isActive }) =>
                            `${styles.sidebar__link} ${isActive ? styles.sidebar__linkActive : ''}`
                        }
                        data-testid="nav-notifications"
                    >
                        <span className={styles.sidebar__linkIcon}>🔔</span>
                        Bildirimler
                        {unreadCount > 0 && <span className={styles.sidebar__badge}>{unreadCount > 9 ? '9+' : unreadCount}</span>}
                    </NavLink>

                    <NavLink
                        to="/ai-assistant"
                        className={({ isActive }) =>
                            `${styles.sidebar__link} ${isActive ? styles.sidebar__linkActive : ''}`
                        }
                        data-testid="nav-ai-assistant"
                    >
                        <span className={styles.sidebar__linkIcon}>✦</span>
                        AI Asistan
                    </NavLink>

                    <span className={styles.sidebar__sectionLabel}>Ayarlar</span>
                    <NavLink
                        to="/settings/organization"
                        className={({ isActive }) =>
                            `${styles.sidebar__link} ${isActive ? styles.sidebar__linkActive : ''}`
                        }
                        data-testid="nav-organization"
                    >
                        <span className={styles.sidebar__linkIcon}>🏢</span>
                        Organizasyon
                    </NavLink>

                    {flags?.canViewAdmin && (
                        <>
                            <span className={styles.sidebar__sectionLabel}>Yönetim</span>
                            <NavLink
                                to="/admin"
                                className={({ isActive }) =>
                                    `${styles.sidebar__link} ${isActive ? styles.sidebar__linkActive : ''}`
                                }
                            >
                                <span className={styles.sidebar__linkIcon}>⚙️</span>
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
                            <div className={styles.sidebar__userRole}>{roleLabel}</div>
                        </div>
                    </div>
                </div>
            </aside>

            {/* ── Main ────────────────── */}
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
                            Çıkış Yap
                        </button>
                    </div>
                </header>

                <main id="main-content" className={styles.content}>
                    {(!orgReady && !roles.includes('Admin'))
                        ? <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', height: '100%', color: 'var(--text-secondary)' }}>Yükleniyor...</div>
                        : <Outlet />}
                </main>
            </div>
        </div>
    );
}
