import { useState, useEffect } from 'react';
import { adminApi } from '../../api/admin';
import type { UserDto, RoleDto } from '../../types';
import styles from './Admin.module.css';

const formatDate = (d: string) =>
    new Date(d).toLocaleDateString('tr-TR', { day: 'numeric', month: 'short', year: 'numeric' });

export default function AdminPage() {
    const [users, setUsers] = useState<UserDto[]>([]);
    const [roles, setRoles] = useState<RoleDto[]>([]);
    const [userRolesMap, setUserRolesMap] = useState<Record<string, string[]>>({});
    const [loading, setLoading] = useState(true);
    const [selectedUserId, setSelectedUserId] = useState<string | null>(null);
    const [toast, setToast] = useState<{ message: string; type: 'success' | 'error' } | null>(null);

    const showToast = (message: string, type: 'success' | 'error' = 'success') => {
        setToast({ message, type });
        setTimeout(() => setToast(null), 3000);
    };

    useEffect(() => {
        loadData();
    }, []);

    const loadData = async () => {
        setLoading(true);
        try {
            const [userData, roleData] = await Promise.all([
                adminApi.getUsers(),
                adminApi.getRoles(),
            ]);
            setUsers(userData);
            setRoles(roleData);

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
                        <p className={styles.subtitle}>Kullanıcı ve rol yönetimi</p>
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
                    <p className={styles.subtitle}>{users.length} kullanıcı kayıtlı</p>
                </div>
            </div>

            {/* ── Users Table ────────── */}
            {users.length === 0 ? (
                <div className={styles.empty}>Kullanıcı bulunamadı.</div>
            ) : (
                <div className={styles.tableWrap}>
                    <table className={styles.table}>
                        <thead>
                            <tr>
                                <th>Kullanıcı</th>
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

            {/* ── Toast ─────────────── */}
            {toast && (
                <div className={`${styles.toast} ${toast.type === 'success' ? styles.toastSuccess : styles.toastError}`}>
                    {toast.message}
                </div>
            )}
        </div>
    );
}
