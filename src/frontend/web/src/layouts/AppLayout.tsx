import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import { useAuthStore } from '../store/authStore';
import { useThemeStore } from '../store/themeStore';
import styles from './AppLayout.module.css';

export default function AppLayout() {
    const { user, roles, flags, logout } = useAuthStore();
    const { theme, toggleTheme } = useThemeStore();
    const navigate = useNavigate();

    const handleLogout = () => {
        logout();
        navigate('/login');
    };

    const initials = user?.userName
        ? user.userName.slice(0, 2).toUpperCase()
        : '??';

    const primaryRole = roles.length > 0 ? roles[0] : 'Member';

    return (
        <div className={styles.appLayout}>
            {/* ── Sidebar ─────────────── */}
            <aside className={styles.sidebar}>
                <div className={styles.sidebar__header}>
                    <div className={styles.sidebar__logoIcon}>⚡</div>
                    <span className={styles.sidebar__logoText}>BitirmeProject</span>
                </div>

                <nav className={styles.sidebar__nav}>
                    <span className={styles.sidebar__sectionLabel}>Ana Menü</span>

                    <NavLink
                        to="/projects"
                        className={({ isActive }) =>
                            `${styles.sidebar__link} ${isActive ? styles.sidebar__linkActive : ''}`
                        }
                    >
                        <span className={styles.sidebar__linkIcon}>📁</span>
                        Projeler
                    </NavLink>

                    <NavLink
                        to="/notifications"
                        className={({ isActive }) =>
                            `${styles.sidebar__link} ${isActive ? styles.sidebar__linkActive : ''}`
                        }
                    >
                        <span className={styles.sidebar__linkIcon}>🔔</span>
                        Bildirimler
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
                            <div className={styles.sidebar__userRole}>{primaryRole}</div>
                        </div>
                    </div>
                </div>
            </aside>

            {/* ── Main ────────────────── */}
            <div className={styles.main}>
                <header className={styles.topbar}>
                    <div className={styles.topbar__left}>
                        <span className={styles.topbar__breadcrumb}>Kontrol Paneli</span>
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
                        <button
                            className={styles.topbar__iconBtn}
                            title="Bildirimler"
                            onClick={() => navigate('/notifications')}
                        >
                            🔔
                        </button>
                        <button className={styles.topbar__logoutBtn} onClick={handleLogout}>
                            Çıkış Yap
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
