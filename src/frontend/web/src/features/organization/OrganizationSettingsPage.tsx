import { useState, useEffect, useCallback, type FormEvent } from 'react';
import { organizationsApi } from '../../api/organizations';
import { useAuthStore } from '../../store/authStore';
import type { OrganizationDto, OrganizationMemberDto, InviteDto, OrganizationRole } from '../../types';
import styles from './OrganizationSettings.module.css';

export default function OrganizationSettingsPage() {
    const { user } = useAuthStore();

    const [org, setOrg] = useState<OrganizationDto | null>(null);
    const [members, setMembers] = useState<OrganizationMemberDto[]>([]);
    const [pendingInvites, setPendingInvites] = useState<InviteDto[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState('');

    // Invite form
    const [inviteEmail, setInviteEmail] = useState('');
    const [inviteRole, setInviteRole] = useState<OrganizationRole>('Member');
    const [inviteLoading, setInviteLoading] = useState(false);
    const [inviteError, setInviteError] = useState('');
    const [inviteSuccess, setInviteSuccess] = useState('');

    // Create org form (when user has no org)
    const [createName, setCreateName] = useState('');
    const [createLoading, setCreateLoading] = useState(false);
    const [createError, setCreateError] = useState('');

    const loadData = useCallback(async () => {
        setLoading(true);
        setError('');
        try {
            const orgData = await organizationsApi.getMy();
            setOrg(orgData);
            if (orgData) {
                const [membersData, invitesData] = await Promise.all([
                    organizationsApi.getMembers(orgData.id),
                    organizationsApi.getPendingInvites(orgData.id),
                ]);
                setMembers(membersData);
                setPendingInvites(invitesData);
            }
        } catch {
            setError('Organizasyon bilgileri yüklenemedi.');
        } finally {
            setLoading(false);
        }
    }, []);

    useEffect(() => {
        void loadData();
    }, [loadData]);

    const handleCreateOrg = async (e: FormEvent) => {
        e.preventDefault();
        setCreateError('');
        if (!createName.trim()) {
            setCreateError('Organizasyon adı boş olamaz.');
            return;
        }
        setCreateLoading(true);
        try {
            await organizationsApi.create({ name: createName.trim() });
            await loadData();
        } catch (err: unknown) {
            if (err && typeof err === 'object' && 'response' in err) {
                const e = err as { response?: { data?: { message?: string } } };
                setCreateError(e.response?.data?.message || 'Organizasyon oluşturulamadı.');
            } else {
                setCreateError('Sunucuya bağlanılamadı.');
            }
        } finally {
            setCreateLoading(false);
        }
    };

    const handleSendInvite = async (e: FormEvent) => {
        e.preventDefault();
        setInviteError('');
        setInviteSuccess('');
        if (!inviteEmail.trim()) {
            setInviteError('E-posta adresi boş olamaz.');
            return;
        }
        if (!org) return;

        setInviteLoading(true);
        try {
            await organizationsApi.sendInvite({
                organizationId: org.id,
                email: inviteEmail.trim(),
                role: inviteRole,
                inviteLinkBaseUrl: window.location.origin,
            });
            setInviteSuccess(`${inviteEmail} adresine davet gönderildi.`);
            setInviteEmail('');
            const invitesData = await organizationsApi.getPendingInvites(org.id);
            setPendingInvites(invitesData);
        } catch (err: unknown) {
            if (err && typeof err === 'object' && 'response' in err) {
                const e = err as { response?: { data?: { message?: string } } };
                setInviteError(e.response?.data?.message || 'Davet gönderilemedi.');
            } else {
                setInviteError('Sunucuya bağlanılamadı.');
            }
        } finally {
            setInviteLoading(false);
        }
    };

    const handleRevokeInvite = async (inviteId: string) => {
        try {
            await organizationsApi.revokeInvite(inviteId);
            setPendingInvites((prev) => prev.filter((i) => i.id !== inviteId));
        } catch { /* ignore */ }
    };

    const handleRemoveMember = async (userId: string) => {
        if (!org) return;
        if (!window.confirm('Bu üyeyi organizasyondan çıkarmak istediğinizden emin misiniz?')) return;
        try {
            await organizationsApi.removeMember(org.id, userId);
            setMembers((prev) => prev.filter((m) => m.userId !== userId));
        } catch { /* ignore */ }
    };

    const handleChangeRole = async (userId: string, newRole: OrganizationRole) => {
        if (!org) return;
        try {
            await organizationsApi.changeMemberRole(org.id, userId, { newRole });
            setMembers((prev) =>
                prev.map((m) => (m.userId === userId ? { ...m, role: newRole } : m))
            );
        } catch { /* ignore */ }
    };

    const formatDate = (dateStr: string) =>
        new Date(dateStr).toLocaleDateString('tr-TR', { day: 'numeric', month: 'long', year: 'numeric' });

    const myRole = members.find((m) => m.userId === user?.id)?.role;
    const canManage = myRole === 'Owner' || myRole === 'Manager';

    if (loading) {
        return <div className={styles.loading}>Yükleniyor...</div>;
    }

    if (error) {
        return (
            <div className={styles.errorBox}>
                <p>{error}</p>
                <button className={styles.btnPrimary} onClick={loadData}>Tekrar Dene</button>
            </div>
        );
    }

    if (!org) {
        return (
            <div className={styles.page}>
                <div className={styles.emptyState}>
                    <div className={styles.emptyIcon}>🏢</div>
                    <h2>Henüz bir organizasyonunuz yok</h2>
                    <p>Ekibinizle çalışmak için bir organizasyon oluşturun.</p>
                    {createError && <p className={styles.errorText}>{createError}</p>}
                    <form onSubmit={handleCreateOrg} className={styles.createForm}>
                        <input
                            type="text"
                            className={styles.input}
                            placeholder="Organizasyon adı"
                            value={createName}
                            onChange={(e) => setCreateName(e.target.value)}
                            autoFocus
                        />
                        <button type="submit" className={styles.btnPrimary} disabled={createLoading}>
                            {createLoading ? 'Oluşturuluyor...' : 'Oluştur'}
                        </button>
                    </form>
                </div>
            </div>
        );
    }

    return (
        <div className={styles.page}>
            <div className={styles.pageHeader}>
                <div>
                    <h1 className={styles.pageTitle}>{org.name}</h1>
                    <p className={styles.pageMeta}>
                        {org.memberCount} üye · {formatDate(org.createdAt)} tarihinde oluşturuldu
                    </p>
                </div>
            </div>

            {/* Members Section */}
            <section className={styles.section}>
                <h2 className={styles.sectionTitle}>Üyeler</h2>
                <div className={styles.memberList}>
                    {members.map((member) => (
                        <div key={member.userId} className={styles.memberRow}>
                            <div className={styles.memberAvatar}>
                                {member.userName.slice(0, 2).toUpperCase()}
                            </div>
                            <div className={styles.memberInfo}>
                                <div className={styles.memberName}>
                                    {member.userName}
                                    {member.userId === user?.id && (
                                        <span className={styles.youBadge}>(siz)</span>
                                    )}
                                </div>
                                <div className={styles.memberEmail}>{member.email}</div>
                            </div>
                            <div className={styles.memberActions}>
                                {canManage && member.role !== 'Owner' && member.userId !== user?.id ? (
                                    <>
                                        <select
                                            className={styles.roleSelect}
                                            value={member.role}
                                            onChange={(e) =>
                                                handleChangeRole(member.userId, e.target.value as OrganizationRole)
                                            }
                                        >
                                            <option value="Manager">Yönetici</option>
                                            <option value="Member">Üye</option>
                                        </select>
                                        <button
                                            className={styles.btnDanger}
                                            onClick={() => handleRemoveMember(member.userId)}
                                        >
                                            Çıkar
                                        </button>
                                    </>
                                ) : (
                                    <span className={styles.roleBadge}>{roleTr(member.role)}</span>
                                )}
                            </div>
                        </div>
                    ))}
                </div>
            </section>

            {/* Invite Section (owner/manager only) */}
            {canManage && (
                <section className={styles.section}>
                    <h2 className={styles.sectionTitle}>Davet Gönder</h2>
                    {inviteError && <p className={styles.errorText}>{inviteError}</p>}
                    {inviteSuccess && <p className={styles.successText}>{inviteSuccess}</p>}
                    <form onSubmit={handleSendInvite} className={styles.inviteForm}>
                        <input
                            type="email"
                            className={styles.input}
                            placeholder="E-posta adresi"
                            value={inviteEmail}
                            onChange={(e) => setInviteEmail(e.target.value)}
                            data-testid="invite-email"
                        />
                        <select
                            className={styles.roleSelect}
                            value={inviteRole}
                            onChange={(e) => setInviteRole(e.target.value as OrganizationRole)}
                            data-testid="invite-role"
                        >
                            <option value="Manager">Yönetici</option>
                            <option value="Member">Üye</option>
                        </select>
                        <button
                            type="submit"
                            className={styles.btnPrimary}
                            disabled={inviteLoading}
                            data-testid="invite-submit"
                        >
                            {inviteLoading ? 'Gönderiliyor...' : 'Davet Gönder'}
                        </button>
                    </form>
                </section>
            )}

            {/* Pending Invites */}
            {canManage && pendingInvites.length > 0 && (
                <section className={styles.section}>
                    <h2 className={styles.sectionTitle}>Bekleyen Davetler</h2>
                    <div className={styles.memberList}>
                        {pendingInvites.map((invite) => (
                            <div key={invite.id} className={styles.memberRow}>
                                <div className={styles.memberInfo}>
                                    <div className={styles.memberName}>{invite.email}</div>
                                    <div className={styles.memberEmail}>
                                        {roleTr(invite.role as OrganizationRole)} · Son geçerlilik: {formatDate(invite.expiresAt)}
                                    </div>
                                </div>
                                <button
                                    className={styles.btnDanger}
                                    onClick={() => handleRevokeInvite(invite.id)}
                                >
                                    İptal Et
                                </button>
                            </div>
                        ))}
                    </div>
                </section>
            )}
        </div>
    );
}

function roleTr(role: OrganizationRole | string): string {
    if (role === 'Owner') return 'Sahip';
    if (role === 'Manager') return 'Yönetici';
    return 'Üye';
}
