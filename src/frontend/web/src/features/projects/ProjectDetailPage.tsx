import { useState, useEffect, type FormEvent } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { projectsApi } from '../../api/projects';
import { useAuthStore } from '../../store/authStore';
import type { ProjectDto, ProjectMemberDto } from '../../types';
import styles from './ProjectDetail.module.css';

const formatDate = (d: string) =>
    new Date(d).toLocaleDateString('tr-TR', { day: 'numeric', month: 'short', year: 'numeric' });

export default function ProjectDetailPage() {
    const { projectId } = useParams<{ projectId: string }>();
    const navigate = useNavigate();
    const { user, flags } = useAuthStore();

    const [project, setProject] = useState<ProjectDto | null>(null);
    const [members, setMembers] = useState<ProjectMemberDto[]>([]);
    const [loading, setLoading] = useState(true);
    const [showAddModal, setShowAddModal] = useState(false);
    const [toast, setToast] = useState<{ message: string; type: 'success' | 'error' } | null>(null);

    const showToast = (message: string, type: 'success' | 'error' = 'success') => {
        setToast({ message, type });
        setTimeout(() => setToast(null), 3000);
    };

    useEffect(() => {
        loadData();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [projectId]);

    const loadData = async () => {
        if (!projectId) return;
        setLoading(true);
        try {
            const [proj, mems] = await Promise.all([
                projectsApi.getById(projectId),
                projectsApi.getMembers(projectId),
            ]);
            setProject(proj);
            setMembers(mems);
        } catch {
            showToast('Proje bilgileri yüklenirken hata oluştu.', 'error');
        } finally {
            setLoading(false);
        }
    };

    const handleAddMember = async (userId: string) => {
        if (!projectId) return;
        try {
            await projectsApi.addMember(projectId, { userId });
            showToast('Üye eklendi!');
            setShowAddModal(false);
            await loadData();
        } catch {
            showToast('Üye eklenirken hata oluştu.', 'error');
        }
    };

    const handleRemoveMember = async (userId: string) => {
        if (!projectId) return;
        try {
            await projectsApi.removeMember(projectId, userId);
            showToast('Üye çıkarıldı.');
            await loadData();
        } catch {
            showToast('Üye çıkarılırken hata oluştu.', 'error');
        }
    };

    if (loading) {
        return (
            <div className={styles.detailPage}>
                <div className={styles.header}>
                    <div className={styles.headerLeft}>
                        <button className={styles.backBtn} onClick={() => navigate('/projects')}>←</button>
                        <h1 className={styles.title}>Yükleniyor...</h1>
                    </div>
                </div>
                <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
                    {[1, 2, 3].map((i) => <div key={i} className={styles.skeleton} />)}
                </div>
            </div>
        );
    }

    if (!project) {
        return (
            <div className={styles.detailPage}>
                <div className={styles.header}>
                    <div className={styles.headerLeft}>
                        <button className={styles.backBtn} onClick={() => navigate('/projects')}>←</button>
                        <h1 className={styles.title}>Proje bulunamadı</h1>
                    </div>
                </div>
            </div>
        );
    }

    return (
        <div className={styles.detailPage}>
            {/* ── Header ─────────────── */}
            <div className={styles.header}>
                <div className={styles.headerLeft}>
                    <button className={styles.backBtn} onClick={() => navigate('/projects')}>←</button>
                    <h1 className={styles.title}>
                        {project.name}
                        <span className={styles.projectKey}>{project.key}</span>
                    </h1>
                </div>
            </div>

            {/* ── Info Cards ──────────── */}
            <div className={styles.infoGrid}>
                <div className={styles.infoCard}>
                    <div className={styles.infoCardLabel}>Toplam Issue</div>
                    <div className={styles.infoCardValue}>{project.issueCount}</div>
                </div>
                <div className={styles.infoCard}>
                    <div className={styles.infoCardLabel}>Açık</div>
                    <div className={styles.infoCardValue} style={{ color: 'var(--status-open)' }}>{project.openIssueCount}</div>
                </div>
                <div className={styles.infoCard}>
                    <div className={styles.infoCardLabel}>Devam Eden</div>
                    <div className={styles.infoCardValue} style={{ color: 'var(--status-in-progress)' }}>{project.inProgressIssueCount}</div>
                </div>
                <div className={styles.infoCard}>
                    <div className={styles.infoCardLabel}>Tamamlanan</div>
                    <div className={styles.infoCardValue} style={{ color: 'var(--status-done)' }}>{project.doneIssueCount}</div>
                </div>
                <div className={styles.infoCard}>
                    <div className={styles.infoCardLabel}>Oluşturulma</div>
                    <div className={styles.infoCardValue} style={{ fontSize: 'var(--font-size-base)' }}>{formatDate(project.createdAt)}</div>
                </div>
            </div>

            {/* ── Nav Links ─────────── */}
            <div style={{ display: 'flex', gap: 12 }}>
                <button
                    className={styles.addMemberBtn}
                    style={{ background: 'var(--color-info)' }}
                    onClick={() => navigate(`/projects/${projectId}/board`)}
                >
                    📋 Board'a Git
                </button>
                <button
                    className={styles.addMemberBtn}
                    style={{ background: 'var(--color-warning)' }}
                    onClick={() => navigate(`/projects/${projectId}/sprints`)}
                >
                    📊 Sprint'lere Git
                </button>
            </div>

            {/* ── Members ────────────── */}
            <div className={styles.section}>
                <div className={styles.sectionHeader}>
                    <h2 className={styles.sectionTitle}>Üyeler ({members.length})</h2>
                    {flags?.canManageProjects !== false && (
                        <button className={styles.addMemberBtn} onClick={() => setShowAddModal(true)}>
                            + Üye Ekle
                        </button>
                    )}
                </div>
                {members.length === 0 ? (
                    <div className={styles.emptyMembers}>Henüz üye eklenmemiş.</div>
                ) : (
                    <div className={styles.memberList}>
                        {members.map((member) => (
                            <div key={member.userId} className={styles.memberItem}>
                                <div className={styles.memberInfo}>
                                    <div className={styles.memberAvatar}>
                                        {member.userId.slice(0, 2).toUpperCase()}
                                    </div>
                                    <div>
                                        <div className={styles.memberName}>{member.userId}</div>
                                        <div className={styles.memberId}>Eklenme: {formatDate(member.addedAt)}</div>
                                    </div>
                                </div>
                                {flags?.canManageProjects !== false && member.userId !== user?.id && (
                                    <button
                                        className={styles.removeBtn}
                                        onClick={() => handleRemoveMember(member.userId)}
                                    >
                                        Çıkar
                                    </button>
                                )}
                            </div>
                        ))}
                    </div>
                )}
            </div>

            {/* ── Add Member Modal ──── */}
            {showAddModal && (
                <AddMemberModal
                    onSubmit={handleAddMember}
                    onClose={() => setShowAddModal(false)}
                />
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

/* ═══════════════════════════════
   Add Member Modal
   ═══════════════════════════════ */
function AddMemberModal({
    onSubmit,
    onClose,
}: {
    onSubmit: (userId: string) => Promise<void>;
    onClose: () => void;
}) {
    const [userId, setUserId] = useState('');
    const [submitting, setSubmitting] = useState(false);
    const [error, setError] = useState('');

    const handleSubmit = async (e: FormEvent) => {
        e.preventDefault();
        setError('');
        if (!userId.trim()) {
            setError('Kullanıcı ID gereklidir.');
            return;
        }
        setSubmitting(true);
        try {
            await onSubmit(userId.trim());
        } catch {
            setError('Üye eklenirken hata oluştu.');
        } finally {
            setSubmitting(false);
        }
    };

    return (
        <div className={styles.modalOverlay} onClick={onClose}>
            <div className={styles.modal} onClick={(e) => e.stopPropagation()}>
                <h2 className={styles.modalTitle}>Üye Ekle</h2>
                {error && (
                    <div style={{
                        padding: '8px 12px',
                        background: 'var(--color-error-light)',
                        color: 'var(--color-error)',
                        borderRadius: 'var(--border-radius-md)',
                        fontSize: 'var(--font-size-sm)',
                        marginBottom: 16,
                    }}>
                        {error}
                    </div>
                )}
                <form onSubmit={handleSubmit}>
                    <div className={styles.formGroup}>
                        <label className={styles.formLabel} htmlFor="memberId">Kullanıcı ID</label>
                        <input
                            id="memberId"
                            type="text"
                            className={styles.formInput}
                            value={userId}
                            onChange={(e) => setUserId(e.target.value)}
                            placeholder="Kullanıcı ID'sini girin"
                            autoFocus
                        />
                    </div>
                    <div className={styles.modalFooter}>
                        <button type="button" className={styles.btnSecondary} onClick={onClose}>İptal</button>
                        <button
                            type="submit"
                            className={styles.btnPrimary}
                            disabled={submitting || !userId.trim()}
                        >
                            {submitting ? 'Ekleniyor...' : 'Ekle'}
                        </button>
                    </div>
                </form>
            </div>
        </div>
    );
}
