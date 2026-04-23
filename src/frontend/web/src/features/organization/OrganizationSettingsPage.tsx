import { useState, useEffect, useCallback, type FormEvent } from 'react';
import { adminApi } from '../../api/admin';
import { organizationsApi } from '../../api/organizations';
import { useAuthStore } from '../../store/authStore';
import { useToastStore } from '../../store/toastStore';
import type { OrganizationDto, OrganizationMemberDto, InviteDto, OrganizationRole } from '../../types';
import styles from './OrganizationSettings.module.css';

export default function OrganizationSettingsPage() {
    const roles = useAuthStore((state) => state.roles);

    if (roles.includes('Admin')) {
        return <AdminOrganizationsPanel />;
    }

    return <MemberOrganizationSettingsPage />;
}

function MemberOrganizationSettingsPage() {
    const { user, activeOrg } = useAuthStore();

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

    const canManageInvites = activeOrg?.role === 'Owner' || activeOrg?.role === 'Manager';

    const loadData = useCallback(async () => {
        setLoading(true);
        setError('');
        try {
            const orgData = await organizationsApi.getMy();
            setOrg(orgData);
            if (orgData) {
                const membersData = await organizationsApi.getMembers(orgData.id);
                setMembers(membersData);
                if (canManageInvites) {
                    const invitesData = await organizationsApi.getPendingInvites(orgData.id);
                    setPendingInvites(invitesData);
                }
            }
        } catch {
            setError('Organizasyon bilgileri yüklenemedi.');
        } finally {
            setLoading(false);
        }
    }, [canManageInvites]);

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

function AdminOrganizationsPanel() {
    const [orgs, setOrgs] = useState<OrganizationDto[]>([]);
    const [selectedOrgId, setSelectedOrgId] = useState<string | null>(null);
    const [membersByOrg, setMembersByOrg] = useState<Record<string, OrganizationMemberDto[]>>({});
    const [loading, setLoading] = useState(true);
    const [membersLoading, setMembersLoading] = useState(false);
    const [error, setError] = useState('');
    const [query, setQuery] = useState('');
    const [pendingIds, setPendingIds] = useState<Set<string>>(new Set());
    const { addToast: showToast } = useToastStore();

    const loadOrgs = useCallback(async () => {
        setLoading(true);
        setError('');
        try {
            const data = await adminApi.getAdminOrgs();
            setOrgs(data);
            setSelectedOrgId((current) => current ?? data[0]?.id ?? null);
        } catch {
            setError('Organizasyonlar yuklenemedi.');
        } finally {
            setLoading(false);
        }
    }, []);

    const loadMembers = useCallback(async (orgId: string, force = false) => {
        setSelectedOrgId(orgId);
        if (!force && membersByOrg[orgId]) return;

        setMembersLoading(true);
        try {
            const members = await adminApi.getOrgMembers(orgId);
            setMembersByOrg((prev) => ({ ...prev, [orgId]: members }));
        } catch {
            showToast('Organizasyon uyeleri yuklenemedi.', 'error');
        } finally {
            setMembersLoading(false);
        }
    }, [membersByOrg, showToast]);

    useEffect(() => {
        void loadOrgs();
    }, [loadOrgs]);

    useEffect(() => {
        if (!loading && selectedOrgId && !membersByOrg[selectedOrgId]) {
            void loadMembers(selectedOrgId);
        }
    }, [loading, selectedOrgId, membersByOrg, loadMembers]);

    const runPending = async (id: string, action: () => Promise<void>) => {
        if (pendingIds.has(id)) return;
        setPendingIds((prev) => new Set(prev).add(id));
        try {
            await action();
        } finally {
            setPendingIds((prev) => {
                const next = new Set(prev);
                next.delete(id);
                return next;
            });
        }
    };

    const handleDeleteOrg = async (org: OrganizationDto) => {
        if (!window.confirm(`"${org.name}" organizasyonunu kalici olarak silmek istiyor musunuz?`)) return;

        await runPending(`org:${org.id}`, async () => {
            try {
                await adminApi.deleteOrg(org.id);
                setOrgs((prev) => prev.filter((item) => item.id !== org.id));
                setMembersByOrg((prev) => {
                    const next = { ...prev };
                    delete next[org.id];
                    return next;
                });
                setSelectedOrgId((current) => (current === org.id ? null : current));
                showToast(`${org.name} silindi.`);
            } catch {
                showToast('Organizasyon silinemedi.', 'error');
            }
        });
    };

    const handleRemoveMember = async (orgId: string, member: OrganizationMemberDto) => {
        if (member.role === 'Owner') {
            showToast('Organizasyon sahibi cikarilamaz.', 'error');
            return;
        }
        if (!window.confirm(`"${member.userName}" kullanicisini organizasyondan cikarmak istiyor musunuz?`)) return;

        await runPending(`member:${orgId}:${member.userId}`, async () => {
            try {
                await adminApi.removeOrgMember(orgId, member.userId);
                setMembersByOrg((prev) => ({
                    ...prev,
                    [orgId]: (prev[orgId] || []).filter((item) => item.userId !== member.userId),
                }));
                setOrgs((prev) =>
                    prev.map((org) =>
                        org.id === orgId
                            ? { ...org, memberCount: Math.max(0, org.memberCount - 1) }
                            : org
                    )
                );
                showToast(`${member.userName} organizasyondan cikarildi.`);
            } catch {
                showToast('Uye cikarilamadi.', 'error');
            }
        });
    };

    const handleChangeRole = async (orgId: string, member: OrganizationMemberDto, newRole: OrganizationRole) => {
        if (member.role === 'Owner' || member.role === newRole) return;

        await runPending(`role:${orgId}:${member.userId}`, async () => {
            try {
                await adminApi.changeOrgMemberRole(orgId, member.userId, newRole);
                setMembersByOrg((prev) => ({
                    ...prev,
                    [orgId]: (prev[orgId] || []).map((item) =>
                        item.userId === member.userId ? { ...item, role: newRole } : item
                    ),
                }));
                showToast(`${member.userName} rolu guncellendi.`);
            } catch {
                showToast('Rol guncellenemedi.', 'error');
            }
        });
    };

    const filteredOrgs = orgs.filter((org) =>
        org.name.toLocaleLowerCase('tr-TR').includes(query.trim().toLocaleLowerCase('tr-TR'))
    );
    const selectedOrg = orgs.find((org) => org.id === selectedOrgId) ?? null;
    const selectedMembers = selectedOrgId ? (membersByOrg[selectedOrgId] || []) : [];
    const totalMembers = orgs.reduce((sum, org) => sum + org.memberCount, 0);

    if (loading) {
        return <div className={styles.loading}>Yukleniyor...</div>;
    }

    return (
        <div className={styles.pageWide}>
            <div className={styles.adminHeader}>
                <div>
                    <h1 className={styles.pageTitle}>Organizasyon Yonetimi</h1>
                    <p className={styles.pageMeta}>
                        Sistemdeki tum organizasyonlari, uyeleri ve organizasyon rollerini yonetin.
                    </p>
                </div>
                <button className={styles.btnSecondary} onClick={loadOrgs}>
                    Yenile
                </button>
            </div>

            <div className={styles.adminStats}>
                <div className={styles.adminStat}>
                    <span className={styles.adminStatLabel}>Organizasyon</span>
                    <strong>{orgs.length}</strong>
                </div>
                <div className={styles.adminStat}>
                    <span className={styles.adminStatLabel}>Toplam uye</span>
                    <strong>{totalMembers}</strong>
                </div>
                <div className={styles.adminStat}>
                    <span className={styles.adminStatLabel}>Secili organizasyon</span>
                    <strong>{selectedOrg ? selectedOrg.name : '-'}</strong>
                </div>
            </div>

            {error && (
                <div className={styles.errorBox}>
                    <p>{error}</p>
                    <button className={styles.btnPrimary} onClick={loadOrgs}>Tekrar Dene</button>
                </div>
            )}

            {!error && (
                <section className={styles.section}>
                    <div className={styles.sectionHeader}>
                        <div>
                            <h2 className={styles.sectionTitle}>Tum Organizasyonlar</h2>
                            <p className={styles.sectionMeta}>
                                Organizasyonu secerek uyelerini ve rollerini yonetin.
                            </p>
                        </div>
                        <input
                            type="search"
                            className={styles.searchInput}
                            placeholder="Organizasyon ara"
                            value={query}
                            onChange={(event) => setQuery(event.target.value)}
                        />
                    </div>

                    {filteredOrgs.length === 0 ? (
                        <div className={styles.emptyInline}>Organizasyon bulunamadi.</div>
                    ) : (
                        <div className={styles.tableWrap}>
                            <table className={styles.table}>
                                <thead>
                                    <tr>
                                        <th>Organizasyon</th>
                                        <th>Uye Sayisi</th>
                                        <th>Olusturulma</th>
                                        <th>Islemler</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {filteredOrgs.map((org) => (
                                        <tr key={org.id} className={org.id === selectedOrgId ? styles.selectedRow : undefined}>
                                            <td>
                                                <div className={styles.orgNameCell}>{org.name}</div>
                                                <div className={styles.orgIdCell}>{org.id.slice(0, 8)}...</div>
                                            </td>
                                            <td>{org.memberCount} uye</td>
                                            <td>{formatAdminDate(org.createdAt)}</td>
                                            <td>
                                                <div className={styles.actionGroup}>
                                                    <button
                                                        className={styles.btnTable}
                                                        onClick={() => loadMembers(org.id, true)}
                                                    >
                                                        Uyeler
                                                    </button>
                                                    <button
                                                        className={styles.btnTableDanger}
                                                        disabled={pendingIds.has(`org:${org.id}`)}
                                                        onClick={() => handleDeleteOrg(org)}
                                                    >
                                                        Sil
                                                    </button>
                                                </div>
                                            </td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>
                        </div>
                    )}
                </section>
            )}

            {selectedOrg && (
                <section className={styles.section}>
                    <div className={styles.sectionHeader}>
                        <div>
                            <h2 className={styles.sectionTitle}>{selectedOrg.name} Uyeleri</h2>
                            <p className={styles.sectionMeta}>
                                Owner korunur; Manager ve Member rolleri sistem admini tarafindan yonetilebilir.
                            </p>
                        </div>
                        <button className={styles.btnSecondary} onClick={() => loadMembers(selectedOrg.id, true)}>
                            Uyeleri Yenile
                        </button>
                    </div>

                    {membersLoading && <div className={styles.emptyInline}>Uyeler yukleniyor...</div>}
                    {!membersLoading && selectedMembers.length === 0 && (
                        <div className={styles.emptyInline}>Uye bulunamadi.</div>
                    )}
                    {!membersLoading && selectedMembers.length > 0 && (
                        <div className={styles.tableWrap}>
                            <table className={styles.table}>
                                <thead>
                                    <tr>
                                        <th>Uye</th>
                                        <th>Rol</th>
                                        <th>Katilma</th>
                                        <th>Islemler</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {selectedMembers.map((member) => {
                                        const isOwner = member.role === 'Owner';
                                        return (
                                            <tr key={member.userId}>
                                                <td>
                                                    <div className={styles.memberName}>{member.userName}</div>
                                                    <div className={styles.memberEmail}>{member.email}</div>
                                                </td>
                                                <td>
                                                    {isOwner ? (
                                                        <span className={styles.roleBadge}>{roleTr(member.role)}</span>
                                                    ) : (
                                                        <select
                                                            className={styles.roleSelect}
                                                            value={member.role}
                                                            disabled={pendingIds.has(`role:${selectedOrg.id}:${member.userId}`)}
                                                            onChange={(event) =>
                                                                handleChangeRole(
                                                                    selectedOrg.id,
                                                                    member,
                                                                    event.target.value as OrganizationRole
                                                                )
                                                            }
                                                        >
                                                            <option value="Manager">Yonetici</option>
                                                            <option value="Member">Uye</option>
                                                        </select>
                                                    )}
                                                </td>
                                                <td>{formatAdminDate(member.joinedAt)}</td>
                                                <td>
                                                    <button
                                                        className={styles.btnTableDanger}
                                                        disabled={isOwner || pendingIds.has(`member:${selectedOrg.id}:${member.userId}`)}
                                                        title={isOwner ? 'Organizasyon sahibi cikarilamaz.' : undefined}
                                                        onClick={() => handleRemoveMember(selectedOrg.id, member)}
                                                    >
                                                        Cikar
                                                    </button>
                                                </td>
                                            </tr>
                                        );
                                    })}
                                </tbody>
                            </table>
                        </div>
                    )}
                </section>
            )}
        </div>
    );
}

function formatAdminDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString('tr-TR', {
        day: 'numeric',
        month: 'short',
        year: 'numeric',
    });
}

function roleTr(role: OrganizationRole | string): string {
    if (role === 'Owner') return 'Sahip';
    if (role === 'Manager') return 'Yönetici';
    return 'Üye';
}
