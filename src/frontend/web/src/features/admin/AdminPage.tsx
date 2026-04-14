import { useState, useEffect } from 'react';
import { adminApi } from '../../api/admin';
import { useToastStore } from '../../store/toastStore';
import type { UserDto, RoleDto, AdminStatsDto, OrganizationDto } from '../../types';
import styles from './Admin.module.css';

const formatDate = (d: string) =>
    new Date(d).toLocaleDateString('tr-TR', { day: 'numeric', month: 'short', year: 'numeric' });

const formatDateTime = (d: string) =>
    new Date(d).toLocaleString('tr-TR', { day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit' });

const SEQ_BASE = 'http://localhost:5341';

interface SeqEvent {
    id: string;
    timestamp: string;
    level: string;
    renderedMessage: string;
    exception?: string;
}

async function fetchSeqLogs(): Promise<SeqEvent[]> {
    const res = await fetch(`${SEQ_BASE}/api/events?count=50`, {
        signal: AbortSignal.timeout(4000),
    });
    if (!res.ok) return [];
    const data = await res.json();
    const events: SeqEvent[] = Array.isArray(data) ? data : (data.events ?? []);
    return events
        .filter((e) => e.level === 'Error' || e.level === 'Fatal')
        .slice(0, 5);
}

type Tab = 'users' | 'orgs';

export default function AdminPage() {
    const [tab, setTab] = useState<Tab>('users');
    const [users, setUsers] = useState<UserDto[]>([]);
    const [roles, setRoles] = useState<RoleDto[]>([]);
    const [userRolesMap, setUserRolesMap] = useState<Record<string, string[]>>({});
    const [stats, setStats] = useState<AdminStatsDto | null>(null);
    const [orgs, setOrgs] = useState<OrganizationDto[]>([]);
    const [loading, setLoading] = useState(true);
    const [selectedUserId, setSelectedUserId] = useState<string | null>(null);
    const [seqLogs, setSeqLogs] = useState<SeqEvent[]>([]);
    const [seqStatus, setSeqStatus] = useState<'loading' | 'ok' | 'error'>('loading');
    const { addToast: showToast } = useToastStore();

    useEffect(() => {
        loadData();
        fetchSeqLogs()
            .then((logs) => { setSeqLogs(logs); setSeqStatus('ok'); })
            .catch(() => setSeqStatus('error'));
    }, []);

    const loadData = async () => {
        setLoading(true);
        try {
            const [userData, roleData, statsData, orgData] = await Promise.all([
                adminApi.getUsers(),
                adminApi.getRoles(),
                adminApi.getStats(),
                adminApi.getAdminOrgs(),
            ]);
            setUsers(userData);
            setRoles(roleData);
            setStats(statsData);
            setOrgs(orgData);

            // Load roles for each user
            const rolesMap: Record<string, string[]> = {};
            await Promise.all(
                userData.map(async (u) => {
                    try {
                        rolesMap[u.id] = await adminApi.getUserRoles(u.id);
                    } catch {
                        rolesMap[u.id] = [];
                    }
                })
            );
            setUserRolesMap(rolesMap);
        } catch {
            showToast('Veriler yüklenirken hata oluştu.', 'error');
        } finally {
            setLoading(false);
        }
    };

    const handleToggleActive = async (user: UserDto) => {
        try {
            if (user.isActive) {
                await adminApi.deactivateUser(user.id);
                showToast(`${user.userName} devre dışı bırakıldı.`);
            } else {
                await adminApi.activateUser(user.id);
                showToast(`${user.userName} aktif edildi.`);
            }
            setUsers((prev) =>
                prev.map((u) => (u.id === user.id ? { ...u, isActive: !u.isActive } : u))
            );
        } catch {
            showToast('İşlem başarısız.', 'error');
        }
    };

    const handleToggleRole = async (userId: string, roleName: string) => {
        const currentRoles = userRolesMap[userId] || [];
        const hasRole = currentRoles.includes(roleName);

        try {
            if (hasRole) {
                await adminApi.removeRole(userId, roleName);
                setUserRolesMap((prev) => ({
                    ...prev,
                    [userId]: prev[userId].filter((r) => r !== roleName),
                }));
                showToast(`Rol kaldırıldı: ${roleName}`);
            } else {
                await adminApi.assignRole(userId, roleName);
                setUserRolesMap((prev) => ({
                    ...prev,
                    [userId]: [...(prev[userId] || []), roleName],
                }));
                showToast(`Rol atandı: ${roleName}`);
            }
        } catch {
            showToast('Rol işlemi başarısız.', 'error');
        }
    };

    if (loading) {
        return (
            <div className={styles.adminPage}>
                <div className={styles.header}>
                    <div>
                        <h1 className={styles.title}>Yönetim Paneli</h1>
                        <p className={styles.subtitle}>Yükleniyor...</p>
                    </div>
                </div>
                <div className={styles.tableWrap}>
                    {[1, 2, 3, 4].map((i) => <div key={i} className={styles.skeleton} />)}
                </div>
            </div>
        );
    }

    return (
        <div className={styles.adminPage}>
            {/* ── Header ─────────────── */}
            <div className={styles.header}>
                <div>
                    <h1 className={styles.title}>Yönetim Paneli</h1>
                    <p className={styles.subtitle}>Sistem geneli kullanıcı ve organizasyon yönetimi</p>
                </div>
            </div>

            {/* ── Stats Cards ─────────── */}
            {stats && (
                <div className={styles.statsGrid}>
                    <div className={styles.statCard}>
                        <div className={styles.statLabel}>Toplam Kullanıcı</div>
                        <div className={styles.statValue}>{stats.totalUsers}</div>
                        <div className={styles.statSub}>{stats.activeUsers} aktif</div>
                    </div>
                    <div className={styles.statCard}>
                        <div className={styles.statLabel}>Aktif Kullanıcı</div>
                        <div className={styles.statValue}>{stats.activeUsers}</div>
                        <div className={styles.statSub}>{stats.totalUsers - stats.activeUsers} pasif</div>
                    </div>
                    <div className={styles.statCard}>
                        <div className={styles.statLabel}>Organizasyon</div>
                        <div className={styles.statValue}>{stats.totalOrgs}</div>
                        <div className={styles.statSub}>toplam kayıtlı</div>
                    </div>
                </div>
            )}

            {/* ── Seq Log Widget ─────── */}
            <div className={styles.seqWidget}>
                <div className={styles.seqWidgetHeader}>
                    <span className={styles.seqWidgetTitle}>Son Sistem Hataları</span>
                    <a
                        href={SEQ_BASE}
                        target="_blank"
                        rel="noreferrer"
                        className={styles.seqWidgetLink}
                    >
                        Tüm Logları Gör →
                    </a>
                </div>
                {seqStatus === 'loading' && (
                    <div className={styles.seqEmpty}>Yükleniyor...</div>
                )}
                {seqStatus === 'error' && (
                    <div className={styles.seqEmpty}>Seq&apos;e bağlanılamadı (localhost:5341)</div>
                )}
                {seqStatus === 'ok' && seqLogs.length === 0 && (
                    <div className={styles.seqEmpty}>Son dönemde hata logu yok.</div>
                )}
                {seqStatus === 'ok' && seqLogs.length > 0 && (
                    <ul className={styles.seqList}>
                        {seqLogs.map((log) => (
                            <li key={log.id} className={styles.seqRow}>
                                <span className={log.level === 'Fatal' ? styles.seqBadgeFatal : styles.seqBadgeError}>
                                    {log.level}
                                </span>
                                <span className={styles.seqTime}>{formatDateTime(log.timestamp)}</span>
                                <span className={styles.seqMsg}>{log.renderedMessage}</span>
                            </li>
                        ))}
                    </ul>
                )}
            </div>

            {/* ── Tabs ───────────────── */}
            <div className={styles.tabs}>
                <button
                    className={`${styles.tab} ${tab === 'users' ? styles.tabActive : ''}`}
                    onClick={() => setTab('users')}
                >
                    Kullanıcılar ({users.length})
                </button>
                <button
                    className={`${styles.tab} ${tab === 'orgs' ? styles.tabActive : ''}`}
                    onClick={() => setTab('orgs')}
                >
                    Organizasyonlar ({orgs.length})
                </button>
            </div>

            {/* ── Users Tab ──────────── */}
            {tab === 'users' && (
                <>
                    {users.length === 0 ? (
                        <div className={styles.empty}>Kullanıcı bulunamadı.</div>
                    ) : (
                        <div className={styles.tableWrap}>
                            <table className={styles.table}>
                                <thead>
                                    <tr>
                                        <th>Kullanıcı</th>
                                        <th>Organizasyon</th>
                                        <th>Durum</th>
                                        <th>Roller</th>
                                        <th>Kayıt Tarihi</th>
                                        <th>İşlemler</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {users.map((user) => {
                                        const userRoles = userRolesMap[user.id] || [];
                                        return (
                                            <tr key={user.id}>
                                                <td>
                                                    <div className={styles.userInfo}>
                                                        <div className={styles.userAvatar}>
                                                            {user.userName.slice(0, 2).toUpperCase()}
                                                        </div>
                                                        <div>
                                                            <div className={styles.userName}>{user.userName}</div>
                                                            <div className={styles.userEmail}>{user.email}</div>
                                                        </div>
                                                    </div>
                                                </td>
                                                <td>
                                                    {user.orgName ? (
                                                        <span className={styles.orgTag}>{user.orgName}</span>
                                                    ) : (
                                                        <span style={{ color: 'var(--text-tertiary)', fontSize: 'var(--font-size-xs)' }}>—</span>
                                                    )}
                                                </td>
                                                <td>
                                                    <span className={user.isActive ? styles.statusActive : styles.statusInactive}>
                                                        {user.isActive ? 'Aktif' : 'Pasif'}
                                                    </span>
                                                </td>
                                                <td>
                                                    <div className={styles.roleTags}>
                                                        {userRoles.length === 0 ? (
                                                            <span style={{ color: 'var(--text-tertiary)', fontSize: 'var(--font-size-xs)' }}>—</span>
                                                        ) : (
                                                            userRoles.map((r) => (
                                                                <span
                                                                    key={r}
                                                                    className={`${styles.roleTag} ${r.toLowerCase() === 'admin' ? styles.roleTagAdmin : ''}`}
                                                                >
                                                                    {r}
                                                                </span>
                                                            ))
                                                        )}
                                                    </div>
                                                </td>
                                                <td>{formatDate(user.createdAt)}</td>
                                                <td>
                                                    <button
                                                        className={styles.actionBtn}
                                                        onClick={() => setSelectedUserId(user.id)}
                                                    >
                                                        Roller
                                                    </button>
                                                    <button
                                                        className={styles.actionBtnDanger}
                                                        onClick={() => handleToggleActive(user)}
                                                    >
                                                        {user.isActive ? 'Devre Dışı' : 'Aktif Et'}
                                                    </button>
                                                </td>
                                            </tr>
                                        );
                                    })}
                                </tbody>
                            </table>
                        </div>
                    )}
                </>
            )}

            {/* ── Orgs Tab ───────────── */}
            {tab === 'orgs' && (
                <>
                    {orgs.length === 0 ? (
                        <div className={styles.empty}>Organizasyon bulunamadı.</div>
                    ) : (
                        <div className={styles.tableWrap}>
                            <table className={styles.table}>
                                <thead>
                                    <tr>
                                        <th>Organizasyon</th>
                                        <th>Üye Sayısı</th>
                                        <th>Oluşturulma</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {orgs.map((org) => (
                                        <tr key={org.id}>
                                            <td>
                                                <div style={{ fontWeight: 600 }}>{org.name}</div>
                                                <div style={{ fontSize: 'var(--font-size-xs)', color: 'var(--text-tertiary)' }}>
                                                    {org.id.slice(0, 8)}...
                                                </div>
                                            </td>
                                            <td>{org.memberCount} üye</td>
                                            <td>{formatDate(org.createdAt)}</td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>
                        </div>
                    )}
                </>
            )}

            {/* ── Role Modal ─────────── */}
            {selectedUserId && (
                <div className={styles.modalOverlay} onClick={() => setSelectedUserId(null)}>
                    <div className={styles.modal} onClick={(e) => e.stopPropagation()}>
                        <h2 className={styles.modalTitle}>
                            Rol Yönetimi — {users.find((u) => u.id === selectedUserId)?.userName}
                        </h2>
                        <div className={styles.roleList}>
                            {roles.map((role) => {
                                const isAssigned = (userRolesMap[selectedUserId] || []).includes(role.name);
                                return (
                                    <div key={role.id} className={styles.roleRow}>
                                        <span className={styles.roleRowName}>{role.name}</span>
                                        <button
                                            className={`${styles.roleToggle} ${isAssigned ? styles.roleAssigned : styles.roleUnassigned}`}
                                            onClick={() => handleToggleRole(selectedUserId, role.name)}
                                        >
                                            {isAssigned ? '✓ Atandı' : 'Ata'}
                                        </button>
                                    </div>
                                );
                            })}
                        </div>
                        <button className={styles.modalClose} onClick={() => setSelectedUserId(null)}>
                            Kapat
                        </button>
                    </div>
                </div>
            )}
        </div>
    );
}
